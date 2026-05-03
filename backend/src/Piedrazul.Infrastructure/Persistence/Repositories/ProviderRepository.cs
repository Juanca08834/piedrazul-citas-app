using Microsoft.EntityFrameworkCore;
using Piedrazul.Application.Abstractions.Repositories;
using Piedrazul.Domain;

namespace Piedrazul.Infrastructure.Persistence.Repositories;

public sealed class ProviderRepository(AppDbContext dbContext) : IProviderRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<Provider>> GetWithAvailabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Providers
            .AsNoTracking()
            .Include(x => x.WeeklyAvailabilities)
            .OrderBy(x => x.Specialty)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Provider?> GetByIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Providers
            .Include(x => x.WeeklyAvailabilities)
            .FirstOrDefaultAsync(x => x.Id == providerId, cancellationToken);
    }

    public async Task AddAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        await _dbContext.Providers.AddAsync(provider, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WeeklyAvailability>> GetAvailabilitiesByProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WeeklyAvailabilities
            .Where(x => x.ProviderId == providerId)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveAvailabilitiesAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var currentAvailabilities = await GetAvailabilitiesByProviderAsync(providerId, cancellationToken);
        _dbContext.WeeklyAvailabilities.RemoveRange(currentAvailabilities);
    }

    public async Task AddAvailabilitiesAsync(IReadOnlyList<WeeklyAvailability> availabilities, CancellationToken cancellationToken = default)
    {
        await _dbContext.WeeklyAvailabilities.AddRangeAsync(availabilities, cancellationToken);
    }
}
