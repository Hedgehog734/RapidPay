using MediatR;

namespace RapidPay.Authorization.Infrastructure.Commands;

public class UpdateCardStatusCommand(string cardNumber, bool isActive)
    : IRequest<bool>
{
    public string CardNumber { get; } = cardNumber;
    public bool IsActive { get; } = isActive;
}