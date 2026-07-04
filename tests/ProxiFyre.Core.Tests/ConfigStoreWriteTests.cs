using System.Text.Json;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using Xunit;

public class ConfigStoreWriteTests
{
    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

    [Fact]
    public void Write_RoundTrips()
    {
        var store = new ConfigStore();
        var cfg = new AppConfig { LogLevel = "Info", BypassLan = false };
        cfg.Proxies.Add(new ProxyRule { Socks5ProxyEndpoint = "127.0.0.1:1080" });
        var path = TempPath();

        store.Write(path, cfg);
        var back = store.Read(path);

        Assert.Equal("Info", back.LogLevel);
        Assert.False(back.BypassLan);
        Assert.Single(back.Proxies);
        File.Delete(path);
    }

    [Fact]
    public void Write_PreservesUnknownTopLevelFields()
    {
        var path = TempPath();
        File.WriteAllText(path, """
        { "logLevel": "Error", "bypassLan": true, "proxies": [], "excludes": [],
          "futureFlag": "keepme" }
        """);
        var store = new ConfigStore();
        var cfg = store.Read(path);
        cfg.LogLevel = "Debug";

        store.Write(path, cfg);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal("Debug", doc.RootElement.GetProperty("logLevel").GetString());
        Assert.Equal("keepme", doc.RootElement.GetProperty("futureFlag").GetString());
        File.Delete(path);
    }

    [Fact]
    public void Write_LeavesNoTempFile()
    {
        var path = TempPath();
        var store = new ConfigStore();
        store.Write(path, new AppConfig());
        Assert.False(File.Exists(path + ".tmp"));
        File.Delete(path);
    }
}
