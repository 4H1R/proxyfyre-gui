using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Models;
using ProxiFyre.Core.Prereq;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class DashboardViewModelTests
{
    private sealed class FakeChecker : IPrereqChecker
    {
        public PrereqStatus Status = new() { DriverInstalled = true, RuntimeInstalled = true };
        public PrereqStatus Check() => Status;
    }
    private sealed class FakeStore : IConfigStore
    {
        public AppConfig Config = new();
        public AppConfig Read(string p) => Config;
        public void Write(string p, AppConfig c) { }
        public ValidationResult Validate(AppConfig c) => new();
    }
    private sealed class FakeLocator : ILocatorService
    {
        public string ExePath => "x"; public string ConfigPath => "c"; public string LogsDir => "l";
        public bool ExeExists => true; public bool ConfigExists => true;
    }

    [Fact]
    public void Load_SetsRuleCountAndPrereqReady()
    {
        var store = new FakeStore();
        store.Config.Proxies.Add(new ProxyRule());
        store.Config.Proxies.Add(new ProxyRule());
        var vm = new DashboardViewModel(new FakeChecker(), store, new FakeLocator());
        vm.Load();
        Assert.Equal(2, vm.RuleCount);
        Assert.True(vm.PrereqReady);
        Assert.Empty(vm.MissingPrereqs);
    }

    [Fact]
    public void Load_MissingPrereq_SetsNotReady()
    {
        var checker = new FakeChecker { Status = new PrereqStatus { DriverInstalled = false } };
        checker.Status.Missing.Add("Windows Packet Filter (WinpkFilter) driver");
        var vm = new DashboardViewModel(checker, new FakeStore(), new FakeLocator());
        vm.Load();
        Assert.False(vm.PrereqReady);
        Assert.NotEmpty(vm.MissingPrereqs);
    }

    [Fact]
    public void Recheck_ReflectsNewlyInstalledPrereqs()
    {
        var checker = new FakeChecker
        {
            Status = new PrereqStatus { DriverInstalled = false, RuntimeInstalled = true }
        };
        checker.Status.Missing.Add("Windows Packet Filter (WinpkFilter) driver");
        var vm = new DashboardViewModel(checker, new FakeStore(), new FakeLocator());
        vm.Load();
        Assert.False(vm.PrereqReady);

        checker.Status = new PrereqStatus { DriverInstalled = true, RuntimeInstalled = true };
        vm.RecheckCommand.Execute(null);
        Assert.True(vm.PrereqReady);
    }
}
