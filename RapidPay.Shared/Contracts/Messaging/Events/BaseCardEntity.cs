namespace RapidPay.Shared.Contracts.Messaging.Events;

public abstract record BaseCardEntity
{
    public string CardNumber { get; init; } = null!;
}