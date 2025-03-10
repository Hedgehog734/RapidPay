namespace RapidPay.Shared.Contracts.Messaging.Events;

public record TransactionAuthorizedEvent : BaseCardEntity
{
    public Guid TransactionId { get; init; } = Guid.NewGuid();
    public string RecipientNumber { get; init; } = null!;
    public decimal Amount { get; init; }
}