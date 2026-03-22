using System.Text.Json.Serialization;

namespace ShadowSOC.Shared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Severity
{
    Low,
    Medium,
    High,
    Critical
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Protocol
{
    Tcp,
    Udp,
    Icmp
}

public class SecurityEvent
{
    public Guid Id { get; set; }
    public int SourcePort { get; set; }
    public int DestinationPort { get; set; }
    public string MitreID {get; set; }
    public Protocol Protocol {get ; set; }
    public string OriginIp { get; set; }
    public string DestinationIp { get; set; }
    public string TypeOfAttack { get; set; }
    public Severity Severity {get; set; }
    public DateTime WhenStarted { get; set; }
    public DateTime WhenEnded { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Country { get; set; }
}