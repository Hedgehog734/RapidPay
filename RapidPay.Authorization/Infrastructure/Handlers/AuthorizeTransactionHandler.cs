using MassTransit;
using MediatR;
using Microsoft.Extensions.Options;
using RapidPay.Authorization.Application.Services;
using RapidPay.Authorization.Infrastructure.Commands;
using RapidPay.Authorization.Infrastructure.Configuration;
using RapidPay.Authorization.Infrastructure.Repositories;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Helpers;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.Authorization.Infrastructure.Handlers;

public class AuthorizeTransactionHandler(
    IMediator mediator,
    ICardManagementService cardManagementService,
    ICacheService cacheService,
    ICardAuthRepository repository,
    IPublishEndpoint publisher,
    IOptions<ServiceRedisSettings> settings,
    ILogger<AuthorizeTransactionHandler> logger)
    : IRequestHandler<AuthorizeTransactionCommand, bool>
{
    private readonly ServiceRedisSettings _settings = settings.Value;

    public async Task<bool> Handle(AuthorizeTransactionCommand request, CancellationToken cancellationToken)
    {
        var lockKey = CacheKeys.CardLock(request.SenderNumber);

        try
        {
            var lockExpiration = TimeSpan.FromSeconds(_settings.LockPeriodSeconds);

            if (!await cacheService.AcquireLockAsync(lockKey, lockExpiration))
            {
                logger.LogWarning("Transaction rejected: card {SenderNumber} is locked", request.SenderNumber);
                return false;
            }

            if (request.Amount <= 0 ||
                !await EnsureFeeAcceptableAsync(request.Amount) ||
                !await EnsureCardActive(request.SenderNumber) ||
                !await EnsureCardActive(request.RecipientNumber) ||
                await IsDuplicateTransaction(request.SenderNumber, request.RecipientNumber, request.Amount))
            {
                await cacheService.ReleaseLockAsync(lockKey);
                return false;
            }

            var cachedSender = await GetCardAsync(request.SenderNumber);
            var cachedRecipientCard = await GetCardAsync(request.RecipientNumber);

            if (cachedSender == null || cachedRecipientCard == null)
            {
                await cacheService.ReleaseLockAsync(lockKey);
                return false;
            }

            if (!FundsHelper.HasSufficientFunds(cachedSender.Balance, cachedSender.CreditLimit,
                    cachedSender.UsedCredit, request.Amount))
            {
                await cacheService.ReleaseLockAsync(lockKey);
                return false;
            }

            var authorizedEvent = new TransactionAuthorizedEvent
            {
                CardNumber = request.SenderNumber,
                RecipientNumber = request.RecipientNumber,
                Amount = request.Amount
            };

            await publisher.Publish(authorizedEvent, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authorize transaction from {SenderNumber} to {RecipientNumber}.",
                request.SenderNumber, request.RecipientNumber);

            await cacheService.ReleaseLockAsync(lockKey);
            return false;
        }
    }

    private async Task<bool> EnsureCardActive(string cardNumber)
    {
        var statusKey = CacheKeys.CardStatus(cardNumber);
        var cachedStatus = await cacheService.GetAsync<CachedCardStatus>(statusKey);

        if (cachedStatus == null)
        {
            var cardAuth = await repository.GetByNumberAsync(cardNumber);

            if (cardAuth == null)
            {
                var isAuthorized = await mediator.Send(new AuthorizeCardCommand(cardNumber));
                cachedStatus = new CachedCardStatus(cardNumber, isAuthorized);
            }
            else
            {
                cachedStatus = new CachedCardStatus(cardAuth.CardNumber, cardAuth.IsActive);
                var expiration = TimeSpan.FromSeconds(_settings.CacheDurationSeconds);
                await cacheService.SetAsync(statusKey, cachedStatus, expiration);
            }
        }

        return cachedStatus.IsActive;
    }

    private async Task<CachedCardData?> GetCardAsync(string cardNumber)
    {
        var cardKey = CacheKeys.CardData(cardNumber);
        var cachedCard = await cacheService.GetAsync<CachedCardData>(cardKey);

        if (cachedCard == null)
        {
            var card = await cardManagementService.GetCardAsync(cardNumber);

            if (card is { IsSuccess: true, Data: not null })
            {
                var cardData = card.Data;

                return new CachedCardData(cardData.CardNumber, cardData.Balance, cardData.CreditLimit,
                    cardData.UsedCredit);
            }
        }

        return cachedCard;
    }

    private async Task<bool> EnsureFeeAcceptableAsync(decimal amount)
    {
        var feeKey = CacheKeys.PaymentFee();
        var fee = await cacheService.GetAsync<decimal>(feeKey);
        return amount > fee && fee != 0;
    }

    private async Task<bool> IsDuplicateTransaction(string senderNumber, string recipientNumber, decimal amount)
    {
        var cacheKey = CacheKeys.TransactionFraud(senderNumber);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await cacheService.RemoveRangeByScoreAsync(cacheKey, 0, now - _settings.FraudPeriodSeconds);

        var existingTransactions = await cacheService.GetSortedSetAsync<CachedFraudData>(cacheKey);

        if (existingTransactions.Any(x => x.RecipientNumber == recipientNumber && x.Amount == amount))
        {
            logger.LogWarning("Duplicate transaction detected: {SenderNumber} -> {RecipientNumber}; Amount: {Amount}",
                senderNumber, recipientNumber, amount);

            return true;
        }

        var transactionData = new CachedFraudData(now, amount, recipientNumber);
        await cacheService.AddToSortedSetAsync(cacheKey, transactionData, now);

        return false;
    }
}
