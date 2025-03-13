using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RapidPay.CardManagement.Application.EventHandlers;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;

namespace RapidPay.CardManagement.UnitTests.Application.EventHandlers;

[TestClass]
public class RefundRequestedEventHandlerTests
{
    private ICardTransactionRepository _logRepository = null!;
    private ICardRepository _cardRepository = null!;
    private IPublishEndpoint _publisher = null!;
    private ILogger<RefundRequestedEventHandler> _logger = null!;
    private RefundRequestedEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _logRepository = Substitute.For<ICardTransactionRepository>();
        _cardRepository = Substitute.For<ICardRepository>();
        _publisher = Substitute.For<IPublishEndpoint>();
        _logger = Substitute.For<ILogger<RefundRequestedEventHandler>>();
        _handler = new RefundRequestedEventHandler(_logRepository, _cardRepository, _publisher, _logger);
    }

    [TestMethod]
    public async Task Consume_Successful_RefundProcessed()
    {
        // Arrange
        var message = new RefundRequestedEvent
        {
            TransactionId = Guid.NewGuid(),
            CardNumber = "123456789",
            Amount = 100
        };

        var context = Substitute.For<ConsumeContext<RefundRequestedEvent>>();
        context.Message.Returns(message);

        _cardRepository.DepositAsync(message.CardNumber, message.Amount).Returns(true);

        // Act
        await _handler.Consume(context);

        // Assert
        await _logRepository.Received(1).AddAsync(Arg.Is<CardTransactionLog>(log =>
            log.CardNumber == message.CardNumber &&
            log.Amount == message.Amount &&
            log.TransactionType == TransactionType.Refund
        ));

        await _publisher.Received(1).Publish(Arg.Is<TransactionRefundedEvent>(e =>
            e.TransactionId == message.TransactionId
        ));
    }

    [TestMethod]
    public async Task Consume_FailedRefund_LogsError()
    {
        // Arrange
        var message = new RefundRequestedEvent
        {
            TransactionId = Guid.NewGuid(),
            CardNumber = "123456789",
            Amount = 100
        };

        var context = Substitute.For<ConsumeContext<RefundRequestedEvent>>();
        context.Message.Returns(message);

        _cardRepository.DepositAsync(message.CardNumber, message.Amount).Returns(false);

        // Act
        await _handler.Consume(context);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to refund transaction")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );

        await _publisher.DidNotReceive().Publish(Arg.Any<TransactionRefundedEvent>());
    }

    [TestMethod]
    public async Task Consume_Error_LogsException()
    {
        // Arrange
        var message = new RefundRequestedEvent
        {
            TransactionId = Guid.NewGuid(),
            CardNumber = "123456789",
            Amount = 100
        };

        var context = Substitute.For<ConsumeContext<RefundRequestedEvent>>();
        context.Message.Returns(message);

        _cardRepository.DepositAsync(Arg.Any<string>(), Arg.Any<decimal>())
            .Returns(Task.FromException<bool>(new Exception("DB error")));

        // Act
        await _handler.Consume(context);

        // Assert
        _logger.Received(1).LogError(Arg.Any<Exception>(), $"Failed to process {nameof(RefundRequestedEvent)} for {message.TransactionId}");
        await _publisher.DidNotReceive().Publish(Arg.Any<TransactionRefundedEvent>());
    }
}