using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;
using RapidPay.Transaction.Application.EventHandlers;
using RapidPay.Transaction.Domain.Entities;
using RapidPay.Transaction.Infrastructure.Persistent;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.UnitTests.Application.EventHandlers;

[TestClass]
public class TransactionAuthorizedEventHandlerTests
{
    private ICacheService _cacheService = null!;
    private TransactionDbContext _dbContext = null!;
    private ITransactionRepository _transactionRepository = null!;
    private IPublishEndpoint _publisher = null!;
    private ILogger<TransactionAuthorizedEventHandler> _logger = null!;
    private TransactionAuthorizedEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _cacheService = Substitute.For<ICacheService>();
        _dbContext = Substitute.For<TransactionDbContext>(new DbContextOptions<TransactionDbContext>());
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _publisher = Substitute.For<IPublishEndpoint>();
        _logger = Substitute.For<ILogger<TransactionAuthorizedEventHandler>>();

        _handler = new TransactionAuthorizedEventHandler(_cacheService, _dbContext, _transactionRepository, _publisher, _logger);
    }

    [TestMethod]
    public async Task Consume_Successful_WithdrawFundsSent()
    {
        // Arrange
        var message = new TransactionAuthorizedEvent
        {
            TransactionId = Guid.NewGuid(),
            CardNumber = "123456789",
            RecipientNumber = "987654321",
            Amount = 100
        };

        var context = Substitute.For<ConsumeContext<TransactionAuthorizedEvent>>();
        context.Message.Returns(message);

        var dbTransaction = Substitute.For<IDbContextTransaction>();

        var databaseFacade = Substitute.For<DatabaseFacade>(_dbContext);
        databaseFacade.BeginTransactionAsync().Returns(dbTransaction);

        _dbContext.Database.Returns(databaseFacade);

        // Act
        await _handler.Consume(context);

        // Assert
        await _transactionRepository.Received(1).AddTransactionAsync(Arg.Is<CardTransaction>(t =>
            t.Id == message.TransactionId &&
            t.SenderNumber == message.CardNumber &&
            t.RecipientNumber == message.RecipientNumber &&
            t.Amount == message.Amount &&
            t.Status == TransactionStatus.Authorized
        ));

        await dbTransaction.Received(1).CommitAsync();

        await _publisher.Received(1).Publish(Arg.Is<WithdrawFundsEvent>(e =>
            e.TransactionId == message.TransactionId &&
            e.CardNumber == message.CardNumber &&
            e.RecipientNumber == message.RecipientNumber &&
            e.Amount == message.Amount
        ));
    }

    [TestMethod]
    public async Task Consume_Error_TransactionFailedSent()
    {
        // Arrange
        var message = new TransactionAuthorizedEvent
        {
            TransactionId = Guid.NewGuid(),
            CardNumber = "123456789",
            RecipientNumber = "987654321",
            Amount = 100
        };

        var context = Substitute.For<ConsumeContext<TransactionAuthorizedEvent>>();
        context.Message.Returns(message);

        var dbTransaction = Substitute.For<IDbContextTransaction>();

        var databaseFacade = Substitute.For<DatabaseFacade>(_dbContext);
        databaseFacade.BeginTransactionAsync().Returns(dbTransaction);

        _dbContext.Database.Returns(databaseFacade);

        _transactionRepository.AddTransactionAsync(Arg.Any<CardTransaction>())
            .Returns(Task.FromException(new Exception("DB error")));

        // Act
        await _handler.Consume(context);

        // Assert
        await dbTransaction.Received(1).RollbackAsync();
        await _cacheService.Received(1).ReleaseLockAsync(CacheKeys.CardLock(message.CardNumber));

        _logger.Received(1).LogError(Arg.Any<Exception>(), $"Failed to process {nameof(TransactionAuthorizedEvent)} for {message.TransactionId}");

        await _publisher.Received(1).Publish(Arg.Is<TransactionFailedEvent>(e =>
            e.TransactionId == message.TransactionId &&
            e.Reason == Reasons.ServerError &&
            e.NeedRefund == false
        ));
    }
}