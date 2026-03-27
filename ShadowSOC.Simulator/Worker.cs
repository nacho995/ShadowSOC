using Confluent.Kafka;
using ShadowSOC.Shared.Models;
using System.Text.Json;

namespace ShadowSOC.Simulator;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    private static readonly Random _random = new();

    private static readonly string[] _attacks =
        { "Brute Force", "Port Scan", "SQL Injection", "DDoS", "XSS", "RCE", "SSH Exploit", "Directory Traversal" };

    private static readonly int[] _destinationPorts = { 80, 443, 22, 21, 25, 3389, 8080, 3306 };

    private static readonly string[] _destinationIp =
        { "192.168.1.1", "192.168.1.2", "192.168.1.3", "10.0.0.1", "10.0.0.2" };

    private static readonly string[] _mitreIDs =
        { "T1043", "T1071", "T1090", "T1100", "T1133", "T1110", "T1046", "T1190" };

    // Fallback IPs from known malicious sources (used when AbuseIPDB rate-limited)
    private static readonly string[] _fallbackIps =
    {
        "45.33.32.156", "185.220.101.34", "103.235.46.39", "77.88.55.242",
        "200.174.2.96", "41.231.53.70", "175.45.176.1", "5.2.69.50",
        "91.198.174.192", "154.118.230.83", "222.186.15.96", "61.177.172.136",
        "218.92.0.107", "112.85.42.88", "185.156.73.54", "45.148.10.174",
        "193.32.162.159", "80.82.77.139", "71.6.146.185", "89.248.167.131"
    };

    private List<string> _realIps = new();
    private DateTime _lastFetchTime = DateTime.MinValue;

    private async Task FetchRealIps(HttpClient http, CancellationToken ct)
    {
        try
        {
            var response = await http.GetStringAsync(
                "https://api.abuseipdb.com/api/v2/blacklist?confidenceMinimum=90&limit=50", ct);

            var doc = JsonDocument.Parse(response);
            var data = doc.RootElement.GetProperty("data");

            var ips = new List<string>();
            foreach (var item in data.EnumerateArray())
            {
                var ip = item.GetProperty("ipAddress").GetString();
                if (!string.IsNullOrEmpty(ip))
                    ips.Add(ip);
            }

            if (ips.Count > 0)
            {
                _realIps = ips;
                logger.LogInformation("Fetched {Count} malicious IPs from AbuseIPDB", _realIps.Count);
            }
            else
            {
                logger.LogWarning("AbuseIPDB returned no IPs, keeping existing cache");
            }

            _lastFetchTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError("Error fetching IPs from AbuseIPDB: {Error}", ex.Message);
            if (_realIps.Count == 0)
            {
                _realIps = _fallbackIps.ToList();
                logger.LogInformation("Using {Count} fallback IPs", _realIps.Count);
            }
            _lastFetchTime = DateTime.UtcNow;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var apiKey = configuration["AbuseIPDB:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Missing required configuration: AbuseIPDB:ApiKey. Configure it with: dotnet user-secrets set \"AbuseIPDB:ApiKey\"      \"<your-key>\" --project ShadowSOC.Simulator"         );
        }
        var kafkaConfig = new ProducerConfig
        {
            BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
        };
        var kafkaUser = Environment.GetEnvironmentVariable("KAFKA_USERNAME");
        if (!string.IsNullOrEmpty(kafkaUser))
        {
            kafkaConfig.SecurityProtocol = SecurityProtocol.SaslSsl;
            kafkaConfig.SaslMechanism = SaslMechanism.ScramSha256;
            kafkaConfig.SaslUsername = kafkaUser;
            kafkaConfig.SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD");
        }
        var producer = new ProducerBuilder<string, string>(kafkaConfig).Build();

        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Key", apiKey);
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        // Fetch IPs on startup
        await FetchRealIps(http, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Refresh cache every 5 minutes
            if (DateTime.UtcNow - _lastFetchTime >= TimeSpan.FromMinutes(5))
            {
                await FetchRealIps(http, stoppingToken);
            }

            if (_realIps.Count == 0)
            {
                logger.LogWarning("No cached IPs available, retrying fetch in 30 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                continue;
            }

            try
            {
                var ip = _realIps[_random.Next(_realIps.Count)];

                // Weighted severity: ~40% Critical, ~35% High, ~15% Medium, ~10% Low
                var severityRoll = _random.Next(100);
                var severity = severityRoll switch
                {
                    < 40 => Severity.Critical,
                    < 75 => Severity.High,
                    < 90 => Severity.Medium,
                    _ => Severity.Low
                };

                var securityEvent = new SecurityEvent
                {
                    Id = Guid.NewGuid(),
                    OriginIp = ip,
                    DestinationIp = _destinationIp[_random.Next(_destinationIp.Length)],
                    TypeOfAttack = _attacks[_random.Next(_attacks.Length)],
                    Severity = severity,
                    Protocol = (Protocol)_random.Next(3),
                    SourcePort = _random.Next(1024, 65535),
                    DestinationPort = _destinationPorts[_random.Next(_destinationPorts.Length)],
                    MitreID = _mitreIDs[_random.Next(_mitreIDs.Length)],
                    WhenStarted = DateTime.UtcNow,
                    WhenEnded = DateTime.UtcNow.AddSeconds(_random.Next(1, 60))
                };

                await producer.ProduceAsync("security-events", new Message<string, string>
                {
                    Key = securityEvent.Id.ToString(),
                    Value = JsonSerializer.Serialize(securityEvent)
                }, stoppingToken);

                logger.LogInformation("Event: {Ip} -> {Attack} [{Severity}]", ip, securityEvent.TypeOfAttack,
                    securityEvent.Severity);
            }
            catch (Exception ex)
            {
                logger.LogError("Error producing event: {Error}", ex.Message);
            }

            // Random delay between 2-5 seconds
            var delayMs = _random.Next(2000, 5001);
            await Task.Delay(delayMs, stoppingToken);
        }
    }
}
