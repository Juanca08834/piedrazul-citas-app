using System.Text.Json;
using Piedrazul.Application.Abstractions.Infrastructure;
using StackExchange.Redis;

namespace Piedrazul.Infrastructure.Cache;

public sealed class RedisCacheService(IConnectionMultiplexer multiplexer) : ICacheService
{
    private readonly IDatabase _database = multiplexer.GetDatabase();

    public async Task<T?> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory, CancellationToken cancellationToken = default)
    {
        var cached = await _database.StringGetAsync(key);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<T>((string)cached!);
        }

        var value = await factory();
        var payload = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, payload, ttl);
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _database.KeyDeleteAsync(key);
    }
}
