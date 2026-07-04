using System.Linq;
using System.Text.Json;
using ProxiFyre.Core.Models;

namespace ProxiFyre.Core.Config;

public sealed class ConfigStore : IConfigStore
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AppConfig Read(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, ReadOpts) ?? new AppConfig();
    }

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly HashSet<string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "logLevel", "bypassLan", "proxies", "excludes"
    };

    public void Write(string path, AppConfig config)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(
            JsonSerializer.Serialize(config, WriteOpts))!.AsObject();

        if (File.Exists(path))
        {
            var existing = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))
                as System.Text.Json.Nodes.JsonObject;
            if (existing is not null)
                foreach (var kv in existing)
                    if (!KnownKeys.Contains(kv.Key) && !node.ContainsKey(kv.Key))
                        node[kv.Key] = kv.Value?.DeepClone();
        }

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, node.ToJsonString(WriteOpts));
        File.Copy(tmp, path, overwrite: true);   // write-then-swap: never lose the original on crash
        File.Delete(tmp);
    }

    public ValidationResult Validate(AppConfig config)
    {
        var r = new ValidationResult();
        for (var i = 0; i < config.Proxies.Count; i++)
        {
            var p = config.Proxies[i];
            var label = $"rule #{i + 1}";

            if (!IsValidEndpoint(p.Socks5ProxyEndpoint))
                r.Errors.Add($"{label}: invalid SOCKS5 endpoint '{p.Socks5ProxyEndpoint}' (expected host:port).");
            if (p.SupportedProtocols.Count == 0)
                r.Errors.Add($"{label}: at least one protocol (TCP/UDP) is required.");
            if (p.AppNames.Count == 0 || p.AppNames.All(string.IsNullOrWhiteSpace))
                r.Warnings.Add($"{label}: no app names — this rule matches all apps (catch-all).");
        }
        return r;
    }

    private static bool IsValidEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return false;
        var idx = endpoint.LastIndexOf(':');
        if (idx <= 0 || idx == endpoint.Length - 1) return false;
        var host = endpoint[..idx];
        var portText = endpoint[(idx + 1)..];
        if (string.IsNullOrWhiteSpace(host)) return false;
        return int.TryParse(portText, out var port) && port is > 0 and <= 65535;
    }
}
