using Microsoft.EntityFrameworkCore;
using Piedrazul.Application.Abstractions.Repositories;
using Piedrazul.Domain;

namespace Piedrazul.Infrastructure.Persistence.Repositories;

public sealed class SystemSettingsRepository(AppDbContext dbContext) : ISystemSettingsRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<SystemSetting?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(SystemSetting settings, CancellationToken cancellationToken = default)
    {
        await _dbContext.SystemSettings.AddAsync(settings, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
