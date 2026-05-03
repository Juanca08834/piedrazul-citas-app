using Piedrazul.Application.Abstractions.Infrastructure;

namespace Piedrazul.Infrastructure.Cache;

/// <summary>
/// No-op cache that bypasses caching entirely. Used when Redis is not configured.
/// </summary>
public sealed class NullCacheService : ICacheService
{
    public Task<T?> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory, CancellationToken cancellationToken = default)
        => factory()!;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
