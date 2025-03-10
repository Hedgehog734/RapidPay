using MassTransit;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.Application.EventHandlers;

public class TransactionFailedEventHandler(
    ITransactionRepository transactionRepository,
    IPublishEndpoint publisher,
    ILogger<TransactionFailedEventHandler> logger)
    : IConsumer<TransactionFailedEvent>
{
    public async Task Consume(ConsumeContext<TransactionFailedEvent> context)
    {
        var message = context.Message;

        try
        {
            await transactionRepository.UpdateStatusAsync(message.TransactionId, message.NeedRefund
                ? TransactionStatus.RefundPending
                : TransactionStatus.Failed);

            if (message is { NeedRefund: true, CardNumber: not null, Amount: not null })
            {
                await publisher.Publish(new RefundRequestedEvent
                {
                    TransactionId = message.TransactionId,
                    CardNumber = message.CardNumber,
                    Amount = message.Amount.Value
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to process {nameof(TransactionFailedEvent)} for {message.TransactionId}");
        }
    }
}