using MediatR;

namespace RapidPay.Authorization.Infrastructure.Commands;

public class AuthorizeTransactionCommand(string senderNumber, string recipientNumber, decimal amount)
    : IRequest<bool>
{
    public string SenderNumber { get; } = senderNumber;
    public string RecipientNumber { get; } = recipientNumber;
    public decimal Amount { get; } = amount;
}