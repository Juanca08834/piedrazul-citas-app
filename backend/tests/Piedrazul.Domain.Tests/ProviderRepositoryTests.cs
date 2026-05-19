using Microsoft.EntityFrameworkCore;
using Piedrazul.Domain;
using Piedrazul.Infrastructure.Persistence;
using Piedrazul.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Piedrazul.Domain.Tests;

public sealed class ProviderRepositoryTests
{
    [Fact]
    public async Task GetWithAvailabilitiesAsync_ShouldReturnOnlyActiveProviders()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"providers-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new AppDbContext(options);

        var activeProvider = new Provider
        {
            FirstName = "Ana",
            LastName = "Gomez",
            Specialty = "Fisioterapia",
            Code = "ACT123",
            IsActive = true,
            WeeklyAvailabilities =
            [
                new WeeklyAvailability
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(12, 0),
                    SlotIntervalMinutes = 30,
                    IsActive = true
                }
            ]
        };

        var inactiveProvider = new Provider
        {
            FirstName = "Luis",
            LastName = "Perez",
            Specialty = "General",
            Code = "INA123",
            IsActive = false,
            WeeklyAvailabilities =
            [
                new WeeklyAvailability
                {
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 0),
                    SlotIntervalMinutes = 30,
                    IsActive = true
                }
            ]
        };

        dbContext.Providers.Add(activeProvider);
        dbContext.Providers.Add(inactiveProvider);
        await dbContext.SaveChangesAsync();

        var repository = new ProviderRepository(dbContext);

        var results = await repository.GetWithAvailabilitiesAsync();

        Assert.Single(results);
        Assert.Equal(activeProvider.Id, results[0].Id);
        Assert.All(results, provider => Assert.True(provider.IsActive));
    }
}
