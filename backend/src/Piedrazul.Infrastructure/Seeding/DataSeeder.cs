using Microsoft.EntityFrameworkCore;
using Piedrazul.Domain;
using Piedrazul.Infrastructure.Persistence;

namespace Piedrazul.Infrastructure.Seeding;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Providers.AnyAsync(cancellationToken))
        {
            return;
        }

        var settings = new SystemSetting
        {
            WeeksAheadBooking = 6,
            TimeZoneId = "America/Bogota"
        };

        var providerOne = new Provider
        {
            Code = "MED001",
            FirstName = "Ana",
            LastName = "Gómez",
            Specialty = "Medicina general",
            DefaultSlotIntervalMinutes = 30,
            IsActive = true
        };

        var providerTwo = new Provider
        {
            Code = "TER001",
            FirstName = "Carlos",
            LastName = "Martínez",
            Specialty = "Terapia física",
            DefaultSlotIntervalMinutes = 45,
            IsActive = true
        };

        var providerThree = new Provider
        {
            Code = "PSI001",
            FirstName = "Laura",
            LastName = "Rivera",
            Specialty = "Psicología",
            DefaultSlotIntervalMinutes = 60,
            IsActive = true
        };

        dbContext.Providers.AddRange(providerOne, providerTwo, providerThree);
        dbContext.SystemSettings.Add(settings);

        dbContext.WeeklyAvailabilities.AddRange(
            new WeeklyAvailability { Provider = providerOne, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0), SlotIntervalMinutes = 30, IsActive = true },
            new WeeklyAvailability { Provider = providerOne, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 0), SlotIntervalMinutes = 30, IsActive = true },
            new WeeklyAvailability { Provider = providerTwo, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(13, 15), SlotIntervalMinutes = 45, IsActive = true },
            new WeeklyAvailability { Provider = providerTwo, DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 30), SlotIntervalMinutes = 45, IsActive = true },
            new WeeklyAvailability { Provider = providerThree, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0), SlotIntervalMinutes = 60, IsActive = true });

        var demoPatient = new PatientProfile
        {
            DocumentNumber = "1000000001",
            FirstName = "Paciente",
            LastName = "Demo",
            Phone = "3001234567",
            Gender = Gender.Female,
            BirthDate = new DateOnly(1992, 5, 12),
            Email = "paciente.demo@piedrazul.test",
            ExternalUserId = "demo-patient",
            IsGuest = false
        };

        dbContext.PatientProfiles.Add(demoPatient);

        var nextMonday = GetNextDate(DayOfWeek.Monday);
        dbContext.Appointments.Add(new Appointment
        {
            PatientProfile = demoPatient,
            Provider = providerOne,
            AppointmentDate = DateOnly.FromDateTime(nextMonday),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(8, 30),
            Channel = AppointmentChannel.Web,
            Status = AppointmentStatus.Scheduled,
            CreatedBy = "seed"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateTime GetNextDate(DayOfWeek dayOfWeek)
    {
        var date = DateTime.Today;
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
