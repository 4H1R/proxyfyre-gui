using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Config;

namespace ProxiFyre.Core.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigStore _store;
    private readonly string _configPath;

    public SettingsViewModel(IConfigStore store, string configPath)
    {
        _store = store; _configPath = configPath;
    }

    public string[] LogLevels { get; } = { "Error", "Warning", "Info", "Debug", "All" };
    [ObservableProperty] private string _logLevel = "Error";

    public void Load() => LogLevel = _store.Read(_configPath).LogLevel;

    [RelayCommand]
    private void Save()
    {
        var cfg = _store.Read(_configPath);
        cfg.LogLevel = LogLevel;
        _store.Write(_configPath, cfg);
    }
}
