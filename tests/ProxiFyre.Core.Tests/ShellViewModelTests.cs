using ProxiFyre.Core.Service;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class ShellViewModelTests
{
    private sealed class FakeController : IServiceController
    {
        public ServiceState CurrentState { get; set; } = ServiceState.Stopped;
        public int Starts, Stops;
        public ServiceState Refresh() => CurrentState;
        public void Start() { Starts++; CurrentState = ServiceState.Running; }
        public void Stop() { Stops++; CurrentState = ServiceState.Stopped; }
        public void Restart() { Stops++; Starts++; }
        public void Uninstall() { CurrentState = ServiceState.NotInstalled; }
    }

    [Fact]
    public void StatusText_ReflectsState()
    {
        var vm = new ShellViewModel(new FakeController { CurrentState = ServiceState.Running });
        vm.RefreshState();
        Assert.Equal("Running", vm.StatusText);
        Assert.True(vm.IsRunning);
    }

    [Fact]
    public void StartCommand_StartsService_AndUpdatesStatus()
    {
        var ctl = new FakeController { CurrentState = ServiceState.Stopped };
        var vm = new ShellViewModel(ctl);
        vm.StartCommand.Execute(null);
        Assert.Equal(1, ctl.Starts);
        Assert.Equal("Running", vm.StatusText);
    }
}
