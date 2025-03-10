namespace RapidPay.Shared.Contracts;

public record CardResponseDto(
    string CardNumber,
    decimal Balance,
    decimal? CreditLimit,
    decimal? UsedCredit)
{
}