using MediatR;
using Microsoft.Extensions.Options;
using RapidPay.Authorization.Application.Services;
using RapidPay.Authorization.Domain.Entities;
using RapidPay.Authorization.Infrastructure.Commands;
using RapidPay.Authorization.Infrastructure.Repositories;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.Authorization.Infrastructure.Handlers;

public class AuthorizeCardHandler(
    ICardManagementService cardManagementService,
    IAuthLogRepository logRepository,
    ICardAuthRepository authRepository,
    ICacheService cacheService,
    IOptions<RedisSettings> settings,
    ILogger<AuthorizeCardHandler> logger)
    : IRequestHandler<AuthorizeCardCommand, bool>
{
    private readonly RedisSettings _settings = settings.Value;

    public async Task<bool> Handle(AuthorizeCardCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var lastAuthLog = await logRepository.GetLastAuthorizationAsync(request.CardNumber);

            if (lastAuthLog is { IsAuthorized: true })
            {
                return true;
            }

            var card = await cardManagementService.GetCardAsync(request.CardNumber);

            if (card is not { IsSuccess: true, Data: not null })
            {
                await AddAuthLog(request.CardNumber, false, Reasons.CardNotFound);
                return false;
            }

            var isAuthorized = CanAuthorizeCard(request.CardNumber);

            await AddAuthLog(request.CardNumber, isAuthorized, isAuthorized
                ? Reasons.Authorized
                : Reasons.AuthorizationFailed);

            await SetCardStatusAsync(request.CardNumber, isAuthorized);

            var statusKey = CacheKeys.CardStatus(request.CardNumber);
            var cachedStatus = new CachedCardStatus(request.CardNumber, isAuthorized);
            var expiration = TimeSpan.FromSeconds(_settings.CacheDurationSeconds);
            await cacheService.SetAsync(statusKey, cachedStatus, expiration);

            return isAuthorized;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authorize card {CardNumber}", request.CardNumber);
            return false;
        }
    }

    private async Task AddAuthLog(string cardNumber, bool isAuthorized, string reason)
    {
        await logRepository.AddAsync(new AuthorizationLog
        {
            Id = Guid.NewGuid(),
            CardNumber = cardNumber,
            IsAuthorized = isAuthorized,
            CreatedAt = DateTime.UtcNow,
            Reason = reason
        });
    }

    private static bool CanAuthorizeCard(string cardNumber)
    {
        return true;
    }

    private async Task SetCardStatusAsync(string cardNumber, bool isActive)
    {
        var cardAuth = await authRepository.GetByNumberAsync(cardNumber);

        if (cardAuth == null)
        {
            await authRepository.AddAsync(new CardAuthorization
            {
                CardNumber = cardNumber,
                IsActive = isActive
            });
        }
        else
        {
            cardAuth.IsActive = isActive;
            await authRepository.UpdateAsync(cardAuth);
        }
    }
}