namespace RapidPay.Shared.Constants;

public static class TransactionStatus
{
    public const string Pending = "Pending";
    public const string Authorized = "Authorized";
    public const string Withdrawn = "Funds withdrawn";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string RefundPending = "Refund pending";
    public const string Refunded = "Refunded";
}