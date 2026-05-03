using Piedrazul.Application.Abstractions.Infrastructure;
using Piedrazul.Domain;

namespace Piedrazul.Infrastructure.Notifications;

public sealed class NoOpNotificationClient : INotificationClient
{
    public Task NotifyAppointmentCreatedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task NotifyAppointmentStatusChangedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
