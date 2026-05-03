using Piedrazul.Domain;

namespace Piedrazul.Application.Abstractions.Repositories;

public interface ISystemSettingsRepository
{
    Task<SystemSetting?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(SystemSetting settings, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
