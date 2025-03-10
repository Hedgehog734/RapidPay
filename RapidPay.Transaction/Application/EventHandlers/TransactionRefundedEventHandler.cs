using MassTransit;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.Application.EventHandlers;

public class TransactionRefundedEventHandler(
    ITransactionRepository transactionRepository,
    ILogger<TransactionRefundedEventHandler> logger)
    : IConsumer<TransactionRefundedEvent>
{
    public async Task Consume(ConsumeContext<TransactionRefundedEvent> context)
    {
        var message = context.Message;

        try
        {
            await transactionRepository.UpdateStatusAsync(message.TransactionId, TransactionStatus.Refunded);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to process {nameof(TransactionRefundedEvent)} for {message.TransactionId}");
        }
    }
}