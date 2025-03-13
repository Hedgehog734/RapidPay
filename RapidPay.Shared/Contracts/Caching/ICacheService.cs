namespace RapidPay.Shared.Contracts.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveRangeByScoreAsync(string key, double min, double max);
    Task<IEnumerable<T>> GetSortedSetAsync<T>(string key);
    Task AddToSortedSetAsync<T>(string key, T value, double score);
    Task<bool> AcquireLockAsync(string key, TimeSpan expiration);
    Task ReleaseLockAsync(string key);
}