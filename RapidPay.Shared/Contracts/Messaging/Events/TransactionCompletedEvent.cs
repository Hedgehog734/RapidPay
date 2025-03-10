namespace RapidPay.Shared.Contracts.Messaging.Events;

public record TransactionCompletedEvent
{
    public Guid TransactionId { get; init; }
}