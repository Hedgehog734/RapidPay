namespace RapidPay.Shared.Contracts.Messaging.Events;

public record TransactionRefundedEvent
{
    public Guid TransactionId { get; init; }
}