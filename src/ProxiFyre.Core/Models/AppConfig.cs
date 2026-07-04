using System.Text.Json.Serialization;

namespace ProxiFyre.Core.Models;

public sealed class AppConfig
{
    [JsonPropertyName("logLevel")] public string LogLevel { get; set; } = "Error";
    [JsonPropertyName("bypassLan")] public bool BypassLan { get; set; } = true;
    [JsonPropertyName("proxies")] public List<ProxyRule> Proxies { get; set; } = new();
    [JsonPropertyName("excludes")] public List<string> Excludes { get; set; } = new();
}

public sealed class ProxyRule
{
    [JsonPropertyName("appNames")] public List<string> AppNames { get; set; } = new();
    [JsonPropertyName("socks5ProxyEndpoint")] public string Socks5ProxyEndpoint { get; set; } = "";
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("password")] public string? Password { get; set; }
    [JsonPropertyName("supportedProtocols")] public List<string> SupportedProtocols { get; set; } = new() { "TCP", "UDP" };
}
