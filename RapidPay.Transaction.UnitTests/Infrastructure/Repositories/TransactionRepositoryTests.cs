using Microsoft.EntityFrameworkCore;
using RapidPay.Shared.Constants;
using RapidPay.Transaction.Domain.Entities;
using RapidPay.Transaction.Infrastructure.Persistent;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.UnitTests.Infrastructure.Repositories;

[TestClass]
public class TransactionRepositoryTests
{
    private TransactionDbContext _dbContext = null!;
    private TransactionRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TransactionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TransactionDbContext(options);
        _repository = new TransactionRepository(_dbContext);
    }

    [TestMethod]
    public async Task AddTransactionAsync_ValidTransaction_AddedToDatabase()
    {
        // Arrange
        var transaction = new CardTransaction
        {
            Id = Guid.NewGuid(),
            SenderNumber = "123456789",
            RecipientNumber = "987654321",
            Amount = 100m,
            Status = TransactionStatus.Authorized,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddTransactionAsync(transaction);
        var result = await _dbContext.Transactions.FindAsync(transaction.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(transaction.Id, result!.Id);
        Assert.AreEqual(TransactionStatus.Authorized, result.Status);
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingTransaction_ReturnsTransaction()
    {
        // Arrange
        var transaction = new CardTransaction
        {
            Id = Guid.NewGuid(),
            SenderNumber = "123456789",
            RecipientNumber = "987654321",
            Amount = 100m,
            Status = TransactionStatus.Authorized,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(transaction.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(transaction.Id, result!.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistingTransaction_ReturnsNull()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(transactionId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UpdateStatusAsync_ExistingTransaction_StatusUpdated()
    {
        // Arrange
        var transaction = new CardTransaction
        {
            Id = Guid.NewGuid(),
            SenderNumber = "123456789",
            RecipientNumber = "987654321",
            Amount = 100,
            Status = TransactionStatus.Authorized,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        await _repository.UpdateStatusAsync(transaction.Id, TransactionStatus.Completed);
        var updatedTransaction = await _dbContext.Transactions.FindAsync(transaction.Id);

        // Assert
        Assert.IsNotNull(updatedTransaction);
        Assert.AreEqual(TransactionStatus.Completed, updatedTransaction!.Status);
    }

    [TestMethod]
    public async Task UpdateStatusAsync_NonExistingTransaction_NoChanges()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        // Act
        await _repository.UpdateStatusAsync(transactionId, TransactionStatus.Completed);

        // Assert
        var transactionCount = await _dbContext.Transactions.CountAsync();
        Assert.AreEqual(0, transactionCount);
    }
}