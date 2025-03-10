namespace RapidPay.Shared.Helpers;

public static class FundsHelper
{
    public static bool HasSufficientFunds(decimal balance, decimal? creditLimit,
        decimal? usedCredit, decimal amount)
    {
        var availableCredit = (creditLimit ?? 0) - (usedCredit ?? 0);
        var availableFunds = balance + Math.Max(availableCredit, 0);
        return availableFunds >= amount;
    }
}