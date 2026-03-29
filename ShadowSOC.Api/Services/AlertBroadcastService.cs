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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _aiAgentUrl;
    private DateTime _lastAnalysis = DateTime.MinValue;
    private static readonly TimeSpan AnalysisCooldown = TimeSpan.FromSeconds(30);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await (_channel?.CloseAsync() ?? Task.CompletedTask);
        await (_connection?.CloseAsync() ?? Task.CompletedTask);
        await base.StopAsync(cancellationToken);
    }

    public AlertBroadcastService(
        IHubContext<AlertHub> hubContext,
        ILogger<AlertBroadcastService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _hubContext = hubContext;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _aiAgentUrl = configuration["AiAgent:Url"] ?? "http://localhost:8000";
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
            if (DateTime.UtcNow - _lastAnalysis >= AnalysisCooldown)
            {
                _lastAnalysis = DateTime.UtcNow;
                _ = AnalyzeAlertAsync(message, stoppingToken);
            }
        };
        await _channel.BasicConsumeAsync(queue: "alerts", autoAck: true, consumer: consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task AnalyzeAlertAsync(string alertMessage, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            var response = await client.PostAsJsonAsync($"{_aiAgentUrl}/analyze", new { alert = alertMessage }, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync(ct);
            await _hubContext.Clients.All.SendAsync("NewAnalysis", result, ct);
            _logger.LogInformation("AI analysis completed for alert");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("AI analysis timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("AI agent unreachable: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AI analysis failed: {Error}", ex.Message);
        }
    }
}