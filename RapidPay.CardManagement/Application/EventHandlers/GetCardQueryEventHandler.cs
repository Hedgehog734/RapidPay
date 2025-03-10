using MassTransit;
using MediatR;
using RapidPay.CardManagement.Domain;
using RapidPay.Shared.Contracts;
using RapidPay.Shared.Contracts.Messaging;

namespace RapidPay.CardManagement.Application.EventHandlers;

public class GetCardQueryEventHandler(IMediator mediator) : IConsumer<GetCardQuery>
{
    public async Task Consume(ConsumeContext<GetCardQuery> context)
    {
        var card = await mediator.Send(context.Message);

        if (card is not null)
        {
            await context.RespondAsync(Result<CardResponseDto>.Success(card));
        }
        else
        {
            await context.RespondAsync(Result<CardResponseDto>.Failure(ErrorMessages.CardNotFound));
        }
    }
}