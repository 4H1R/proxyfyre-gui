using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class SettingsViewModelTests
{
    private sealed class Store : IConfigStore
    {
        public AppConfig ToLoad = new();
        public AppConfig Saved = new();
        public AppConfig Read(string p) => ToLoad;
        public void Write(string p, AppConfig c) => Saved = c;
        public ValidationResult Validate(AppConfig c) => new();
    }

    [Fact]
    public void Load_ReadsLogLevel()
    {
        var store = new Store { ToLoad = new AppConfig { LogLevel = "Debug" } };
        var vm = new SettingsViewModel(store, "cfg.json");
        vm.Load();
        Assert.Equal("Debug", vm.LogLevel);
    }

    [Fact]
    public void Save_PersistsLogLevel_PreservingOtherFields()
    {
        var store = new Store();
        store.ToLoad.Proxies.Add(new ProxyRule { Socks5ProxyEndpoint = "127.0.0.1:1080" });
        var vm = new SettingsViewModel(store, "cfg.json");
        vm.Load();
        vm.LogLevel = "Warning";
        vm.SaveCommand.Execute(null);
        Assert.Equal("Warning", store.Saved.LogLevel);
        Assert.Single(store.Saved.Proxies); // preserved
    }
}
