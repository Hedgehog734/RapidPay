namespace RapidPay.Shared.Contracts.Caching;

public record CachedCardData(
    string CardNumber,
    decimal Balance,
    decimal? CreditLimit,
    decimal? UsedCredit)
{
}