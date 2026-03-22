using System.Text;
using System.Text.Json;
using ConnectionFactory = RabbitMQ.Client.ConnectionFactory;
using ShadowSOC.Shared.Models;

namespace ShadowSOC.Api.Services;

public class RabbitMQService
{
    private ConnectionFactory _alerts;

    public RabbitMQService()
    {
       _alerts = new ConnectionFactory { Uri = new Uri(Environment.GetEnvironmentVariable("RABBITMQ_URL") ?? "amqp://guest:guest@localhost:5672") };
    }
    public async Task PublishAlertAsync(SecurityEvent securityEvent)
    {
        var conection = await _alerts.CreateConnectionAsync();
        var channel = await conection.CreateChannelAsync();
        
        await channel.QueueDeclareAsync(
            queue: "alerts",                                                                                                                
            durable: false, 
            exclusive: false,
            autoDelete: false,
            arguments: null                                                                                                                 
        ); 
        var json = JsonSerializer.Serialize(securityEvent);
        var body =Encoding.UTF8.GetBytes(json);
        await channel.BasicPublishAsync(                                                                                                    
            exchange: "",                                                                                                                   
            routingKey: "alerts",                                                                                                           
            mandatory: false,                                                                                                               
            basicProperties: new RabbitMQ.Client.BasicProperties(),                                                                         
            body: body                                                                                                                      
        );       
        
    }
}