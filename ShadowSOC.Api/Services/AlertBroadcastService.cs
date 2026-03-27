using System.Text;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ShadowSOC.Api.Hubs;

namespace ShadowSOC.Api.Services;

public class AlertBroadcastService : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly IHubContext<AlertHub> _hubContext;
    private readonly ILogger<AlertBroadcastService> _logger;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await (_channel?.CloseAsync() ?? Task.CompletedTask);
        await (_connection?.CloseAsync() ?? Task.CompletedTask);
        await base.StopAsync(cancellationToken);
    }
    public AlertBroadcastService(IHubContext<AlertHub> hubContext, ILogger<AlertBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(Environment.GetEnvironmentVariable("RABBITMQ_URL") ?? "amqp://guest:guest@localhost:5672") };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        await _channel.QueueDeclareAsync(queue: "alerts", durable: false, exclusive: false, autoDelete: false,
            arguments: null);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogInformation("Broadcasting alert via SignalR");
            await _hubContext.Clients.All.SendAsync("NewAlert", message, stoppingToken);
        };
        await _channel.BasicConsumeAsync(queue: "alerts", autoAck: true, consumer: consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}