using System.Net.Http.Json;
using Piedrazul.Application.Abstractions.Infrastructure;
using Piedrazul.Domain;

namespace Piedrazul.Infrastructure.Notifications;

public sealed class HttpNotificationClient(HttpClient httpClient) : INotificationClient
{
    private readonly HttpClient _httpClient = httpClient;

    public Task NotifyAppointmentCreatedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        return _httpClient.PostAsJsonAsync("/notifications/appointment", new
        {
            appointment.Id,
            appointment.AppointmentDate,
            StartTime = appointment.StartTime.ToString("HH:mm"),
            EndTime = appointment.EndTime.ToString("HH:mm"),
            appointment.PatientProfileId,
            appointment.ProviderId
        }, cancellationToken);
    }

    public Task NotifyAppointmentStatusChangedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        return _httpClient.PostAsJsonAsync("/notifications/appointment/status", new
        {
            appointment.Id,
            appointment.Status,
            appointment.AppointmentDate,
            StartTime = appointment.StartTime.ToString("HH:mm"),
            appointment.PatientProfileId
        }, cancellationToken);
    }
}
