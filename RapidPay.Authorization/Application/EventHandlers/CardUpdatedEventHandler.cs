using MassTransit;
using MediatR;
using Microsoft.Extensions.Options;
using RapidPay.Authorization.Infrastructure.Commands;
using RapidPay.Authorization.Infrastructure.Repositories;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.Authorization.Application.EventHandlers;

public class CardUpdatedEventHandler(
    IMediator mediator,
    ICardAuthRepository repository,
    ICacheService cacheService,
    IOptions<RedisSettings> settings,
    ILogger<CardUpdatedEventHandler> logger)
    : IConsumer<CardUpdatedEvent>
{
    private readonly RedisSettings _settings = settings.Value;

    public async Task Consume(ConsumeContext<CardUpdatedEvent> context)
    {
        var data = context.Message;
        var statusKey = CacheKeys.CardStatus(data.CardNumber);

        try
        {
            var cardAuth = await repository.GetByNumberAsync(data.CardNumber);

            if (cardAuth == null)
            {
                await mediator.Send(new AuthorizeCardCommand(data.CardNumber));
            }
            else
            {
                var cachedStatus = new CachedCardStatus(data.CardNumber, cardAuth.IsActive);
                var expiration = TimeSpan.FromSeconds(_settings.CacheDurationSeconds);
                await cacheService.SetAsync(statusKey, cachedStatus, expiration);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to process {nameof(CardUpdatedEvent)} for {data.CardNumber}");
            throw;
        }
    }
}