using RapidPay.Shared.Contracts;

namespace RapidPay.CardManagement.Infrastructure.Commands;

public class UpdateCardCommand(string cardNumber, decimal? balance, decimal? creditLimit)
    : ICommand<bool>
{
    public string CardNumber { get; } = cardNumber;
    public decimal? Balance { get; } = balance;
    public decimal? CreditLimit { get; } = creditLimit;
}