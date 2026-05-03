namespace Piedrazul.Application.Abstractions.Infrastructure;

public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
