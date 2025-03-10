using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RapidPay.Shared.Contracts.Caching;

namespace RapidPay.Shared.Infrastructure.Caching;

public class RedisCacheService(IDistributedCache cache) : ICacheService
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
}