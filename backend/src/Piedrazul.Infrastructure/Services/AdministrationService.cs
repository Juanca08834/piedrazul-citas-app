using Microsoft.EntityFrameworkCore;
using Piedrazul.Application;
using Piedrazul.Domain;
using Piedrazul.Infrastructure.Persistence;

namespace Piedrazul.Infrastructure.Services;

public sealed class AdministrationService(AppDbContext dbContext) : IAdministrationService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<SystemSettingsResponse> GetSystemSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                       ?? new SystemSetting { WeeksAheadBooking = 6, TimeZoneId = "America/Bogota" };

        return new SystemSettingsResponse(settings.WeeksAheadBooking, settings.TimeZoneId);
    }

    public async Task<OperationResult<SystemSettingsResponse>> UpdateSystemSettingsAsync(SystemSettingsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.WeeksAheadBooking is < 1 or > 24)
        {
            return OperationResult<SystemSettingsResponse>.Validation("La ventana de tiempo para agendar citas debe estar entre 1 y 24 semanas.");
        }

        var settings = await _dbContext.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new SystemSetting();
            _dbContext.SystemSettings.Add(settings);
        }

        settings.WeeksAheadBooking = request.WeeksAheadBooking;
        settings.TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId) ? "America/Bogota" : request.TimeZoneId.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<SystemSettingsResponse>.Success(new SystemSettingsResponse(settings.WeeksAheadBooking, settings.TimeZoneId));
    }

    public async Task<IReadOnlyList<ProviderScheduleResponse>> GetProviderSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var providers = await _dbContext.Providers
            .AsNoTracking()
            .Include(x => x.WeeklyAvailabilities)
            .OrderBy(x => x.Specialty)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);

        return providers.Select(ToResponse).ToList();
    }


    public async Task<OperationResult<ProviderScheduleResponse>> CreateProviderScheduleAsync(ProviderScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        errors.AddRange(ScheduleValidator.ValidateInterval(request.DefaultSlotIntervalMinutes));
        errors.AddRange(PatientInputValidator.ValidateBasicPatientData("12345", request.FirstName, request.LastName, "3001234567", null)
            .Where(x => !x.Contains("documento", StringComparison.OrdinalIgnoreCase) && !x.Contains("celular", StringComparison.OrdinalIgnoreCase)));

        if (string.IsNullOrWhiteSpace(PatientInputValidator.Normalize(request.Specialty)) || request.Specialty.Length > 80)
        {
            errors.Add("La especialidad es obligatoria y no puede superar 80 caracteres.");
        }

        if (request.WeeklyAvailabilities.Count == 0)
        {
            errors.Add("Debes configurar al menos una franja de disponibilidad.");
        }

        var availabilitiesToSave = new List<WeeklyAvailability>();
        foreach (var item in request.WeeklyAvailabilities)
        {
            errors.AddRange(ScheduleValidator.ValidateInterval(item.SlotIntervalMinutes));
            if (!TimeOnly.TryParse(item.StartTime, out var startTime) || !TimeOnly.TryParse(item.EndTime, out var endTime) || startTime >= endTime)
            {
                errors.Add($"La franja del día {item.DayOfWeek} no es válida.");
                continue;
            }

            availabilitiesToSave.Add(new WeeklyAvailability
            {
                DayOfWeek = item.DayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                SlotIntervalMinutes = item.SlotIntervalMinutes,
                IsActive = item.IsActive,
            });
        }

        if (errors.Count > 0)
        {
            return OperationResult<ProviderScheduleResponse>.Validation(errors.Distinct().ToArray());
        }

        var codeBase = string.Concat(PatientInputValidator.Normalize(request.FirstName).Take(3)).ToUpperInvariant();
        var provider = new Provider
        {
            Code = $"{codeBase}{DateTime.UtcNow:HHmmss}",
            FirstName = PatientInputValidator.Normalize(request.FirstName),
            LastName = PatientInputValidator.Normalize(request.LastName),
            Specialty = PatientInputValidator.Normalize(request.Specialty),
            DefaultSlotIntervalMinutes = request.DefaultSlotIntervalMinutes,
            IsActive = true,
            WeeklyAvailabilities = availabilitiesToSave,
        };

        _dbContext.Providers.Add(provider);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var refreshedProvider = await _dbContext.Providers
            .AsNoTracking()
            .Include(x => x.WeeklyAvailabilities)
            .FirstAsync(x => x.Id == provider.Id, cancellationToken);

        return OperationResult<ProviderScheduleResponse>.Success(ToResponse(refreshedProvider));
    }

    public async Task<OperationResult<ProviderScheduleResponse>> UpdateProviderScheduleAsync(Guid providerId, ProviderScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var provider = await _dbContext.Providers.FirstOrDefaultAsync(x => x.Id == providerId, cancellationToken);
        if (provider is null)
        {
            return OperationResult<ProviderScheduleResponse>.NotFound("No se encontró el profesional que deseas actualizar.");
        }

        var errors = new List<string>();
        errors.AddRange(ScheduleValidator.ValidateInterval(request.DefaultSlotIntervalMinutes));
        errors.AddRange(PatientInputValidator.ValidateBasicPatientData("12345", request.FirstName, request.LastName, "3001234567", null)
            .Where(x => !x.Contains("documento", StringComparison.OrdinalIgnoreCase) && !x.Contains("celular", StringComparison.OrdinalIgnoreCase)));

        if (string.IsNullOrWhiteSpace(PatientInputValidator.Normalize(request.Specialty)) || request.Specialty.Length > 80)
        {
            errors.Add("La especialidad es obligatoria y no puede superar 80 caracteres.");
        }

        if (request.WeeklyAvailabilities.Count == 0)
        {
            errors.Add("Debes configurar al menos una franja de disponibilidad.");
        }

        var availabilitiesToSave = new List<WeeklyAvailability>();
        foreach (var item in request.WeeklyAvailabilities)
        {
            errors.AddRange(ScheduleValidator.ValidateInterval(item.SlotIntervalMinutes));
            if (!TimeOnly.TryParse(item.StartTime, out var startTime) || !TimeOnly.TryParse(item.EndTime, out var endTime) || startTime >= endTime)
            {
                errors.Add($"La franja del día {item.DayOfWeek} no es válida.");
                continue;
            }

            availabilitiesToSave.Add(new WeeklyAvailability
            {
                ProviderId = provider.Id,
                DayOfWeek = item.DayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                SlotIntervalMinutes = item.SlotIntervalMinutes,
                IsActive = item.IsActive,
            });
        }

        if (errors.Count > 0)
        {
            return OperationResult<ProviderScheduleResponse>.Validation(errors.Distinct().ToArray());
        }

        provider.FirstName = PatientInputValidator.Normalize(request.FirstName);
        provider.LastName = PatientInputValidator.Normalize(request.LastName);
        provider.Specialty = PatientInputValidator.Normalize(request.Specialty);
        provider.DefaultSlotIntervalMinutes = request.DefaultSlotIntervalMinutes;

        try
        {
            var currentAvailabilities = await _dbContext.WeeklyAvailabilities
                .Where(x => x.ProviderId == provider.Id)
                .ToListAsync(cancellationToken);

            _dbContext.WeeklyAvailabilities.RemoveRange(currentAvailabilities);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.WeeklyAvailabilities.AddRange(availabilitiesToSave);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return OperationResult<ProviderScheduleResponse>.Conflict("No pudimos guardar la disponibilidad. Revisa que las franjas no se solapen y vuelve a intentarlo.");
        }
        catch (Exception)
        {
            return OperationResult<ProviderScheduleResponse>.Conflict("No pudimos guardar la disponibilidad en este momento. Inténtalo nuevamente.");
        }

        var refreshedProvider = await _dbContext.Providers
            .AsNoTracking()
            .Include(x => x.WeeklyAvailabilities)
            .FirstAsync(x => x.Id == provider.Id, cancellationToken);

        return OperationResult<ProviderScheduleResponse>.Success(ToResponse(refreshedProvider));
    }


    public async Task<OperationResult<bool>> DeleteProviderScheduleAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _dbContext.Providers.FirstOrDefaultAsync(x => x.Id == providerId, cancellationToken);
        if (provider is null)
        {
            return OperationResult<bool>.NotFound("No se encontró el profesional que deseas eliminar.");
        }

        provider.IsActive = false;
        var availabilities = await _dbContext.WeeklyAvailabilities.Where(x => x.ProviderId == providerId).ToListAsync(cancellationToken);
        _dbContext.WeeklyAvailabilities.RemoveRange(availabilities);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<PatientLookupResponse>> SearchPatientsForAdminAsync(string term, CancellationToken cancellationToken = default)
    {
        var normalized = PatientInputValidator.Normalize(term);
        if (string.IsNullOrWhiteSpace(normalized)) return Array.Empty<PatientLookupResponse>();

        var profiles = await _dbContext.PatientProfiles
            .AsNoTracking()
            .Where(x => x.DocumentNumber.Contains(normalized) || x.FirstName.Contains(normalized) || x.LastName.Contains(normalized))
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Take(25)
            .ToListAsync(cancellationToken);

        var profileIds = profiles.Select(x => x.Id).ToArray();
        var counts = await _dbContext.Appointments
            .AsNoTracking()
            .Where(x => profileIds.Contains(x.PatientProfileId) && x.Status == AppointmentStatus.Scheduled)
            .GroupBy(x => x.PatientProfileId)
            .Select(x => new { PatientProfileId = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);
        var countMap = counts.ToDictionary(x => x.PatientProfileId, x => x.Count);

        return profiles.Select(profile => new PatientLookupResponse(
            profile.Id,
            profile.DocumentNumber,
            profile.FirstName,
            profile.LastName,
            profile.FullName,
            profile.Phone,
            profile.Gender,
            profile.BirthDate,
            profile.Email,
            countMap.GetValueOrDefault(profile.Id, 0),
            !string.IsNullOrWhiteSpace(profile.ExternalUserId))).ToList();
    }

    public async Task<OperationResult<PatientLookupResponse>> UpdatePatientAsync(Guid patientId, PatientProfileUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var errors = PatientInputValidator.ValidateBasicPatientData(request.DocumentNumber, request.FirstName, request.LastName, request.Phone, request.Email).ToList();
        if (errors.Count > 0)
        {
            return OperationResult<PatientLookupResponse>.Validation(errors.ToArray());
        }

        var patient = await _dbContext.PatientProfiles.FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken);
        if (patient is null)
        {
            return OperationResult<PatientLookupResponse>.NotFound("No se encontró el paciente que deseas editar.");
        }

        patient.DocumentNumber = PatientInputValidator.Normalize(request.DocumentNumber);
        patient.FirstName = PatientInputValidator.Normalize(request.FirstName);
        patient.LastName = PatientInputValidator.Normalize(request.LastName);
        patient.Phone = PatientInputValidator.Normalize(request.Phone);
        patient.Gender = request.Gender;
        patient.BirthDate = request.BirthDate;
        patient.Email = string.IsNullOrWhiteSpace(request.Email) ? null : PatientInputValidator.Normalize(request.Email);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return OperationResult<PatientLookupResponse>.Conflict("Ya existe otro paciente con esa cédula.");
        }

        var scheduledCount = await _dbContext.Appointments.AsNoTracking().CountAsync(x => x.PatientProfileId == patient.Id && x.Status == AppointmentStatus.Scheduled, cancellationToken);
        return OperationResult<PatientLookupResponse>.Success(new PatientLookupResponse(patient.Id, patient.DocumentNumber, patient.FirstName, patient.LastName, patient.FullName, patient.Phone, patient.Gender, patient.BirthDate, patient.Email, scheduledCount, !string.IsNullOrWhiteSpace(patient.ExternalUserId)));
    }

    private static ProviderScheduleResponse ToResponse(Provider provider)
    {
        return new ProviderScheduleResponse(
            provider.Id,
            provider.DisplayName,
            provider.Specialty,
            provider.DefaultSlotIntervalMinutes,
            provider.WeeklyAvailabilities
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .Select(x => new WeeklyAvailabilityDto(x.Id, x.DayOfWeek, x.StartTime.ToString("HH:mm"), x.EndTime.ToString("HH:mm"), x.SlotIntervalMinutes, x.IsActive))
                .ToList());
    }
}
