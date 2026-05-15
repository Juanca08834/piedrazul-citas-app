using System.Text;
using System.Text.Json;
using Piedrazul.Application.Abstractions.Infrastructure;
using Piedrazul.Domain;
using RabbitMQ.Client;

namespace Piedrazul.Infrastructure.Notifications;
//PUBLICADOR DE EVENTOS EN RABBITMQ, DESACOPLANDO LA API DEL MICROSERVICIO DE NOTIFICACIONES
public sealed class RabbitMqNotificationClient(IConnection connection) : INotificationClient
{
    private const string Exchange = "piedrazul";

    public Task NotifyAppointmentCreatedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var payload = new
        {   
            Id = appointment.Id,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime.ToString("HH:mm"),
            EndTime = appointment.EndTime.ToString("HH:mm"),
            PatientProfileId = appointment.PatientProfileId,
            ProviderId = appointment.ProviderId,
        };
        return PublishAsync("appointment.created", payload, cancellationToken);
    }

    public Task NotifyAppointmentStatusChangedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {   //El payload incluye la información relevante del cambio de estado de la cita, como el Id,
        // el nuevo estado, la fecha y hora de la cita, y los Ids del paciente
        var payload = new
        {
            Id = appointment.Id,
            Status = appointment.Status.ToString(),
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime.ToString("HH:mm"),
            PatientProfileId = appointment.PatientProfileId,
        };
        return PublishAsync("appointment.status", payload, cancellationToken);
    }

    private async Task PublishAsync<T>(string routingKey, T payload, CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        //Serializa el payload a JSON y lo convierte a bytes para enviarlo a RabbitMQ
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var props = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }
}
