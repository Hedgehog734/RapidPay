using MassTransit;
using MediatR;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;

namespace RapidPay.CardManagement.Infrastructure.Commands.Handlers;

public class UpdateCardHandler(
    ICardTransactionRepository logRepository,
    ICardRepository cardRepository,
    IPublishEndpoint publisher,
    ILogger<UpdateCardHandler> logger)
    : IRequestHandler<UpdateCardCommand, bool>
{
    public async Task<bool> Handle(UpdateCardCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.Balance.HasValue && !request.CreditLimit.HasValue)
            {
                return false;
            }

            var card = await cardRepository.GetByNumberAsync(request.CardNumber);

            if (card == null)
            {
                return false;
            }

            var isBalanceUpdated = false;
            var isLimitUpdated = false;
            var initialBalance = card.Balance;

            if (request.Balance.HasValue && request.Balance.Value != card.Balance)
            {
                card.Balance = request.Balance.Value;
                isBalanceUpdated = true;
            }

            if (request.CreditLimit.HasValue && request.CreditLimit.Value != card.CreditLimit)
            {
                card.CreditLimit = request.CreditLimit.Value;
                isLimitUpdated = true;
            }

            if (!isBalanceUpdated && !isLimitUpdated)
            {
                return false;
            }

            await cardRepository.UpdateAsync(card);

            var updatedEvent = new CardUpdatedEvent
            {
                CardNumber = card.CardNumber,
                Balance = card.Balance,
                CreditLimit = card.CreditLimit,
                UsedCredit = card.UsedCredit
            };

            await publisher.Publish(updatedEvent, cancellationToken);

            if (isBalanceUpdated)
            {
                await logRepository.AddAsync(new CardTransactionLog
                {
                    Id = Guid.NewGuid(),
                    CardNumber = card.CardNumber,
                    Amount = -(initialBalance - card.Balance),
                    TransactionType = TransactionType.Update,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update {CardNumber} card", request.CardNumber);
            return false;
        }
    }
}