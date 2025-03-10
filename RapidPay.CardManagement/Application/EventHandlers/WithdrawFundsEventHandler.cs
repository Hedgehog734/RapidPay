using MassTransit;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Persistence;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;

namespace RapidPay.CardManagement.Application.EventHandlers;

public class WithdrawFundsEventHandler(
    CardDbContext dbContext,
    ICardTransactionRepository logRepository,
    ICardRepository cardRepository,
    IPublishEndpoint publisher,
    ILogger<WithdrawFundsEventHandler> logger)
    : IConsumer<WithdrawFundsEvent>
{
    public async Task Consume(ConsumeContext<WithdrawFundsEvent> context)
    {
        var message = context.Message;
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();

        var needRefund = false;

        try
        {
            var success = await cardRepository.WithdrawAsync(message.CardNumber, message.Amount);

            if (!success)
            {
                await dbTransaction.RollbackAsync();

                await publisher.Publish(new TransactionFailedEvent
                {
                    TransactionId = message.TransactionId,
                    Reason = Reasons.InsufficientFunds,
                    NeedRefund = needRefund
                });

                return;
            }

            needRefund = true;

            await logRepository.AddAsync(new CardTransactionLog
            {
                Id = Guid.NewGuid(),
                CardNumber = message.CardNumber,
                Amount = -message.Amount,
                TransactionType = TransactionType.Withdrawal,
                CreatedAt = DateTime.UtcNow
            });

            await dbTransaction.CommitAsync();

            await publisher.Publish(new FundsWithdrawnEvent
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

            logger.LogError(ex, $"Failed to process {nameof(WithdrawFundsEvent)} for {message.TransactionId}");

            await publisher.Publish(new TransactionFailedEvent
            {
                TransactionId = message.TransactionId,
                Reason = Reasons.ServerError,
                NeedRefund = needRefund,
                Amount = message.Amount,
                CardNumber = message.CardNumber
            });
        }
    }
}