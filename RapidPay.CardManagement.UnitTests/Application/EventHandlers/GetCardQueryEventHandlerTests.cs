using MassTransit;
using MediatR;
using NSubstitute;
using RapidPay.CardManagement.Application.EventHandlers;
using RapidPay.CardManagement.Domain;
using RapidPay.Shared.Contracts;
using RapidPay.Shared.Contracts.Messaging;

namespace RapidPay.CardManagement.UnitTests.Application.EventHandlers;

[TestClass]
public class GetCardQueryEventHandlerTests
{
    private IMediator _mediator = null!;
    private GetCardQueryEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mediator = Substitute.For<IMediator>();
        _handler = new GetCardQueryEventHandler(_mediator);
    }

    [TestMethod]
    public async Task Consume_CardFound_ResponseWithSuccess()
    {
        // Arrange
        var query = new GetCardQuery("123456789");
        var context = Substitute.For<ConsumeContext<GetCardQuery>>();
        context.Message.Returns(query);

        var cardResponse = new CardResponseDto(query.CardNumber, 500, 1000, 200);
        _mediator.Send(query).Returns(cardResponse);

        // Act
        await _handler.Consume(context);

        // Assert
        await context.Received(1).RespondAsync(Arg.Is<Result<CardResponseDto>>(result =>
            result.IsSuccess &&
            result.Data!.CardNumber == query.CardNumber &&
            result.Data.Balance == 500
        ));
    }

    [TestMethod]
    public async Task Consume_CardNotFound_ResponseWithFailure()
    {
        // Arrange
        var query = new GetCardQuery("123456789");
        var context = Substitute.For<ConsumeContext<GetCardQuery>>();
        context.Message.Returns(query);

        _mediator.Send(query).Returns((CardResponseDto?)null);

        // Act
        await _handler.Consume(context);

        // Assert
        await context.Received(1).RespondAsync(Arg.Is<Result<CardResponseDto>>(result =>
            !result.IsSuccess &&
            result.Error == ErrorMessages.CardNotFound
        ));
    }
}