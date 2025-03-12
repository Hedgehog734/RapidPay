using MassTransit;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.CardManagement.Application.EventHandlers;

public class FeeUpdatedEventHandler(ICacheService cacheService) : IConsumer<FeeUpdatedEvent>
{
    public async Task Consume(ConsumeContext<FeeUpdatedEvent> context)
    {
        var feeKey = CacheKeys.PaymentFee();
        var newFee = context.Message.Value;
        await cacheService.SetAsync(feeKey, newFee, TimeSpan.FromHours(1));
    }
}