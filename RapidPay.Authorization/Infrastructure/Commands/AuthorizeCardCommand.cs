using MediatR;

namespace RapidPay.Authorization.Infrastructure.Commands;

public class AuthorizeCardCommand(string cardNumber)
    : IRequest<bool>
{
    public string CardNumber { get; } = cardNumber;
}