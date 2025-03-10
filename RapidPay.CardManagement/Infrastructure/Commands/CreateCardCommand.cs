using RapidPay.Shared.Contracts;

namespace RapidPay.CardManagement.Infrastructure.Commands;

public class CreateCardCommand(string cardNumber, decimal initialBalance, decimal? creditLimit)
    : ICommand<CardResponseDto>
{
    public string CardNumber { get; } = cardNumber;
    public decimal InitialBalance { get; } = initialBalance;
    public decimal? CreditLimit { get; } = creditLimit;
}