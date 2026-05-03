using System.Text;
using Piedrazul.Application.Abstractions.Infrastructure;
using Piedrazul.Application.Abstractions.Repositories;
using Piedrazul.Domain;

namespace Piedrazul.Application;

public sealed class AppointmentService(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    ISystemSettingsRepository settingsRepository,
    IAppointmentPdfExporter pdfExporter,
    ICacheService cacheService,
    IAuditLogger auditLogger,
    INotificationClient notificationClient) : IAppointmentService
{
    private readonly IAppointmentRepository _appointments = appointmentRepository;
    private readonly IPatientRepository _patients = patientRepository;
    private readonly ISystemSettingsRepository _settings = settingsRepository;
    private readonly IAppointmentPdfExporter _pdfExporter = pdfExporter;
    private readonly ICacheService _cache = cacheService;
    private readonly IAuditLogger _audit = auditLogger;
    private readonly INotificationClient _notifications = notificationClient;

    public async Task<IReadOnlyList<ProviderSummaryResponse>> GetActiveProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providers = await _appointments.GetActiveProvidersAsync(cancellationToken);
        return providers
            .OrderBy(x => x.Specialty)
            .ThenBy(x => x.FirstName)
            .Select(x => new ProviderSummaryResponse(x.Id, x.DisplayName, x.Specialty, x.DefaultSlotIntervalMinutes))
            .ToList();
    }

    public async Task<OperationResult<IReadOnlyList<AvailabilitySlotResponse>>> GetAvailabilityAsync(Guid providerId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var provider = await _appointments.GetActiveProviderAsync(providerId, cancellationToken);
        if (provider is null)
        {
            return OperationResult<IReadOnlyList<AvailabilitySlotResponse>>.NotFound("No se encontró el médico o terapista seleccionado.");
        }

        var settings = await GetSettingsAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (date < today)
        {
            return OperationResult<IReadOnlyList<AvailabilitySlotResponse>>.Validation("No es posible reservar citas en fechas pasadas.");
        }

        if (date > today.AddDays(settings.WeeksAheadBooking * 7))
        {
            return OperationResult<IReadOnlyList<AvailabilitySlotResponse>>.Validation($"Solo se pueden reservar citas dentro de las próximas {settings.WeeksAheadBooking} semanas.");
        }

        var cacheKey = $"availability:{providerId}:{date:yyyyMMdd}";
        var cached = await _cache.GetOrSetAsync(cacheKey, TimeSpan.FromMinutes(2), async () =>
        {
            var availabilities = await _appointments.GetWeeklyAvailabilitiesAsync(providerId, date.DayOfWeek, cancellationToken);
            var bookedTimes = await _appointments.GetBookedTimesAsync(providerId, date, cancellationToken);

            var slots = new List<AvailabilitySlotResponse>();
            foreach (var availability in availabilities.OrderBy(x => x.StartTime))
            {
                foreach (var slot in BookingSlotCalculator.BuildSlots(availability.StartTime, availability.EndTime, availability.SlotIntervalMinutes))
                {
                    slots.Add(new AvailabilitySlotResponse(
                        slot.StartTime.ToString("HH:mm"),
                        slot.EndTime.ToString("HH:mm"),
                        !bookedTimes.Contains(slot.StartTime)));
                }
            }

            return (IReadOnlyList<AvailabilitySlotResponse>)slots;
        }, cancellationToken);

        return OperationResult<IReadOnlyList<AvailabilitySlotResponse>>.Success(cached ?? Array.Empty<AvailabilitySlotResponse>());
    }

    public async Task<OperationResult<AppointmentResponse>> CreatePublicAppointmentAsync(PublicAppointmentRequest request, string? externalUserId, string createdBy, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidatePublicRequest(request);
        if (validationErrors.Count > 0)
        {
            return OperationResult<AppointmentResponse>.Validation(validationErrors.ToArray());
        }

        var result = await CreateAppointmentCoreAsync(
            request.ProviderId,
            request.AppointmentDate,
            request.StartTime,
            request.DocumentNumber,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Gender,
            request.BirthDate,
            request.Email,
            request.BookAsGuest,
            AppointmentChannel.Web,
            null,
            externalUserId,
            createdBy,
            cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            await _cache.RemoveAsync($"availability:{request.ProviderId}:{request.AppointmentDate:yyyyMMdd}", cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult<AppointmentResponse>> CreateInternalAppointmentAsync(InternalCreateAppointmentRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateInternalRequest(request);
        if (validationErrors.Count > 0)
        {
            return OperationResult<AppointmentResponse>.Validation(validationErrors.ToArray());
        }

        var channel = ParseChannel(request.Channel);

        var result = await CreateAppointmentCoreAsync(
            request.ProviderId,
            request.AppointmentDate,
            request.StartTime,
            request.DocumentNumber,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Gender,
            request.BirthDate,
            request.Email,
            false,
            channel,
            request.Notes,
            null,
            createdBy,
            cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            await _cache.RemoveAsync($"availability:{request.ProviderId}:{request.AppointmentDate:yyyyMMdd}", cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult<AppointmentListResponse>> GetAppointmentsByProviderAndDateAsync(Guid providerId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var provider = await _appointments.GetActiveProviderAsync(providerId, cancellationToken);
        if (provider is null)
        {
            return OperationResult<AppointmentListResponse>.NotFound("No se encontró el médico o terapista solicitado.");
        }

        var appointmentEntities = await _appointments.GetAppointmentsByProviderAndDateAsync(providerId, date, cancellationToken);
        var appointments = appointmentEntities.Select(ToResponse).ToList();
        var response = new AppointmentListResponse(provider.DisplayName, provider.Specialty, date, appointments.Count, appointments);
        return OperationResult<AppointmentListResponse>.Success(response);
    }

    public async Task<byte[]> ExportAppointmentsPdfAsync(Guid providerId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var appointmentsResult = await GetAppointmentsByProviderAndDateAsync(providerId, date, cancellationToken);
        if (!appointmentsResult.Succeeded || appointmentsResult.Data is null)
        {
            return Array.Empty<byte>();
        }

        return _pdfExporter.Export(
            "Piedrazul - Centro Médico",
            appointmentsResult.Data.ProviderName,
            appointmentsResult.Data.Specialty,
            date,
            appointmentsResult.Data.Items);
    }

    public async Task<byte[]> ExportAppointmentsCsvAsync(Guid providerId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var appointmentsResult = await GetAppointmentsByProviderAndDateAsync(providerId, date, cancellationToken);
        if (!appointmentsResult.Succeeded || appointmentsResult.Data is null)
        {
            return Array.Empty<byte>();
        }

        var builder = new StringBuilder();
        builder.AppendLine("Hora,Paciente,Documento,Teléfono,Canal,Estado,Observaciones");

        foreach (var appointment in appointmentsResult.Data.Items)
        {
            var line = string.Join(',', new[]
            {
                EscapeCsv(appointment.StartTime),
                EscapeCsv(appointment.PatientFullName),
                EscapeCsv(appointment.DocumentNumber),
                EscapeCsv(appointment.Phone),
                EscapeCsv(appointment.Channel),
                EscapeCsv(appointment.Status),
                EscapeCsv(appointment.Notes ?? string.Empty)
            });
            builder.AppendLine(line);
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetBytes(builder.ToString());
    }

    public async Task<IReadOnlyList<AppointmentHistoryResponse>> GetAppointmentHistoryAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var history = await _appointments.GetHistoryAsync(appointmentId, cancellationToken);
        return history.Select(x => new AppointmentHistoryResponse(
                x.AppointmentId,
                x.PreviousDate,
                x.PreviousStartTime.ToString("HH:mm"),
                x.PreviousEndTime.ToString("HH:mm"),
                x.NewDate,
                x.NewStartTime.ToString("HH:mm"),
                x.NewEndTime.ToString("HH:mm"),
                x.Reason,
                x.ChangedBy,
                x.ChangedAtUtc))
            .ToList();
    }

    public async Task<OperationResult<AppointmentResponse>> CancelPatientAppointmentAsync(Guid appointmentId, string externalUserId, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetAppointmentByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            return OperationResult<AppointmentResponse>.NotFound("No se encontró la cita seleccionada.");
        }

        if (!string.Equals(appointment.PatientProfile?.ExternalUserId, externalUserId, StringComparison.Ordinal))
        {
            return OperationResult<AppointmentResponse>.Conflict("No tienes permisos para cancelar esta cita.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            return OperationResult<AppointmentResponse>.Validation("Solo puedes cancelar citas que sigan programadas.");
        }

        var appointmentStart = appointment.AppointmentDate.ToDateTime(appointment.StartTime);
        if (DateTime.Now >= appointmentStart)
        {
            return OperationResult<AppointmentResponse>.Validation("Solo puedes cancelar citas antes de la hora de atención.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await _appointments.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("appointment.cancelled", new { appointment.Id, appointment.AppointmentDate, appointment.StartTime }, cancellationToken);
        await _notifications.NotifyAppointmentStatusChangedAsync(appointment, cancellationToken);
        await _cache.RemoveAsync($"availability:{appointment.ProviderId}:{appointment.AppointmentDate:yyyyMMdd}", cancellationToken);

        return OperationResult<AppointmentResponse>.Success(ToResponse(appointment));
    }

    public async Task<OperationResult<AppointmentResponse>> RescheduleAppointmentAsync(RescheduleAppointmentRequest request, string changedBy, CancellationToken cancellationToken = default)
    {
        if (request.AppointmentId == Guid.Empty)
        {
            return OperationResult<AppointmentResponse>.Validation("La cita seleccionada no es válida.");
        }

        if (request.NewDate == default)
        {
            return OperationResult<AppointmentResponse>.Validation("Debes seleccionar una fecha válida.");
        }

        if (string.IsNullOrWhiteSpace(request.NewStartTime))
        {
            return OperationResult<AppointmentResponse>.Validation("Debes seleccionar una franja horaria.");
        }

        var appointment = await _appointments.GetAppointmentByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return OperationResult<AppointmentResponse>.NotFound("No se encontró la cita seleccionada.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            return OperationResult<AppointmentResponse>.Validation("Solo puedes reagendar citas que sigan programadas.");
        }

        var settings = await GetSettingsAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (request.NewDate < today)
        {
            return OperationResult<AppointmentResponse>.Validation("No es posible reagendar citas en fechas pasadas.");
        }

        if (request.NewDate > today.AddDays(settings.WeeksAheadBooking * 7))
        {
            return OperationResult<AppointmentResponse>.Validation($"Solo se pueden reservar citas dentro de las próximas {settings.WeeksAheadBooking} semanas.");
        }

        var slotResult = await ValidateAndResolveSlotAsync(appointment.ProviderId, request.NewDate, request.NewStartTime, cancellationToken);
        if (!slotResult.Succeeded || slotResult.Data is null)
        {
            return OperationResult<AppointmentResponse>.Validation(slotResult.Errors.ToArray());
        }

        var normalizedReason = PatientInputValidator.Normalize(request.Reason);
        if (!string.IsNullOrWhiteSpace(normalizedReason) && normalizedReason.Length > 500)
        {
            return OperationResult<AppointmentResponse>.Validation("El motivo no puede superar los 500 caracteres.");
        }

        normalizedReason = string.IsNullOrWhiteSpace(normalizedReason) ? null : normalizedReason;
        var normalizedChangedBy = PatientInputValidator.Normalize(changedBy);
        if (string.IsNullOrWhiteSpace(normalizedChangedBy))
        {
            normalizedChangedBy = "system";
        }

        var previousDate = appointment.AppointmentDate;
        var history = new AppointmentHistory
        {
            AppointmentId = appointment.Id,
            PreviousDate = appointment.AppointmentDate,
            PreviousStartTime = appointment.StartTime,
            PreviousEndTime = appointment.EndTime,
            NewDate = request.NewDate,
            NewStartTime = slotResult.Data.StartTime,
            NewEndTime = slotResult.Data.EndTime,
            Reason = normalizedReason,
            ChangedBy = normalizedChangedBy,
            ChangedAtUtc = DateTime.UtcNow
        };

        appointment.AppointmentDate = request.NewDate;
        appointment.StartTime = slotResult.Data.StartTime;
        appointment.EndTime = slotResult.Data.EndTime;

        await _appointments.AddHistoryAsync(history, cancellationToken);

        try
        {
            await _appointments.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            return OperationResult<AppointmentResponse>.Conflict("La franja seleccionada ya fue tomada por otro usuario. Por favor elige otra hora.");
        }

        await _audit.LogAsync("appointment.rescheduled", new
        {
            appointment.Id,
            history.PreviousDate,
            history.PreviousStartTime,
            history.NewDate,
            history.NewStartTime,
            history.ChangedBy
        }, cancellationToken);

        await _cache.RemoveAsync($"availability:{appointment.ProviderId}:{previousDate:yyyyMMdd}", cancellationToken);
        await _cache.RemoveAsync($"availability:{appointment.ProviderId}:{appointment.AppointmentDate:yyyyMMdd}", cancellationToken);
        await _notifications.NotifyAppointmentStatusChangedAsync(appointment, cancellationToken);

        return OperationResult<AppointmentResponse>.Success(ToResponse(appointment));
    }

    public async Task<OperationResult<AppointmentResponse>> UpdateAppointmentStatusAsync(Guid appointmentId, string status, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetAppointmentByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            return OperationResult<AppointmentResponse>.NotFound("No se encontró la cita seleccionada.");
        }

        if (!TryParseStatus(status, out var parsedStatus))
        {
            return OperationResult<AppointmentResponse>.Validation("El estado enviado no es válido.");
        }

        var appointmentStart = appointment.AppointmentDate.ToDateTime(appointment.StartTime);
        if (parsedStatus != AppointmentStatus.Cancelled && DateTime.Now < appointmentStart)
        {
            return OperationResult<AppointmentResponse>.Validation("Solo puedes marcar como completada o no asistió desde la hora de la cita en adelante.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            return OperationResult<AppointmentResponse>.Validation("Solo puedes actualizar citas que sigan programadas.");
        }

        appointment.Status = parsedStatus;
        await _appointments.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("appointment.status.updated", new { appointment.Id, appointment.Status }, cancellationToken);
        await _notifications.NotifyAppointmentStatusChangedAsync(appointment, cancellationToken);
        await _cache.RemoveAsync($"availability:{appointment.ProviderId}:{appointment.AppointmentDate:yyyyMMdd}", cancellationToken);

        return OperationResult<AppointmentResponse>.Success(ToResponse(appointment));
    }

    public async Task<IReadOnlyList<PatientLookupResponse>> SearchPatientsAsync(string documentTerm, CancellationToken cancellationToken = default)
    {
        var normalized = PatientInputValidator.Normalize(documentTerm);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<PatientLookupResponse>();
        }

        var profiles = await _patients.SearchByPrefixAsync(normalized, 10, cancellationToken);
        return await MapPatientLookupResponsesAsync(profiles, cancellationToken);
    }

    public async Task<PatientLookupResponse?> GetPatientByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        var normalized = PatientInputValidator.Normalize(documentNumber);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var profile = await _patients.GetByDocumentAsync(normalized, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var scheduledCount = await _appointments.CountScheduledAppointmentsByPatientIdAsync(profile.Id, cancellationToken);
        return new PatientLookupResponse(
            profile.Id,
            profile.DocumentNumber,
            profile.FirstName,
            profile.LastName,
            profile.FullName,
            profile.Phone,
            profile.Gender,
            profile.BirthDate,
            profile.Email,
            scheduledCount,
            !string.IsNullOrWhiteSpace(profile.ExternalUserId));
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetAppointmentsByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        var normalized = PatientInputValidator.Normalize(documentNumber);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<AppointmentResponse>();
        }

        var items = await _appointments.GetAppointmentsByDocumentAsync(normalized, cancellationToken);
        return items.Select(ToResponse).ToList();
    }

    private async Task<IReadOnlyList<PatientLookupResponse>> MapPatientLookupResponsesAsync(IReadOnlyList<PatientProfile> profiles, CancellationToken cancellationToken)
    {
        if (profiles.Count == 0)
        {
            return Array.Empty<PatientLookupResponse>();
        }

        var profileIds = profiles.Select(x => x.Id).ToList();
        var counts = new Dictionary<Guid, int>();
        foreach (var profileId in profileIds)
        {
            counts[profileId] = await _appointments.CountScheduledAppointmentsByPatientIdAsync(profileId, cancellationToken);
        }

        return profiles.Select(x => new PatientLookupResponse(
                x.Id,
                x.DocumentNumber,
                x.FirstName,
                x.LastName,
                x.FullName,
                x.Phone,
                x.Gender,
                x.BirthDate,
                x.Email,
                counts.GetValueOrDefault(x.Id, 0),
                !string.IsNullOrWhiteSpace(x.ExternalUserId)))
            .ToList();
    }

    private async Task<OperationResult<AppointmentResponse>> CreateAppointmentCoreAsync(
        Guid providerId,
        DateOnly appointmentDate,
        string startTime,
        string documentNumber,
        string firstName,
        string lastName,
        string phone,
        Gender gender,
        DateOnly? birthDate,
        string? email,
        bool isGuest,
        AppointmentChannel channel,
        string? notes,
        string? externalUserId,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var provider = await _appointments.GetActiveProviderAsync(providerId, cancellationToken);
        if (provider is null)
        {
            return OperationResult<AppointmentResponse>.NotFound("El médico o terapista seleccionado ya no se encuentra disponible.");
        }

        var slotResult = await ValidateAndResolveSlotAsync(providerId, appointmentDate, startTime, cancellationToken);
        if (!slotResult.Succeeded || slotResult.Data is null)
        {
            return OperationResult<AppointmentResponse>.Validation(slotResult.Errors.ToArray());
        }

        var normalizedDocument = PatientInputValidator.Normalize(documentNumber);
        var normalizedFirstName = PatientInputValidator.Normalize(firstName);
        var normalizedLastName = PatientInputValidator.Normalize(lastName);
        var normalizedPhone = PatientInputValidator.Normalize(phone);
        var normalizedEmail = PatientInputValidator.Normalize(email);
        var normalizedNotes = NormalizeNotes(notes);

        var patient = !string.IsNullOrWhiteSpace(externalUserId)
            ? await _patients.GetByExternalUserIdAsync(externalUserId, cancellationToken)
            : null;

        patient ??= await _patients.GetByDocumentAsync(normalizedDocument, cancellationToken);

        if (isGuest && string.IsNullOrWhiteSpace(externalUserId) && patient is not null)
        {
            var guestReservationCount = await _appointments.CountScheduledAppointmentsByPatientIdAsync(patient.Id, cancellationToken);
            if (guestReservationCount >= 3 && string.IsNullOrWhiteSpace(patient.ExternalUserId))
            {
                return OperationResult<AppointmentResponse>.Validation("Ya alcanzaste 3 reservas como invitado. Debes crear un usuario para continuar reservando.");
            }
        }

        if (patient is null)
        {
            patient = new PatientProfile
            {
                DocumentNumber = normalizedDocument,
                FirstName = normalizedFirstName,
                LastName = normalizedLastName,
                Phone = normalizedPhone,
                Gender = gender,
                BirthDate = birthDate,
                Email = string.IsNullOrWhiteSpace(normalizedEmail) ? null : normalizedEmail,
                ExternalUserId = externalUserId,
                IsGuest = isGuest || string.IsNullOrWhiteSpace(externalUserId)
            };
            await _patients.AddAsync(patient, cancellationToken);
        }
        else
        {
            patient.DocumentNumber = normalizedDocument;
            patient.FirstName = normalizedFirstName;
            patient.LastName = normalizedLastName;
            patient.Phone = normalizedPhone;
            patient.Gender = gender;
            patient.BirthDate = birthDate;
            patient.Email = string.IsNullOrWhiteSpace(normalizedEmail) ? patient.Email : normalizedEmail;
            if (!string.IsNullOrWhiteSpace(externalUserId))
            {
                patient.ExternalUserId = externalUserId;
                patient.IsGuest = false;
            }
        }

        var appointment = new Appointment
        {
            PatientProfile = patient,
            Provider = provider,
            AppointmentDate = appointmentDate,
            StartTime = slotResult.Data.StartTime,
            EndTime = slotResult.Data.EndTime,
            Channel = channel,
            Notes = normalizedNotes,
            CreatedBy = createdBy,
            Status = AppointmentStatus.Scheduled
        };

        await _appointments.AddAppointmentAsync(appointment, cancellationToken);

        try
        {
            await _appointments.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            return OperationResult<AppointmentResponse>.Conflict("La franja seleccionada ya fue tomada por otro usuario. Por favor elige otra hora.");
        }

        await _audit.LogAsync("appointment.created", new { appointment.Id, appointment.AppointmentDate, appointment.StartTime, createdBy }, cancellationToken);
        await _notifications.NotifyAppointmentCreatedAsync(appointment, cancellationToken);

        return OperationResult<AppointmentResponse>.Success(ToResponse(appointment));
    }

    private async Task<OperationResult<TimeSlot>> ValidateAndResolveSlotAsync(Guid providerId, DateOnly date, string startTime, CancellationToken cancellationToken)
    {
        if (!TimeOnly.TryParse(startTime, out var requestedStartTime))
        {
            return OperationResult<TimeSlot>.Validation("La hora seleccionada no tiene un formato válido.");
        }

        var availabilityResult = await GetAvailabilityAsync(providerId, date, cancellationToken);
        if (!availabilityResult.Succeeded || availabilityResult.Data is null)
        {
            return OperationResult<TimeSlot>.Validation(availabilityResult.Errors.ToArray());
        }

        var slot = availabilityResult.Data.FirstOrDefault(x => x.StartTime == requestedStartTime.ToString("HH:mm"));
        if (slot is null)
        {
            return OperationResult<TimeSlot>.Validation("La franja seleccionada no corresponde al horario configurado para este profesional.");
        }

        if (!slot.Available)
        {
            return OperationResult<TimeSlot>.Validation("La franja seleccionada ya no está disponible.");
        }

        return OperationResult<TimeSlot>.Success(new TimeSlot(TimeOnly.Parse(slot.StartTime), TimeOnly.Parse(slot.EndTime)));
    }

    private static IReadOnlyList<string> ValidatePublicRequest(PublicAppointmentRequest request)
    {
        var errors = PatientInputValidator.ValidateBasicPatientData(request.DocumentNumber, request.FirstName, request.LastName, request.Phone, request.Email).ToList();
        if (request.ProviderId == Guid.Empty)
        {
            errors.Add("Debes seleccionar un médico o terapista.");
        }

        if (request.AppointmentDate == default)
        {
            errors.Add("Debes seleccionar una fecha válida.");
        }

        if (string.IsNullOrWhiteSpace(request.StartTime))
        {
            errors.Add("Debes seleccionar una franja horaria.");
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateInternalRequest(InternalCreateAppointmentRequest request)
    {
        var errors = PatientInputValidator.ValidateBasicPatientData(request.DocumentNumber, request.FirstName, request.LastName, request.Phone, request.Email).ToList();
        if (request.ProviderId == Guid.Empty)
        {
            errors.Add("Debes seleccionar un médico o terapista.");
        }

        if (request.AppointmentDate == default)
        {
            errors.Add("Debes seleccionar una fecha válida.");
        }

        if (string.IsNullOrWhiteSpace(request.StartTime))
        {
            errors.Add("Debes seleccionar una franja horaria.");
        }

        if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Length > 500)
        {
            errors.Add("Las observaciones no pueden superar los 500 caracteres.");
        }

        return errors;
    }

    private async Task<SystemSetting> GetSettingsAsync(CancellationToken cancellationToken)
    {
        return await _settings.GetAsync(cancellationToken)
               ?? new SystemSetting { WeeksAheadBooking = 6, TimeZoneId = "America/Bogota" };
    }

    private static AppointmentChannel ParseChannel(string? channel)
    {
        return channel?.Trim().ToLowerInvariant() switch
        {
            "whatsapp" => AppointmentChannel.WhatsApp,
            "phone" => AppointmentChannel.Phone,
            "internal" => AppointmentChannel.Internal,
            _ => AppointmentChannel.WhatsApp
        };
    }

    private static string? NormalizeNotes(string? notes)
    {
        var normalized = PatientInputValidator.Normalize(notes);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized[..Math.Min(normalized.Length, 500)];
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static AppointmentResponse ToResponse(Appointment appointment)
    {
        return new AppointmentResponse(
            appointment.Id,
            appointment.Provider?.DisplayName ?? string.Empty,
            appointment.Provider?.Specialty ?? string.Empty,
            appointment.PatientProfile?.FullName ?? string.Empty,
            appointment.PatientProfile?.DocumentNumber ?? string.Empty,
            appointment.PatientProfile?.Phone ?? string.Empty,
            appointment.AppointmentDate,
            appointment.StartTime.ToString("HH:mm"),
            appointment.EndTime.ToString("HH:mm"),
            TranslateStatus(appointment.Status),
            TranslateChannel(appointment.Channel),
            appointment.Notes);
    }

    private static bool TryParseStatus(string? value, out AppointmentStatus status)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "programada":
            case "scheduled":
                status = AppointmentStatus.Scheduled;
                return true;
            case "cancelada":
            case "cancelled":
                status = AppointmentStatus.Cancelled;
                return true;
            case "completada":
            case "completed":
                status = AppointmentStatus.Completed;
                return true;
            case "no-show":
            case "no show":
            case "no asistio":
            case "no asistió":
                status = AppointmentStatus.NoShow;
                return true;
            default:
                status = AppointmentStatus.Scheduled;
                return false;
        }
    }

    private static string TranslateStatus(AppointmentStatus status)
    {
        return status switch
        {
            AppointmentStatus.Scheduled => "Programada",
            AppointmentStatus.Cancelled => "Cancelada",
            AppointmentStatus.Completed => "Completada",
            AppointmentStatus.NoShow => "No asistió",
            _ => "Programada"
        };
    }

    private static string TranslateChannel(AppointmentChannel channel)
    {
        return channel switch
        {
            AppointmentChannel.Web => "Web",
            AppointmentChannel.WhatsApp => "WhatsApp",
            AppointmentChannel.Phone => "Llamada",
            AppointmentChannel.Internal => "Portal interno",
            _ => "Web"
        };
    }
}
