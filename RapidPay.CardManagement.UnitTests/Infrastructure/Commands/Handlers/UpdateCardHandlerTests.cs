using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Commands;
using RapidPay.CardManagement.Infrastructure.Commands.Handlers;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;

namespace RapidPay.CardManagement.UnitTests.Infrastructure.Commands.Handlers;

[TestClass]
public class UpdateCardHandlerTests
{
    private ICardTransactionRepository _logRepository = null!;
    private ICardRepository _cardRepository = null!;
    private IPublishEndpoint _publisher = null!;
    private ILogger<UpdateCardHandler> _logger = null!;
    private UpdateCardHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _logRepository = Substitute.For<ICardTransactionRepository>();
        _cardRepository = Substitute.For<ICardRepository>();
        _publisher = Substitute.For<IPublishEndpoint>();
        _logger = Substitute.For<ILogger<UpdateCardHandler>>();
        _handler = new UpdateCardHandler(_logRepository, _cardRepository, _publisher, _logger);
    }

    [TestMethod]
    public async Task Handle_Successful_BalanceUpdatedAndEventPublished()
    {
        // Arrange
        var command = new UpdateCardCommand("123456789", 700, null);
        var cancellationToken = CancellationToken.None;

        var existingCard = new Card
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        _cardRepository.GetByNumberAsync(command.CardNumber).Returns(existingCard);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        await _cardRepository.Received(1).UpdateAsync(Arg.Is<Card>(card =>
            card.CardNumber == command.CardNumber &&
            card.Balance == command.Balance &&
            card.CreditLimit == existingCard.CreditLimit
        ));

        await _publisher.Received(1).Publish(Arg.Is<CardUpdatedEvent>(e =>
            e.CardNumber == command.CardNumber &&
            e.Balance == command.Balance &&
            e.CreditLimit == existingCard.CreditLimit &&
            e.UsedCredit == existingCard.UsedCredit
        ), cancellationToken);

        await _logRepository.Received(1).AddAsync(Arg.Is<CardTransactionLog>(log =>
            log.CardNumber == command.CardNumber &&
            log.TransactionType == TransactionType.Update &&
            Math.Abs(log.Amount - 200) < 0.001m
        ));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Handle_Successful_CreditLimitUpdatedAndEventPublished()
    {
        // Arrange
        var command = new UpdateCardCommand("123456789", 1500, 500);
        var cancellationToken = CancellationToken.None;

        var existingCard = new Card
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        _cardRepository.GetByNumberAsync(command.CardNumber).Returns(existingCard);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        await _cardRepository.Received(1).UpdateAsync(Arg.Is<Card>(card =>
            card.CardNumber == command.CardNumber &&
            card.Balance == command.Balance &&
            card.CreditLimit == command.CreditLimit
        ));

        await _publisher.Received(1).Publish(Arg.Is<CardUpdatedEvent>(e =>
            e.CardNumber == command.CardNumber &&
            e.Balance == command.Balance &&
            e.CreditLimit == command.CreditLimit &&
            e.UsedCredit == existingCard.UsedCredit
        ), cancellationToken);

        await _logRepository.Received(1).AddAsync(Arg.Is<CardTransactionLog>(log =>
            log.CardNumber == command.CardNumber &&
            log.TransactionType == TransactionType.Update &&
            Math.Abs(log.Amount - 1000) < 0.001m
        ));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Handle_NoChanges_ReturnsFalse()
    {
        // Arrange
        var command = new UpdateCardCommand("123456789", 500, 1000);
        var cancellationToken = CancellationToken.None;

        var existingCard = new Card
        {
            CardNumber = "123456789",
            Balance = 500,
            CreditLimit = 1000,
            UsedCredit = 0
        };

        _cardRepository.GetByNumberAsync(command.CardNumber).Returns(existingCard);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        await _cardRepository.DidNotReceive().UpdateAsync(Arg.Any<Card>());
        await _publisher.DidNotReceive().Publish(Arg.Any<CardUpdatedEvent>(), cancellationToken);
        await _logRepository.DidNotReceive().AddAsync(Arg.Any<CardTransactionLog>());

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Handle_CardNotFound_ReturnsFalse()
    {
        // Arrange
        var command = new UpdateCardCommand("123456789", 600, null);
        var cancellationToken = CancellationToken.None;

        _cardRepository.GetByNumberAsync(command.CardNumber).Returns((Card?)null);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.IsFalse(result);
        await _cardRepository.DidNotReceive().UpdateAsync(Arg.Any<Card>());
        await _publisher.DidNotReceive().Publish(Arg.Any<CardUpdatedEvent>(), cancellationToken);
        await _logRepository.DidNotReceive().AddAsync(Arg.Any<CardTransactionLog>());
    }

    [TestMethod]
    public async Task Handle_Error_LogsExceptionAndReturnsFalse()
    {
        // Arrange
        var command = new UpdateCardCommand("123456789", 600, null);
        var cancellationToken = CancellationToken.None;

        _cardRepository.GetByNumberAsync(Arg.Any<string>())!
            .Returns(Task.FromException<Card>(new Exception("DB error")));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.IsFalse(result);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to update 123456789 card")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }
}