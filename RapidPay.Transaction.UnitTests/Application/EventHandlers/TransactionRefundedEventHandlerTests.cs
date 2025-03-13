using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Transaction.Application.EventHandlers;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.UnitTests.Application.EventHandlers;

[TestClass]
public class TransactionRefundedEventHandlerTests
{
    private ITransactionRepository _transactionRepository = null!;
    private ILogger<TransactionRefundedEventHandler> _logger = null!;
    private TransactionRefundedEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _logger = Substitute.For<ILogger<TransactionRefundedEventHandler>>();
        _handler = new TransactionRefundedEventHandler(_transactionRepository, _logger);
    }

    [TestMethod]
    public async Task Consume_Successful_StatusUpdated()
    {
        // Arrange
        var message = new TransactionRefundedEvent
        {
            TransactionId = Guid.NewGuid()
        };

        var context = Substitute.For<ConsumeContext<TransactionRefundedEvent>>();
        context.Message.Returns(message);

        // Act
        await _handler.Consume(context);

        // Assert
        await _transactionRepository.Received(1).UpdateStatusAsync(message.TransactionId, TransactionStatus.Refunded);
    }

    [TestMethod]
    public async Task Consume_Error_Logged()
    {
        // Arrange
        var message = new TransactionRefundedEvent
        {
            TransactionId = Guid.NewGuid()
        };

        var context = Substitute.For<ConsumeContext<TransactionRefundedEvent>>();
        context.Message.Returns(message);

        _transactionRepository.UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromException(new Exception("DB error")));

        // Act
        await _handler.Consume(context);

        // Assert
        _logger.Received(1).LogError(Arg.Any<Exception>(), $"Failed to process {nameof(TransactionRefundedEvent)} for {message.TransactionId}");
    }
}