namespace Piedrazul.Domain;

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum AppointmentStatus
{
    Scheduled = 1,
    Cancelled = 2,
    Completed = 3,
    NoShow = 4
}

public enum AppointmentChannel
{
    Web = 1,
    WhatsApp = 2,
    Phone = 3,
    Internal = 4
}

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PatientProfile : AuditableEntity
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Email { get; set; }
    public string? ExternalUserId { get; set; }
    public bool IsGuest { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed class Provider : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public int DefaultSlotIntervalMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;

    public ICollection<WeeklyAvailability> WeeklyAvailabilities { get; set; } = new List<WeeklyAvailability>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public string DisplayName => string.Join(' ', new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed class WeeklyAvailability : AuditableEntity
{
    public Guid ProviderId { get; set; }
    public Provider? Provider { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotIntervalMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public sealed class SystemSetting : AuditableEntity
{
    public int WeeksAheadBooking { get; set; } = 6;
    public string TimeZoneId { get; set; } = "America/Bogota";
}

public sealed class Appointment : AuditableEntity
{
    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }
    public Guid ProviderId { get; set; }
    public Provider? Provider { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentChannel Channel { get; set; } = AppointmentChannel.Web;
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = "system";
    public ICollection<AppointmentHistory> History { get; set; } = new List<AppointmentHistory>();
}

public sealed class AppointmentHistory : AuditableEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public DateOnly PreviousDate { get; set; }
    public TimeOnly PreviousStartTime { get; set; }
    public TimeOnly PreviousEndTime { get; set; }
    public DateOnly NewDate { get; set; }
    public TimeOnly NewStartTime { get; set; }
    public TimeOnly NewEndTime { get; set; }
    public string? Reason { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
}
