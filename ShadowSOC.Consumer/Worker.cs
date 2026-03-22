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

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
                                                                                                                                     
        var factory = new ConnectionFactory { HostName = "localhost" };
        var connection = await factory.CreateConnectionAsync();                                                                         
        var channel = await connection.CreateChannelAsync();
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "detection-service-v2",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe("security-events");
        await channel.QueueDeclareAsync(                                                                                                
            queue: "alerts",                                                                                                            
            durable: false,                                                                                                             
            exclusive: false,                                                                                                           
            autoDelete: false,                                                                                                          
            arguments: null
        );      

        while (!stoppingToken.IsCancellationRequested)
        {
            // leer de kafka
            var result = consumer.Consume(stoppingToken);
            var securityEvent = JsonSerializer.Deserialize<SecurityEvent>(result.Message.Value);

            if (securityEvent.Severity >= Severity.High || securityEvent.DestinationPort == 22)
            {
                //publicar en rabbitmq
                await GeolocateAsync(securityEvent);
                var json = JsonSerializer.Serialize(securityEvent);
                var body = Encoding.UTF8.GetBytes(json);
                await channel.BasicPublishAsync( "", "alerts", false, new BasicProperties(), body);
                _logger.LogInformation("ALERTA: {attack} desde {ip} ({country})", securityEvent.TypeOfAttack, securityEvent.OriginIp, securityEvent.Country);
            }
        }


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
