using MassTransit;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Persistence;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Constants;
using RapidPay.Shared.Contracts.Messaging.Events;

namespace RapidPay.CardManagement.Application.EventHandlers;

public class DepositFundsEventHandler(
    ICardTransactionRepository logRepository,
    CardDbContext dbContext,
    ICardRepository cardRepository,
    IPublishEndpoint publisher,
    ILogger<DepositFundsEventHandler> logger)
    : IConsumer<DepositFundsEvent>
{
    public async Task Consume(ConsumeContext<DepositFundsEvent> context)
    {
        var message = context.Message;
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();

        var isFundsDeposited = false;

        try
        {
            var success = await cardRepository.DepositAsync(message.CardNumber, message.Amount);

            if (!success)
            {
                await publisher.Publish(new TransactionFailedEvent
                {
                    TransactionId = message.TransactionId,
                    Reason = Reasons.InvalidRecipient,
                    NeedRefund = true,
                    CardNumber = message.SenderNumber,
                    Amount = message.Amount
                });

                return;
            }

            await logRepository.AddAsync(new CardTransactionLog
            {
                Id = Guid.NewGuid(),
                CardNumber = message.CardNumber,
                Amount = message.Amount,
                TransactionType = TransactionType.Deposit,
                CreatedAt = DateTime.UtcNow
            });

            await dbTransaction.CommitAsync();
            isFundsDeposited = true;

            await publisher.Publish(new TransactionCompletedEvent
            {
                TransactionId = message.TransactionId
            });
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            logger.LogError(ex, $"Failed to process {nameof(DepositFundsEvent)} for {message.TransactionId}");

            await publisher.Publish(new TransactionFailedEvent
            {
                TransactionId = message.TransactionId,
                Reason = Reasons.ServerError,
                NeedRefund = !isFundsDeposited,
                CardNumber = !isFundsDeposited ? message.SenderNumber : null!,
                Amount = !isFundsDeposited ? message.Amount : null
            });
        }
    }
}