using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using Xunit;

public class ConfigStoreValidateTests
{
    private static AppConfig Valid()
    {
        var c = new AppConfig();
        c.Proxies.Add(new ProxyRule
        {
            AppNames = { "chrome" },
            Socks5ProxyEndpoint = "127.0.0.1:1080",
            SupportedProtocols = { "TCP" }
        });
        return c;
    }

    [Fact]
    public void Valid_Config_HasNoErrors()
    {
        var r = new ConfigStore().Validate(Valid());
        Assert.True(r.IsValid);
        Assert.Empty(r.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nohost")]
    [InlineData("127.0.0.1:notaport")]
    [InlineData("127.0.0.1:99999")]
    public void Invalid_Endpoint_IsError(string endpoint)
    {
        var c = Valid();
        c.Proxies[0].Socks5ProxyEndpoint = endpoint;
        var r = new ConfigStore().Validate(c);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("endpoint", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EmptyProtocols_IsError()
    {
        var c = Valid();
        c.Proxies[0].SupportedProtocols.Clear();
        var r = new ConfigStore().Validate(c);
        Assert.False(r.IsValid);
    }

    [Fact]
    public void EmptyAppNames_IsWarningNotError()
    {
        var c = Valid();
        c.Proxies[0].AppNames.Clear();
        var r = new ConfigStore().Validate(c);
        Assert.True(r.IsValid);
        Assert.NotEmpty(r.Warnings);
    }
}
