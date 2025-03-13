using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RapidPay.CardManagement.Application.EventHandlers;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.CardManagement.UnitTests.Application.EventHandlers;

[TestClass]
public class CardUpdatedEventHandlerTests
{
    private ICacheService _cacheService = null!;
    private IOptions<RedisSettings> _settings = null!;
    private ILogger<CardUpdatedEventHandler> _logger = null!;
    private CardUpdatedEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _cacheService = Substitute.For<ICacheService>();
        _settings = Options.Create(new RedisSettings { CacheDurationSeconds = 3600 });
        _logger = Substitute.For<ILogger<CardUpdatedEventHandler>>();
        _handler = new CardUpdatedEventHandler(_cacheService, _settings, _logger);
    }

    [TestMethod]
    public async Task Consume_Successful_CacheUpdated()
    {
        // Arrange
        var message = new CardUpdatedEvent
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 200
        };

        var context = Substitute.For<ConsumeContext<CardUpdatedEvent>>();
        context.Message.Returns(message);

        var cardKey = CacheKeys.CardData(message.CardNumber);
        var cachedCard = new CachedCardData(message.CardNumber, 400, 900, 150);
        _cacheService.GetAsync<CachedCardData>(cardKey).Returns(cachedCard);

        // Act
        await _handler.Consume(context);

        // Assert
        await _cacheService.Received(1).SetAsync(cardKey, Arg.Is<CachedCardData>(c =>
            c.CardNumber == message.CardNumber &&
            c.Balance == message.Balance &&
            c.CreditLimit == message.CreditLimit &&
            c.UsedCredit == message.UsedCredit
        ), TimeSpan.FromSeconds(_settings.Value.CacheDurationSeconds));
    }

    [TestMethod]
    public async Task Consume_CacheMiss_CacheSetWithNewData()
    {
        // Arrange
        var message = new CardUpdatedEvent
        {
            CardNumber = "123456789",
            Balance = 500m,
            CreditLimit = 1000m,
            UsedCredit = 200m
        };

        var context = Substitute.For<ConsumeContext<CardUpdatedEvent>>();
        context.Message.Returns(message);

        var cardKey = CacheKeys.CardData(message.CardNumber);
        _cacheService.GetAsync<CachedCardData>(cardKey).Returns((CachedCardData?)null);

        // Act
        await _handler.Consume(context);

        // Assert
        await _cacheService.Received(1).SetAsync(cardKey, Arg.Is<CachedCardData>(c =>
            c.CardNumber == message.CardNumber &&
            c.Balance == message.Balance &&
            c.CreditLimit == message.CreditLimit &&
            c.UsedCredit == message.UsedCredit
        ), TimeSpan.FromSeconds(_settings.Value.CacheDurationSeconds));
    }

    [TestMethod]
    public async Task Consume_Error_LoggedAndExceptionThrown()
    {
        // Arrange
        var message = new CardUpdatedEvent
        {
            CardNumber = "123456789",
            Balance = 500m
        };

        var context = Substitute.For<ConsumeContext<CardUpdatedEvent>>();
        context.Message.Returns(message);

        var cardKey = CacheKeys.CardData(message.CardNumber);
        _cacheService.GetAsync<CachedCardData>(cardKey)!
            .Returns(Task.FromException<CachedCardData>(new Exception("Cache error")));

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<Exception>(() => _handler.Consume(context));
        _logger.Received(1).LogError(exception, $"Failed to process {nameof(CardUpdatedEvent)} for {message.CardNumber}");
    }
}