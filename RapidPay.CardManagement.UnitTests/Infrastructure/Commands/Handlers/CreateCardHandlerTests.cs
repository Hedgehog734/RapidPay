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
public class CreateCardHandlerTests
{
    private ICardTransactionRepository _logRepository = null!;
    private ICardRepository _cardRepository = null!;
    private IPublishEndpoint _publisher = null!;
    private ILogger<CreateCardHandler> _logger = null!;
    private CreateCardHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _logRepository = Substitute.For<ICardTransactionRepository>();
        _cardRepository = Substitute.For<ICardRepository>();
        _publisher = Substitute.For<IPublishEndpoint>();
        _logger = Substitute.For<ILogger<CreateCardHandler>>();
        _handler = new CreateCardHandler(_logRepository, _cardRepository, _publisher, _logger);
    }

    [TestMethod]
    public async Task Handle_Successful_CardCreatedAndPublished()
    {
        // Arrange
        var command = new CreateCardCommand("123456789", 500, 1000);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        await _cardRepository.Received(1).AddAsync(Arg.Is<Card>(card =>
            card.CardNumber == command.CardNumber &&
            card.Balance == command.InitialBalance &&
            card.CreditLimit == command.CreditLimit &&
            card.UsedCredit == 0
        ));

        await _publisher.Received(1).Publish(Arg.Is<CardUpdatedEvent>(e =>
            e.CardNumber == command.CardNumber &&
            e.Balance == command.InitialBalance &&
            e.CreditLimit == command.CreditLimit &&
            e.UsedCredit == 0
        ), cancellationToken);

        await _logRepository.Received(1).AddAsync(Arg.Is<CardTransactionLog>(log =>
            log.CardNumber == command.CardNumber &&
            log.Amount == command.InitialBalance &&
            log.TransactionType == TransactionType.Initial
        ));

        Assert.IsNotNull(result);
        Assert.AreEqual(command.CardNumber, result.CardNumber);
        Assert.AreEqual(command.InitialBalance, result.Balance);
        Assert.AreEqual(command.CreditLimit, result.CreditLimit);
        Assert.AreEqual(0, result.UsedCredit);
    }

    [TestMethod]
    public async Task Handle_Error_ThrowsExceptionAndLogsError()
    {
        // Arrange
        var command = new CreateCardCommand("123456789", 500, 1000);
        var cancellationToken = CancellationToken.None;

        _cardRepository.AddAsync(Arg.Any<Card>())
            .Returns(Task.FromException<Card>(new Exception("DB error")));

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<Exception>(() => _handler.Handle(command, cancellationToken));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to create")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }
}