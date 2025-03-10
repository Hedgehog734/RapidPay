namespace RapidPay.Shared.Contracts.Messaging.Events;

public record FundsWithdrawnEvent : BaseCardEntity
{
    public Guid TransactionId { get; init; }
    public string RecipientNumber { get; init; } = null!;
    public decimal Amount { get; init; }
}