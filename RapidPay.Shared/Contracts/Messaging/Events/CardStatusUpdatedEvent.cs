namespace RapidPay.Shared.Contracts.Messaging.Events;

public record CardStatusUpdatedEvent : BaseCardEntity
{
    public bool IsActive { get; init; }
}