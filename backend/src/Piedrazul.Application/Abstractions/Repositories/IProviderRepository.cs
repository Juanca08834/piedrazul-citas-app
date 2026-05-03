using Piedrazul.Domain;

namespace Piedrazul.Application.Abstractions.Repositories;

public interface IProviderRepository
{
    Task<IReadOnlyList<Provider>> GetWithAvailabilitiesAsync(CancellationToken cancellationToken = default);
    Task<Provider?> GetByIdAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task AddAsync(Provider provider, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WeeklyAvailability>> GetAvailabilitiesByProviderAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task RemoveAvailabilitiesAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task AddAvailabilitiesAsync(IReadOnlyList<WeeklyAvailability> availabilities, CancellationToken cancellationToken = default);
}
