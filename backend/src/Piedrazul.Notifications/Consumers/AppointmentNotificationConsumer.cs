using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Piedrazul.Notifications.Consumers;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

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

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("[{RoutingKey}] {Payload}", ea.RoutingKey, json);

            // Extension point: send WhatsApp / SMS / email based on ea.RoutingKey

            if (_channel is not null)
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message [{RoutingKey}]", ea.RoutingKey);
            if (_channel is not null)
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
