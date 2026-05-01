using Microsoft.EntityFrameworkCore;
using Piedrazul.Application;
using Piedrazul.Domain;
using Piedrazul.Infrastructure.Persistence;

namespace Piedrazul.Infrastructure.Services;

public sealed class PatientService(AppDbContext dbContext) : IPatientService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<OperationResult<PatientProfileResponse>> GetMyProfileAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.PatientProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.ExternalUserId == externalUserId, cancellationToken);
        if (patient is null)
        {
            return OperationResult<PatientProfileResponse>.NotFound("Aún no has completado tu perfil. Guarda tus datos básicos para continuar.");
        }

        return OperationResult<PatientProfileResponse>.Success(ToResponse(patient));
    }

    public async Task<OperationResult<PatientProfileResponse>> UpsertMyProfileAsync(string externalUserId, string? email, PatientProfileUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var errors = PatientInputValidator.ValidateBasicPatientData(request.DocumentNumber, request.FirstName, request.LastName, request.Phone, request.Email ?? email).ToList();
        if (errors.Count > 0)
        {
            return OperationResult<PatientProfileResponse>.Validation(errors.ToArray());
        }

        var normalizedDocument = PatientInputValidator.Normalize(request.DocumentNumber);
        var normalizedEmail = PatientInputValidator.Normalize(request.Email ?? email);

        var patient = await _dbContext.PatientProfiles.FirstOrDefaultAsync(x => x.ExternalUserId == externalUserId, cancellationToken)
                     ?? await _dbContext.PatientProfiles.FirstOrDefaultAsync(x => x.DocumentNumber == normalizedDocument, cancellationToken);

        if (patient is null)
        {
            patient = new PatientProfile();
            _dbContext.PatientProfiles.Add(patient);
        }

        patient.ExternalUserId = externalUserId;
        patient.DocumentNumber = normalizedDocument;
        patient.FirstName = PatientInputValidator.Normalize(request.FirstName);
        patient.LastName = PatientInputValidator.Normalize(request.LastName);
        patient.Phone = PatientInputValidator.Normalize(request.Phone);
        patient.Gender = request.Gender;
        patient.BirthDate = request.BirthDate;
        patient.Email = string.IsNullOrWhiteSpace(normalizedEmail) ? null : normalizedEmail;
        patient.IsGuest = false;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return OperationResult<PatientProfileResponse>.Conflict("Ya existe otro paciente con ese documento o usuario vinculado.");
        }

        return OperationResult<PatientProfileResponse>.Success(ToResponse(patient));
    }

    public async Task<OperationResult<IReadOnlyList<AppointmentResponse>>> GetMyAppointmentsAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.PatientProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.ExternalUserId == externalUserId, cancellationToken);
        if (patient is null)
        {
            return OperationResult<IReadOnlyList<AppointmentResponse>>.NotFound("Aún no tienes un perfil vinculado al usuario autenticado.");
        }

        var appointmentEntities = await _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.PatientProfile)
            .Where(x => x.PatientProfileId == patient.Id)
            .OrderByDescending(x => x.AppointmentDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        var appointments = appointmentEntities.Select(x => new AppointmentResponse(
                x.Id,
                x.Provider!.DisplayName,
                x.Provider!.Specialty,
                x.PatientProfile!.FullName,
                x.PatientProfile!.DocumentNumber,
                x.PatientProfile!.Phone,
                x.AppointmentDate,
                x.StartTime.ToString("HH:mm"),
                x.EndTime.ToString("HH:mm"),
                x.Status switch { AppointmentStatus.Scheduled => "Programada", AppointmentStatus.Cancelled => "Cancelada", AppointmentStatus.Completed => "Completada", AppointmentStatus.NoShow => "No asistió", _ => "Programada" },
                x.Channel switch { AppointmentChannel.Web => "Web", AppointmentChannel.WhatsApp => "WhatsApp", AppointmentChannel.Phone => "Llamada", AppointmentChannel.Internal => "Portal interno", _ => "Web" },
                x.Notes))
            .ToList();

        return OperationResult<IReadOnlyList<AppointmentResponse>>.Success(appointments);
    }

    private static PatientProfileResponse ToResponse(PatientProfile patient)
    {
        return new PatientProfileResponse(
            patient.Id,
            patient.DocumentNumber,
            patient.FirstName,
            patient.LastName,
            patient.Phone,
            patient.Gender,
            patient.BirthDate,
            patient.Email,
            patient.IsGuest);
    }
}
