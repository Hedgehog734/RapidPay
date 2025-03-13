using MassTransit;
using NSubstitute;
using RapidPay.CardManagement.Application.EventHandlers;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.CardManagement.UnitTests.Application.EventHandlers;

[TestClass]
public class FeeUpdatedEventHandlerTests
{
    private ICacheService _cacheService = null!;
    private FeeUpdatedEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _cacheService = Substitute.For<ICacheService>();
        _handler = new FeeUpdatedEventHandler(_cacheService);
    }

    [TestMethod]
    public async Task Consume_Successful_FeeUpdatedInCache()
    {
        // Arrange
        var message = new FeeUpdatedEvent { Value = 0.05m };
        var context = Substitute.For<ConsumeContext<FeeUpdatedEvent>>();
        context.Message.Returns(message);

        var feeKey = CacheKeys.PaymentFee();
        var expiration = TimeSpan.FromHours(1);

        // Act
        await _handler.Consume(context);

        // Assert
        await _cacheService.Received(1).SetAsync(feeKey, message.Value, expiration);
    }
}