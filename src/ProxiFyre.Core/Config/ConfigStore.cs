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

    public void Write(string path, AppConfig config) => throw new NotImplementedException();
    public ValidationResult Validate(AppConfig config) => throw new NotImplementedException();
}
