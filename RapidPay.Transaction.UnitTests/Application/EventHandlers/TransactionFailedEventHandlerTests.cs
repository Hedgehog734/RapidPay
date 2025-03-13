using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Transaction.Application.EventHandlers;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.UnitTests.Application.EventHandlers;

[TestClass]
public class TransactionFailedEventHandlerTests
{
    private ITransactionRepository _transactionRepository = null!;
    private IPublishEndpoint _publisher = null!;
    private ILogger<TransactionFailedEventHandler> _logger = null!;
    private TransactionFailedEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _publisher = Substitute.For<IPublishEndpoint>();
        _logger = Substitute.For<ILogger<TransactionFailedEventHandler>>();
        _handler = new TransactionFailedEventHandler(_transactionRepository, _publisher, _logger);
    }

    [TestMethod]
    public async Task Consume_FailedWithoutRefund_StatusUpdated()
    {
        // Arrange
        var message = new TransactionFailedEvent
        {
            TransactionId = Guid.NewGuid(),
            NeedRefund = false
        };
        var context = Substitute.For<ConsumeContext<TransactionFailedEvent>>();
        context.Message.Returns(message);

        // Act
        await _handler.Consume(context);

        // Assert
        await _transactionRepository.Received(1).UpdateStatusAsync(message.TransactionId, TransactionStatus.Failed);
        await _publisher.DidNotReceive().Publish(Arg.Any<RefundRequestedEvent>());
    }

    [TestMethod]
    public async Task Consume_FailedWithRefund_StatusUpdatedAndRefundRequested()
    {
        // Arrange
        var message = new TransactionFailedEvent
        {
            TransactionId = Guid.NewGuid(),
            NeedRefund = true,
            CardNumber = "123456789",
            Amount = 100
        };

        var context = Substitute.For<ConsumeContext<TransactionFailedEvent>>();
        context.Message.Returns(message);

        // Act
        await _handler.Consume(context);

        // Assert
        await _transactionRepository.Received(1).UpdateStatusAsync(message.TransactionId, TransactionStatus.RefundPending);

        await _publisher.Received(1).Publish(Arg.Is<RefundRequestedEvent>(e =>
            e.TransactionId == message.TransactionId &&
            e.CardNumber == message.CardNumber &&
            e.Amount == message.Amount
        ));
    }

    [TestMethod]
    public async Task Consume_Error_Logged()
    {
        // Arrange
        var message = new TransactionFailedEvent
        {
            TransactionId = Guid.NewGuid(),
            NeedRefund = true,
            CardNumber = "123456789",
            Amount = 100
        };

        var context = Substitute.For<ConsumeContext<TransactionFailedEvent>>();
        context.Message.Returns(message);

        _transactionRepository.UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromException(new Exception("DB error")));

        // Act
        await _handler.Consume(context);

        // Assert
        _logger.Received(1).LogError(Arg.Any<Exception>(), $"Failed to process {nameof(TransactionFailedEvent)} for {message.TransactionId}");
    }
}