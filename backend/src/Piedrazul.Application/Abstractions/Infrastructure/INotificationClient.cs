using Piedrazul.Domain;

namespace Piedrazul.Application.Abstractions.Infrastructure;
//Desacopla la Api del Microservicio 
public interface INotificationClient
{
    //Define métodos para notificar eventos relacionados con las citas, como la creación de una cita o el cambio de estado de una cita.|
    Task NotifyAppointmentCreatedAsync(Appointment appointment, CancellationToken cancellationToken = default);
    //Define un método para notificar cambios en el estado de una cita, como la cancelación o la finalización de una cita.|
    Task NotifyAppointmentStatusChangedAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
