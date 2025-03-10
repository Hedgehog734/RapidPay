namespace RapidPay.Shared.Contracts.Messaging.Events;

public record CardUpdatedEvent : BaseCardEntity
{
    public decimal Balance { get; init; }
    public decimal? CreditLimit { get; init; }
    public decimal? UsedCredit { get; init; }
}
