using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Piedrazul.Notifications.Consumers;

/// 
/// Consumidor encargado de procesar eventos de notificación de citas desde RabbitMQ.
/// 
public sealed class AppointmentNotificationConsumer : BackgroundService
{
    private const string Exchange = "piedrazul";
    private const string Queue = "notifications.appointments";

    private readonly ILogger<AppointmentNotificationConsumer> _logger;
    private readonly string _connectionString;
    private IConnection? _connection;
    private IChannel? _channel;

    public AppointmentNotificationConsumer(
        IConfiguration configuration,
        ILogger<AppointmentNotificationConsumer> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetSection("RabbitMq").GetValue<string>("ConnectionString")
            ?? throw new InvalidOperationException("RabbitMq:ConnectionString is required.");
    }

    /// 
    /// Método principal del servicio en segundo plano.
    /// Se conecta a RabbitMQ y comienza a consumir mensajes de la cola.
    /// 
    /// <param name="stoppingToken">Un token que indica cuándo se está deteniendo el servicio.</param>
    /// <returns>Una tarea que representa la operación de larga duración.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appointment notification consumer starting.");

        stoppingToken.ThrowIfCancellationRequested();

        var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: "appointment.*",
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new EventingBasicConsumer(_channel);

        // Event handler for when a message is received
        consumer.Received += async (ch, ea) =>
        {
            await HandleMessage(ea, ch);
        };

        await _channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "AppointmentNotificationConsumer started. Listening on '{Exchange}' → '{Queue}'",
            Exchange, Queue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — stoppingToken was cancelled.
        }
    }

    /// <summary>
    /// Maneja un mensaje entrante de la cola de RabbitMQ.
    /// Deserializa el mensaje, lo registra y envía un acuse de recibo (ACK/NACK).
    /// </summary>
    /// <param name="ea">Los argumentos del evento que contienen los datos del mensaje.</param>
    /// <param name="channel">El canal de RabbitMQ.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    private async Task HandleMessage(BasicDeliverEventArgs ea, IModel channel)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var routingKey = ea.RoutingKey;

        _logger.LogInformation("[{RoutingKey}] {Payload}", routingKey, message);

        // Extension point: send WhatsApp / SMS / email based on ea.RoutingKey

        if (_channel is not null)
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
