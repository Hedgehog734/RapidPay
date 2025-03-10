using MassTransit;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.Application.EventHandlers;

public class FundsWithdrawnEventHandler(
    ITransactionRepository transactionRepository,
    IPublishEndpoint publisher,
    ILogger<FundsWithdrawnEventHandler> logger)
    : IConsumer<FundsWithdrawnEvent>
{
    public async Task Consume(ConsumeContext<FundsWithdrawnEvent> context)
    {
        var message = context.Message;

        try
        {
            await transactionRepository.UpdateStatusAsync(message.TransactionId, TransactionStatus.Withdrawn);

            await publisher.Publish(new DepositFundsEvent
            {
                TransactionId = message.TransactionId,
                CardNumber = message.RecipientNumber,
                SenderNumber = message.CardNumber,
                Amount = message.Amount
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to process {nameof(FundsWithdrawnEvent)} for {message.TransactionId}");

            await publisher.Publish(new TransactionFailedEvent
            {
                TransactionId = message.TransactionId,
                Reason = Reasons.ServerError,
                NeedRefund = true,
                CardNumber = message.CardNumber,
                Amount = message.Amount
            });
        }
    }
}