namespace RapidPay.Shared.Contracts.Caching;

public record CachedFraudData(long Timestamp, decimal Amount, string RecipientNumber);