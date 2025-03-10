using MassTransit;
using Microsoft.Extensions.Options;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.CardManagement.Application.EventHandlers;

public class CardUpdatedEventHandler(
    ICacheService cacheService,
    IOptions<RedisSettings> settings,
    ILogger<CardUpdatedEventHandler> logger)
    : IConsumer<CardUpdatedEvent>
{
    private readonly RedisSettings _settings = settings.Value;

    public async Task Consume(ConsumeContext<CardUpdatedEvent> context)
    {
        var data = context.Message;
        var cardKey = CacheKeys.CardData(data.CardNumber);

        try
        {
            var cachedCard = await cacheService.GetAsync<CachedCardData>(cardKey);

            if (cachedCard == null)
            {
                cachedCard = new CachedCardData(data.CardNumber, data.Balance, data.CreditLimit, data.UsedCredit);
            }
            else
            {
                cachedCard = cachedCard with
                {
                    Balance = data.Balance,
                    CreditLimit = data.CreditLimit ?? cachedCard.CreditLimit,
                    UsedCredit = data.UsedCredit ?? cachedCard.UsedCredit
                };
            }

            var expiration = TimeSpan.FromSeconds(_settings.CacheDurationSeconds);
            await cacheService.SetAsync(cardKey, cachedCard, expiration);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to process {nameof(CardUpdatedEvent)} for {data.CardNumber}");
            throw;
        }
    }
}