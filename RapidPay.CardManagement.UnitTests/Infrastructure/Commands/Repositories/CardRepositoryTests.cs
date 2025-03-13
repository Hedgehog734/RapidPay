using Microsoft.EntityFrameworkCore;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Persistence;
using RapidPay.CardManagement.Infrastructure.Repositories;

namespace RapidPay.CardManagement.UnitTests.Infrastructure.Commands.Repositories;

[TestClass]
public class CardRepositoryTests
{
    private CardDbContext _dbContext = null!;
    private CardRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new CardDbContext(options);
        _repository = new CardRepository(_dbContext);
    }

    [TestMethod]
    public async Task AddAsync_ValidCard_CardAdded()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        // Act
        var result = await _repository.AddAsync(card);
        var storedCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == "123456789");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(storedCard);
        Assert.AreEqual(500, storedCard.Balance);
    }

    [TestMethod]
    public async Task GetByNumberAsync_ExistingCard_ReturnsCard()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNumberAsync("123456789");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("123456789", result.CardNumber);
    }

    [TestMethod]
    public async Task GetByNumberAsync_NonExistingCard_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByNumberAsync("999999999");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UpdateAsync_CardBalanceUpdated()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        card.Balance = 700;

        // Act
        await _repository.UpdateAsync(card);
        var updatedCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == "123456789");

        // Assert
        Assert.IsNotNull(updatedCard);
        Assert.AreEqual(700, updatedCard.Balance);
    }

    [TestMethod]
    public async Task WithdrawAsync_SufficientBalance_WithdrawSuccessful()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.WithdrawAsync("123456789", 200);
        var updatedCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == "123456789");

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(updatedCard);
        Assert.AreEqual(300, updatedCard.Balance);
    }

    [TestMethod]
    public async Task WithdrawAsync_InsufficientBalance_UsesCreditLimit()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 100,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.WithdrawAsync("123456789", 300);
        var updatedCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == "123456789");

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(updatedCard);
        Assert.AreEqual(0, updatedCard.Balance);
        Assert.AreEqual(200, updatedCard.UsedCredit);
    }

    [TestMethod]
    public async Task WithdrawAsync_ExceedsCreditLimit_ReturnsFalse()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 100,
            CreditLimit = 200,
            UsedCredit = 0
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.WithdrawAsync("123456789", 400);
        var updatedCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == "123456789");

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(updatedCard);
        Assert.AreEqual(100, updatedCard!.Balance);
        Assert.AreEqual(0, updatedCard.UsedCredit);
    }

    [TestMethod]
    public async Task DepositAsync_ValidDeposit_BalanceIncreased()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.DepositAsync("123456789", 200);
        var updatedCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == "123456789");

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(updatedCard);
        Assert.AreEqual(700, updatedCard.Balance);
    }

    [TestMethod]
    public async Task DepositAsync_PartialCreditRepayment_UpdatesUsedCreditAndBalance()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 100,
            CreditLimit = 1000,
            UsedCredit = 200
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.DepositAsync("123456789", 150m);
        var updatedCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == "123456789");

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(updatedCard);
        Assert.AreEqual(50, updatedCard.UsedCredit);
        Assert.AreEqual(100, updatedCard.Balance);
    }

    [TestMethod]
    public async Task DepositAsync_FullCreditRepayment_UpdatesBalance()
    {
        // Arrange
        var card = new Card
        {
            CardNumber = "123456789",
            Balance = 100,
            CreditLimit = 1000,
            UsedCredit = 200
        };
        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.DepositAsync("123456789", 300);
        var updatedCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == "123456789");

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(updatedCard);
        Assert.AreEqual(0, updatedCard.UsedCredit);
        Assert.AreEqual(200, updatedCard.Balance);
    }
}
