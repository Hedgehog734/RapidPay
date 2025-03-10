namespace RapidPay.Shared.Contracts.Messaging.Events;

public record TransactionFailedEvent : BaseCardEntity
{
    public Guid TransactionId { get; init; }
    public string Reason { get; init; } = null!;
    public bool NeedRefund { get; init; }
    public decimal? Amount { get; init; }
}