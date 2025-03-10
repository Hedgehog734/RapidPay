using MassTransit;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.Application.EventHandlers;

public class TransactionCompletedEventHandler(
    ITransactionRepository transactionRepository,
    ILogger<TransactionCompletedEventHandler> logger)
    : IConsumer<TransactionCompletedEvent>
{
    public async Task Consume(ConsumeContext<TransactionCompletedEvent> context)
    {
        var message = context.Message;

        try
        {
            await transactionRepository.UpdateStatusAsync(message.TransactionId, TransactionStatus.Completed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to process {nameof(TransactionCompletedEvent)} for {message.TransactionId}");
        }
    }
}