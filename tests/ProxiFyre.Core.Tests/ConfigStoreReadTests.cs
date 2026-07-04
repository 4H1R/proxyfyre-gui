using ProxiFyre.Core.Config;
using Xunit;

public class ConfigStoreReadTests
{
    private static string WriteTemp(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Read_ParsesAllFields()
    {
        var path = WriteTemp("""
        {
          "logLevel": "Debug",
          "bypassLan": false,
          "proxies": [
            { "appNames": ["chrome","firefox"], "socks5ProxyEndpoint": "127.0.0.1:1080",
              "username": "u", "password": "p", "supportedProtocols": ["TCP","UDP"] }
          ],
          "excludes": ["edge"]
        }
        """);
        var cfg = new ConfigStore().Read(path);
        Assert.Equal("Debug", cfg.LogLevel);
        Assert.False(cfg.BypassLan);
        Assert.Single(cfg.Proxies);
        Assert.Equal("127.0.0.1:1080", cfg.Proxies[0].Socks5ProxyEndpoint);
        Assert.Equal(2, cfg.Proxies[0].AppNames.Count);
        Assert.Equal("u", cfg.Proxies[0].Username);
        Assert.Equal(new[] { "edge" }, cfg.Excludes);
        File.Delete(path);
    }
}
