using Microsoft.EntityFrameworkCore;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Persistence;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;

namespace RapidPay.CardManagement.UnitTests.Infrastructure.Commands.Repositories;

[TestClass]
public class CardTransactionRepositoryTests
{
    private CardDbContext _dbContext = null!;
    private CardTransactionRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new CardDbContext(options);
        _repository = new CardTransactionRepository(_dbContext);
    }

    [TestMethod]
    public async Task AddAsync_ValidTransactionLog_LogAddedToDatabase()
    {
        // Arrange
        var transactionLog = new CardTransactionLog
        {
            Id = Guid.NewGuid(),
            CardNumber = "123456789",
            Amount = 500,
            TransactionType = TransactionType.Deposit,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(transactionLog);
        var storedLog = await _dbContext.CardsLogs.FirstOrDefaultAsync(log => log.Id == transactionLog.Id);

        // Assert
        Assert.IsNotNull(storedLog);
        Assert.AreEqual(transactionLog.CardNumber, storedLog!.CardNumber);
        Assert.AreEqual(transactionLog.Amount, storedLog.Amount);
        Assert.AreEqual(transactionLog.TransactionType, storedLog.TransactionType);
    }
}