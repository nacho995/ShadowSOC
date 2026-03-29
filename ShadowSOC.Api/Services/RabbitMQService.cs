using System.Text;
using System.Text.Json;
using ConnectionFactory = RabbitMQ.Client.ConnectionFactory;
using ShadowSOC.Shared.Models;
using RabbitMQ.Client;
namespace ShadowSOC.Api.Services;

public class RabbitMQService : IAsyncDisposable
{
    private IConnection? _connection;
    private readonly ConnectionFactory _alerts;
    private IChannel? _channel;
    public async ValueTask DisposeAsync()
    {
        await (_channel?.CloseAsync() ?? Task.CompletedTask);
        await (_connection?.CloseAsync() ?? Task.CompletedTask);
    }

    private async Task InitializeAsync()
    {
       if (_connection != null) return;
        _connection = await _alerts.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        await _channel.QueueDeclareAsync(
            queue: "alerts",                                                                                                                
            durable: false, 
            exclusive: false,
            autoDelete: false,
            arguments: null                                                                                                                 
        ); 
        }
    public RabbitMQService()
    {
       _alerts = new ConnectionFactory { Uri = new Uri(Environment.GetEnvironmentVariable("RABBITMQ_URL") ?? "amqp://guest:guest@localhost:5672") };
    }
    public async Task PublishAlertAsync(SecurityEvent securityEvent)
    {
        await InitializeAsync();
        var json = JsonSerializer.Serialize(securityEvent);
        var body =Encoding.UTF8.GetBytes(json);
        await _channel.BasicPublishAsync(                                                                                                    
            exchange: "",                                                                                                                   
            routingKey: "alerts",                                                                                                           
            mandatory: false,                                                                                                               
            basicProperties: new RabbitMQ.Client.BasicProperties(),                                                                         
            body: body                                                                                                                      
        );       
        
    }
}