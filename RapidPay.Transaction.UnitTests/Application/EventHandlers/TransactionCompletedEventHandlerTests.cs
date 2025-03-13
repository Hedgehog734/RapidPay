using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Transaction.Application.EventHandlers;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.UnitTests.Application.EventHandlers;

[TestClass]
public class TransactionCompletedEventHandlerTests
{
    private ITransactionRepository _transactionRepository = null!;
    private ILogger<TransactionCompletedEventHandler> _logger = null!;
    private TransactionCompletedEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _logger = Substitute.For<ILogger<TransactionCompletedEventHandler>>();
        _handler = new TransactionCompletedEventHandler(_transactionRepository, _logger);
    }

    [TestMethod]
    public async Task Consume_Successful_StatusUpdated()
    {
        // Arrange
        var message = new TransactionCompletedEvent
        {
            TransactionId = Guid.NewGuid()
        };

        var context = Substitute.For<ConsumeContext<TransactionCompletedEvent>>();
        context.Message.Returns(message);

        // Act
        await _handler.Consume(context);

        // Assert
        await _transactionRepository.Received(1).UpdateStatusAsync(message.TransactionId, TransactionStatus.Completed);
    }

    [TestMethod]
    public async Task Consume_Error_Logged()
    {
        // Arrange
        var message = new TransactionCompletedEvent
        {
            TransactionId = Guid.NewGuid()
        };

        var context = Substitute.For<ConsumeContext<TransactionCompletedEvent>>();
        context.Message.Returns(message);

        _transactionRepository
            .UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromException(new Exception("DB error")));

        // Act
        await _handler.Consume(context);

        // Assert
        _logger.Received(1).LogError(Arg.Any<Exception>(), $"Failed to process {nameof(TransactionCompletedEvent)} for {message.TransactionId}");
    }
}