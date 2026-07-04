using ProxiFyre.Core.Models;
using Xunit;

public class AppConfigTests
{
    [Fact]
    public void NewConfig_HasEmptyCollections_AndDefaults()
    {
        var cfg = new AppConfig();
        Assert.Equal("Error", cfg.LogLevel);
        Assert.True(cfg.BypassLan);
        Assert.Empty(cfg.Proxies);
        Assert.Empty(cfg.Excludes);
    }

    [Fact]
    public void ProxyRule_DefaultsAreEmptyNotNull()
    {
        var rule = new ProxyRule();
        Assert.NotNull(rule.AppNames);
        Assert.NotNull(rule.SupportedProtocols);
        Assert.Equal("", rule.Socks5ProxyEndpoint);
    }
}
