namespace RapidPay.Shared.Contracts;

public class GetCardQuery(string cardNumber) : IQuery<CardResponseDto?>
{
    public string CardNumber { get; } = cardNumber;
}