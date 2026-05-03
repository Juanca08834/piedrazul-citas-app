var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/notifications/appointment", (AppointmentNotification notification, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("Notifications");
    logger.LogInformation("Appointment created {@Notification}", notification);
    return Results.Accepted();
});

app.MapPost("/notifications/appointment/status", (AppointmentStatusNotification notification, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("Notifications");
    logger.LogInformation("Appointment status changed {@Notification}", notification);
    return Results.Accepted();
});

app.Run();

public sealed record AppointmentNotification(
    Guid Id,
    DateOnly AppointmentDate,
    string StartTime,
    string EndTime,
    Guid PatientProfileId,
    Guid ProviderId);

public sealed record AppointmentStatusNotification(
    Guid Id,
    string Status,
    DateOnly AppointmentDate,
    string StartTime,
    Guid PatientProfileId);
