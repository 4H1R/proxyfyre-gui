using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class RulesViewModelTests
{
    private sealed class CapturingStore : IConfigStore
    {
        public AppConfig Saved = new();
        public AppConfig ToLoad = new();
        public AppConfig Read(string p) => ToLoad;
        public void Write(string p, AppConfig c) => Saved = c;
        public ValidationResult Validate(AppConfig c) => new ConfigStore().Validate(c);
    }

    private static RulesViewModel Make(CapturingStore store) =>
        new(store, configPath: "cfg.json");

    [Fact]
    public void Load_PopulatesRulesFromConfig()
    {
        var store = new CapturingStore();
        store.ToLoad.Proxies.Add(new ProxyRule { Socks5ProxyEndpoint = "127.0.0.1:1080" });
        var vm = Make(store);
        vm.Load();
        Assert.Single(vm.Rules);
        Assert.Equal("127.0.0.1:1080", vm.Rules[0].Endpoint);
    }

    [Fact]
    public void AddRule_ThenSave_WritesConfig()
    {
        var store = new CapturingStore();
        var vm = Make(store);
        vm.Load();
        vm.AddRuleCommand.Execute(null);
        vm.Rules[0].Endpoint = "10.0.0.1:1080";
        vm.Rules[0].AppNamesText = "chrome, firefox";
        vm.Rules[0].Tcp = true; vm.Rules[0].Udp = false;
        vm.SaveCommand.Execute(null);

        Assert.Single(store.Saved.Proxies);
        Assert.Equal("10.0.0.1:1080", store.Saved.Proxies[0].Socks5ProxyEndpoint);
        Assert.Equal(new[] { "chrome", "firefox" }, store.Saved.Proxies[0].AppNames);
        Assert.Equal(new[] { "TCP" }, store.Saved.Proxies[0].SupportedProtocols);
    }

    [Fact]
    public void DeleteRule_RemovesIt()
    {
        var store = new CapturingStore();
        store.ToLoad.Proxies.Add(new ProxyRule { Socks5ProxyEndpoint = "127.0.0.1:1080" });
        var vm = Make(store);
        vm.Load();
        vm.DeleteRuleCommand.Execute(vm.Rules[0]);
        Assert.Empty(vm.Rules);
    }

    [Fact]
    public void Save_WithInvalidEndpoint_SetsErrorAndDoesNotWrite()
    {
        var store = new CapturingStore();
        var vm = Make(store);
        vm.Load();
        vm.AddRuleCommand.Execute(null);
        vm.Rules[0].Endpoint = "bad";
        vm.SaveCommand.Execute(null);
        Assert.False(string.IsNullOrEmpty(vm.ErrorText));
        Assert.Empty(store.Saved.Proxies); // untouched
    }
}
