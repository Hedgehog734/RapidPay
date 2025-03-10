namespace RapidPay.Shared.Contracts.Messaging.Events;

public record DepositFundsEvent : BaseCardEntity
{
    public Guid TransactionId { get; init; }
    public string SenderNumber { get; init; } = null!;
    public decimal Amount { get; init; }
}