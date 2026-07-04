using ProxiFyre.Core.Service;
using Xunit;

public class ServiceControllerTests
{
    private sealed class FakeHost : IServiceHost
    {
        public ServiceState State = ServiceState.NotInstalled;
        public List<string> Calls = new();
        public ServiceState Query() => State;
        public void Install() { Calls.Add("install"); State = ServiceState.Stopped; }
        public void Uninstall() { Calls.Add("uninstall"); State = ServiceState.NotInstalled; }
        public void Start() { Calls.Add("start"); State = ServiceState.Running; }
        public void Stop() { Calls.Add("stop"); State = ServiceState.Stopped; }
    }

    [Fact]
    public void Start_WhenNotInstalled_InstallsThenStarts()
    {
        var host = new FakeHost { State = ServiceState.NotInstalled };
        var ctl = new ServiceController(host);
        ctl.Start();
        Assert.Equal(new[] { "install", "start" }, host.Calls);
        Assert.Equal(ServiceState.Running, ctl.CurrentState);
    }

    [Fact]
    public void Start_WhenStopped_JustStarts()
    {
        var host = new FakeHost { State = ServiceState.Stopped };
        var ctl = new ServiceController(host);
        ctl.Start();
        Assert.Equal(new[] { "start" }, host.Calls);
    }

    [Fact]
    public void Restart_StopsThenStarts()
    {
        var host = new FakeHost { State = ServiceState.Running };
        var ctl = new ServiceController(host);
        ctl.Restart();
        Assert.Equal(new[] { "stop", "start" }, host.Calls);
    }
}
