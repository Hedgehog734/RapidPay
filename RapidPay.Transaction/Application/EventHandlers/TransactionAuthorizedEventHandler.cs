using MassTransit;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Transaction.Domain.Entities;
using RapidPay.Transaction.Infrastructure.Persistent;
using RapidPay.Transaction.Infrastructure.Repositories;

namespace RapidPay.Transaction.Application.EventHandlers;

public class TransactionAuthorizedEventHandler(
    TransactionDbContext dbContext,
    ITransactionRepository transactionRepository,
    IPublishEndpoint publisher,
    ILogger<TransactionAuthorizedEventHandler> logger)
    : IConsumer<TransactionAuthorizedEvent>
{
    public async Task Consume(ConsumeContext<TransactionAuthorizedEvent> context)
    {
        var message = context.Message;
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var transaction = new CardTransaction
            {
                Id = message.TransactionId,
                SenderNumber = message.CardNumber,
                RecipientNumber = message.RecipientNumber,
                Amount = message.Amount,
                Status = TransactionStatus.Authorized,
                CreatedAt = DateTime.UtcNow
            };

            await transactionRepository.AddTransactionAsync(transaction);
            await dbTransaction.CommitAsync();

            await publisher.Publish(new WithdrawFundsEvent
            {
                TransactionId = message.TransactionId,
                CardNumber = message.CardNumber,
                RecipientNumber = message.RecipientNumber,
                Amount = message.Amount
            });
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            logger.LogError(ex, $"Failed to process {nameof(TransactionAuthorizedEvent)} for {message.TransactionId}");

            await publisher.Publish(new TransactionFailedEvent
            {
                TransactionId = message.TransactionId,
                Reason = Reasons.ServerError,
                NeedRefund = false
            });
        }
    }
}