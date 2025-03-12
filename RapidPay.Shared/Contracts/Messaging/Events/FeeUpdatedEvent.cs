namespace RapidPay.Shared.Contracts.Messaging.Events;

public record FeeUpdatedEvent
{
    public decimal Value { get; init; }
}