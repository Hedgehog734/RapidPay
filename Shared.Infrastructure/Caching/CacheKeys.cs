namespace RapidPay.Shared.Infrastructure.Caching;

public static class CacheKeys
{
    public static string CardData(string cardNumber) => $"card:{cardNumber}:data";
    public static string CardStatus(string cardNumber) => $"card:{cardNumber}:status";
    public static string TransactionFraud(string cardNumber) => $"card:{cardNumber}:fraud";
    public static string PaymentFee() => "fee";
}