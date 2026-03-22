using System.Text;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ShadowSOC.Api.Hubs;

namespace ShadowSOC.Api.Services;

public class AlertBroadcastService : BackgroundService
{
    private readonly IHubContext<AlertHub> _hubContext;
    private readonly ILogger<AlertBroadcastService> _logger;

    public AlertBroadcastService(IHubContext<AlertHub> hubContext, ILogger<AlertBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // aquí: conectar a RabbitMQ, declarar cola "alerts", escuchar
        var factory = new ConnectionFactory { HostName = "localhost" };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue: "alerts", durable: false, exclusive: false, autoDelete: false,
            arguments: null);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogInformation("Broadcasting alert via SignalR");
            await _hubContext.Clients.All.SendAsync("NewAlert", message, stoppingToken);
        };
        await channel.BasicConsumeAsync(queue: "alerts", autoAck: true, consumer: consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}