using MassTransit;
using MediatR;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts;
using RapidPay.Shared.Contracts.Messaging.Events;

namespace RapidPay.CardManagement.Infrastructure.Commands.Handlers;

public class CreateCardHandler(
    ICardTransactionRepository logRepository,
    ICardRepository cardRepository,
    IPublishEndpoint publisher,
    ILogger<CreateCardHandler> logger)
    : IRequestHandler<CreateCardCommand, CardResponseDto>
{
    public async Task<CardResponseDto> Handle(CreateCardCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var card = new Card
            {
                CardNumber = request.CardNumber,
                Balance = request.InitialBalance,
                CreditLimit = request.CreditLimit,
                UsedCredit = 0
            };

            await cardRepository.AddAsync(card);
            var cardDto = new CardResponseDto(card.CardNumber, card.Balance, card.CreditLimit, card.UsedCredit);

            var updatedEvent = new CardUpdatedEvent
            {
                CardNumber = card.CardNumber,
                Balance = card.Balance,
                CreditLimit = card.CreditLimit,
                UsedCredit = card.UsedCredit
            };

            await publisher.Publish(updatedEvent, cancellationToken);

            await logRepository.AddAsync(new CardTransactionLog
            {
                Id = Guid.NewGuid(),
                CardNumber = card.CardNumber,
                Amount = card.Balance,
                TransactionType = TransactionType.Initial,
                CreatedAt = DateTime.UtcNow
            });

            return cardDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create {CardNumber} card", request.CardNumber);
            throw;
        }
    }
}