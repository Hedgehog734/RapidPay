namespace RapidPay.Shared.Contracts.Messaging.Events;

public record RefundRequestedEvent : BaseCardEntity
{
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
}