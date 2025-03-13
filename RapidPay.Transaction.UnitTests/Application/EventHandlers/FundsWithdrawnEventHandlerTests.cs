using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;
using RapidPay.Transaction.Application.EventHandlers;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.UnitTests.Application.EventHandlers
{
    [TestClass]
    public class FundsWithdrawnEventHandlerTests
    {
        private ICacheService _cacheService = null!;
        private ITransactionRepository _transactionRepository = null!;
        private IPublishEndpoint _publisher = null!;
        private ILogger<FundsWithdrawnEventHandler> _logger = null!;
        private FundsWithdrawnEventHandler _handler = null!;

        [TestInitialize]
        public void Setup()
        {
            _cacheService = Substitute.For<ICacheService>();
            _transactionRepository = Substitute.For<ITransactionRepository>();
            _publisher = Substitute.For<IPublishEndpoint>();
            _logger = Substitute.For<ILogger<FundsWithdrawnEventHandler>>();

            _handler = new FundsWithdrawnEventHandler(_cacheService, _transactionRepository, _publisher, _logger);
        }

        [TestMethod]
        public async Task Consume_Successful_DepositFundsSent()
        {
            // Arrange
            var message = new FundsWithdrawnEvent
            {
                TransactionId = Guid.NewGuid(),
                CardNumber = "123456789",
                RecipientNumber = "987654321",
                Amount = 100
            };

            var context = Substitute.For<ConsumeContext<FundsWithdrawnEvent>>();
            context.Message.Returns(message);

            // Act
            await _handler.Consume(context);

            // Assert
            await _transactionRepository.Received(1).UpdateStatusAsync(message.TransactionId, TransactionStatus.Withdrawn);

            await _publisher.Received(1).Publish(Arg.Is<DepositFundsEvent>(e =>
                e.TransactionId == message.TransactionId &&
                e.CardNumber == message.RecipientNumber &&
                e.SenderNumber == message.CardNumber &&
                e.Amount == message.Amount
            ));

            await _cacheService.Received(1).ReleaseLockAsync(CacheKeys.CardLock(message.CardNumber));
        }

        [TestMethod]
        public async Task Consume_Error_TransactionFailedSent()
        {
            // Arrange
            var message = new FundsWithdrawnEvent
            {
                TransactionId = Guid.NewGuid(),
                CardNumber = "123456789",
                RecipientNumber = "987654321",
                Amount = 100m
            };

            var context = Substitute.For<ConsumeContext<FundsWithdrawnEvent>>();
            context.Message.Returns(message);

            _transactionRepository
                .UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns(Task.FromException(new Exception("DB error")));

            // Act
            await _handler.Consume(context);

            // Assert
            _logger.Received(1).LogError(Arg.Any<Exception>(), $"Failed to process {nameof(FundsWithdrawnEvent)} for {message.TransactionId}");

            await _publisher.Received(1).Publish(Arg.Is<TransactionFailedEvent>(e =>
                e.TransactionId == message.TransactionId &&
                e.Reason == Reasons.ServerError &&
                e.NeedRefund &&
                e.CardNumber == message.CardNumber &&
                e.Amount == message.Amount
            ));

            await _cacheService.Received(1).ReleaseLockAsync(CacheKeys.CardLock(message.CardNumber));
        }
    }
}
