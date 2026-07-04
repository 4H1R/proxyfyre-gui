namespace ProxiFyre.Core.Service;

public sealed class ServiceController : IServiceController
{
    private readonly IServiceHost _host;
    public ServiceController(IServiceHost host)
    {
        _host = host;
        CurrentState = host.Query();
    }

    public ServiceState CurrentState { get; private set; }

    public ServiceState Refresh() => CurrentState = _host.Query();

    public void Start()
    {
        if (CurrentState == ServiceState.NotInstalled) _host.Install();
        _host.Start();
        Refresh();
    }

    public void Stop()
    {
        if (CurrentState == ServiceState.Running) _host.Stop();
        Refresh();
    }

    public void Restart()
    {
        if (CurrentState == ServiceState.Running) _host.Stop();
        _host.Start();
        Refresh();
    }

    public void Uninstall()
    {
        if (CurrentState == ServiceState.Running) _host.Stop();
        _host.Uninstall();
        Refresh();
    }
}
