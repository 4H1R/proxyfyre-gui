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

    public ValidationResult Validate(AppConfig config) => throw new NotImplementedException();
}
