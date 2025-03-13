using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RapidPay.Shared.Contracts.Caching;
using StackExchange.Redis;

namespace RapidPay.Shared.Infrastructure.Caching;

public class RedisCacheService(IDistributedCache cache, IDatabase database) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var jsonData = await cache.GetStringAsync(key);
        return jsonData != null ? JsonSerializer.Deserialize<T>(jsonData) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var jsonData = JsonSerializer.Serialize(value);

        await cache.SetStringAsync(key, jsonData, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });
    }

    public async Task RemoveRangeByScoreAsync(string key, double min, double max)
    {
        await database.SortedSetRemoveRangeByScoreAsync(key, min, max);
    }

    public async Task<IEnumerable<T>> GetSortedSetAsync<T>(string key)
    {
        var values = await database.SortedSetRangeByRankAsync(key);

        return values.Select(value => value.HasValue
                ? JsonSerializer.Deserialize<T>(value.ToString())
                : default)
            .Where(value => value != null)!;
    }

    public async Task AddToSortedSetAsync<T>(string key, T value, double score)
    {
        var json = JsonSerializer.Serialize(value);
        await database.SortedSetAddAsync(key, json, score);
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiration)
    {
        return await database.StringSetAsync(key, "locked", expiration, When.NotExists);
    }

    public async Task ReleaseLockAsync(string key)
    {
        await database.KeyDeleteAsync(key);
    }
}