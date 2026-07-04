namespace ProxiFyre.Core.Service;

public interface IServiceHost
{
    ServiceState Query();
    void Install();
    void Uninstall();
    void Start();
    void Stop();
}

public interface IServiceController
{
    ServiceState CurrentState { get; }
    ServiceState Refresh();
    void Start();
    void Stop();
    void Restart();
    void Uninstall();
}
