using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Service;

namespace ProxiFyre.Core.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IServiceController _controller;

    public ShellViewModel(IServiceController controller)
    {
        _controller = controller;
        RefreshState();
    }

    [ObservableProperty] private string _statusText = "Unknown";
    [ObservableProperty] private bool _isRunning;

    public void RefreshState()
    {
        var state = _controller.Refresh();
        IsRunning = state == ServiceState.Running;
        StatusText = state switch
        {
            ServiceState.Running => "Running",
            ServiceState.Stopped => "Stopped",
            ServiceState.NotInstalled => "Not installed",
            _ => "Unknown"
        };
    }

    [RelayCommand] private void Start() { _controller.Start(); RefreshState(); }
    [RelayCommand] private void Stop() { _controller.Stop(); RefreshState(); }
    [RelayCommand] private void Restart() { _controller.Restart(); RefreshState(); }
}
