using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class ExcludesViewModelTests
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
    public void Load_PopulatesExcludesAndBypassLan()
    {
        var store = new Store { ToLoad = new AppConfig { BypassLan = false } };
        store.ToLoad.Excludes.Add("edge");
        var vm = new ExcludesViewModel(store, "cfg.json");
        vm.Load();
        Assert.Contains("edge", vm.Excludes);
        Assert.False(vm.BypassLan);
    }

    [Fact]
    public void AddAndSave_PersistsExcludesAndBypassLan()
    {
        var store = new Store();
        var vm = new ExcludesViewModel(store, "cfg.json");
        vm.Load();
        vm.NewExclude = "chrome.exe";
        vm.AddCommand.Execute(null);
        vm.BypassLan = true;
        vm.SaveCommand.Execute(null);
        Assert.Contains("chrome.exe", store.Saved.Excludes);
        Assert.True(store.Saved.BypassLan);
    }
}
