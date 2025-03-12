using MassTransit;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Persistence;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.CardManagement.Application.EventHandlers;

public class DepositFundsEventHandler(
    ICacheService cacheService,
    ICardTransactionRepository logRepository,
    CardDbContext dbContext,
    ICardRepository cardRepository,
    IPublishEndpoint publisher,
    ILogger<DepositFundsEventHandler> logger)
    : IConsumer<DepositFundsEvent>
{
    public async Task Consume(ConsumeContext<DepositFundsEvent> context)
    {
        var message = context.Message;
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();

        var isFundsDeposited = false;

        try
        {
            var fee = await cacheService.GetAsync<decimal>(CacheKeys.PaymentFee());

            if (fee == 0)
            {
                await SendFailedEvent(message, Reasons.FeeNotFound);
                return;
            }

            var success = await cardRepository.DepositAsync(message.CardNumber, message.Amount, fee);

            if (!success)
            {
                await SendFailedEvent(message, Reasons.InvalidRecipient);
                return;
            }

            await logRepository.AddAsync(new CardTransactionLog
            {
                Id = Guid.NewGuid(),
                CardNumber = message.CardNumber,
                Amount = message.Amount,
                TransactionType = TransactionType.Deposit,
                CreatedAt = DateTime.UtcNow
            });

            await dbTransaction.CommitAsync();
            isFundsDeposited = true;

            await publisher.Publish(new TransactionCompletedEvent
            {
                TransactionId = message.TransactionId
            });
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            logger.LogError(ex, $"Failed to process {nameof(DepositFundsEvent)} for {message.TransactionId}");

            await publisher.Publish(new TransactionFailedEvent
            {
                TransactionId = message.TransactionId,
                Reason = Reasons.ServerError,
                NeedRefund = !isFundsDeposited,
                CardNumber = !isFundsDeposited ? message.SenderNumber : null!,
                Amount = !isFundsDeposited ? message.Amount : null
            });
        }
    }

    private async Task SendFailedEvent(DepositFundsEvent message, string reason)
    {
        await publisher.Publish(new TransactionFailedEvent
        {
            TransactionId = message.TransactionId,
            Reason = reason,
            NeedRefund = true,
            CardNumber = message.SenderNumber,
            Amount = message.Amount
        });
    }
}