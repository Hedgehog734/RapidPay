using MassTransit;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;

namespace RapidPay.CardManagement.Application.EventHandlers;

public class RefundRequestedEventHandler(
    ICardTransactionRepository logRepository,
    ICardRepository cardRepository,
    IPublishEndpoint publisher,
    ILogger<RefundRequestedEventHandler> logger)
    : IConsumer<RefundRequestedEvent>
{
    public async Task Consume(ConsumeContext<RefundRequestedEvent> context)
    {
        var message = context.Message;

        try
        {
            var success = await cardRepository.DepositAsync(message.CardNumber, message.Amount);

            if (!success)
            {
                logger.LogError("Failed to refund transaction {TransactionId}", message.TransactionId);
                return;
            }

            await logRepository.AddAsync(new CardTransactionLog
            {
                Id = Guid.NewGuid(),
                CardNumber = message.CardNumber,
                Amount = message.Amount,
                TransactionType = TransactionType.Refund,
                CreatedAt = DateTime.UtcNow
            });

            await publisher.Publish(new TransactionRefundedEvent
            {
                TransactionId = message.TransactionId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to process {nameof(RefundRequestedEvent)} for {message.TransactionId}");
        }
    }
}