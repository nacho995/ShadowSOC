using System.Text;
using RabbitMQ.Client;
using ShadowSOC.Shared.Models;
using System.Text.Json;
using Confluent.Kafka;


namespace ShadowSOC.Consumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _http = new();
    private readonly Dictionary<string, (double lat, double lon, string country)> _geoCache = new();
    private IConnection _connection;
    private IChannel _channel;
    private IConsumer<string, string>? _kafkaConsumer;
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
                                                                                                                                     
        var factory = new ConnectionFactory { Uri = new Uri(Environment.GetEnvironmentVariable("RABBITMQ_URL") ?? "amqp://guest:guest@localhost:5672") };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
       
        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
            GroupId = "detection-service-v2",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        var kafkaUser = Environment.GetEnvironmentVariable("KAFKA_USERNAME");
        if (!string.IsNullOrEmpty(kafkaUser))
        {
            kafkaConfig.SecurityProtocol = SecurityProtocol.SaslSsl;
            kafkaConfig.SaslMechanism = SaslMechanism.ScramSha256;
            kafkaConfig.SaslUsername = kafkaUser;
            kafkaConfig.SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD");
        }
        _kafkaConsumer = new ConsumerBuilder<string, string>(kafkaConfig).Build();
        _kafkaConsumer.Subscribe("security-events");
        await _channel.QueueDeclareAsync(                                                                                                
            queue: "alerts",                                                                                                            
            durable: false,                                                                                                             
            exclusive: false,                                                                                                           
            autoDelete: false,                                                                                                          
            arguments: null
        );      

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _kafkaConsumer.Consume(stoppingToken);
            var securityEvent = JsonSerializer.Deserialize<SecurityEvent>(result.Message.Value);
            if (securityEvent.Severity >= Severity.High || securityEvent.DestinationPort == 22)
            {
                await GeolocateAsync(securityEvent);
                var json = JsonSerializer.Serialize(securityEvent);
                var body = Encoding.UTF8.GetBytes(json);
                await _channel.BasicPublishAsync( "", "alerts", false, new BasicProperties(), body);
                _logger.LogInformation("ALERTA: {attack} desde {ip} ({country})", securityEvent.TypeOfAttack, securityEvent.OriginIp, securityEvent.Country);
            }
        }
 

    }
    public override async Task StopAsync(CancellationToken cancellationToken)                                  
    {
        _kafkaConsumer?.Close();                                                                               
        await (_channel?.CloseAsync() ?? Task.CompletedTask);
        await (_connection?.CloseAsync() ?? Task.CompletedTask);                                               
        await base.StopAsync(cancellationToken);
    }    

    private async Task GeolocateAsync(SecurityEvent securityEvent)
    {
        try
        {
            if (_geoCache.TryGetValue(securityEvent.OriginIp, out var cached))
            {
                securityEvent.Latitude = cached.lat;
                securityEvent.Longitude = cached.lon;
                securityEvent.Country = cached.country;
                return;
            }

            var response = await _http.GetStringAsync($"http://ip-api.com/json/{securityEvent.OriginIp}");
            var json = JsonDocument.Parse(response);
            var lat = json.RootElement.GetProperty("lat").GetDouble();
            var lon = json.RootElement.GetProperty("lon").GetDouble();
            var country = json.RootElement.GetProperty("country").GetString() ?? "Unknown";

            securityEvent.Latitude = lat;
            securityEvent.Longitude = lon;
            securityEvent.Country = country;

            _geoCache[securityEvent.OriginIp] = (lat, lon, country);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Geolocation failed for {Ip}: {Error}", securityEvent.OriginIp, ex.Message);
            securityEvent.Country = "Unknown";
            // Cache failed IPs too so we don't keep retrying
            _geoCache[securityEvent.OriginIp] = (0, 0, "Unknown");
        }
    }
}
