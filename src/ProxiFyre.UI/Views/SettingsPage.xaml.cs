using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel Vm { get; }
    public SettingsPage()
    {
        var store = App.Services.GetRequiredService<IConfigStore>();
        var locator = App.Services.GetRequiredService<ILocatorService>();
        Vm = new SettingsViewModel(store, locator.ConfigPath);
        InitializeComponent();
        Vm.Load();
    }

    private void Autostart_Toggled(object sender, RoutedEventArgs e)
    {
        var mode = AutostartSwitch.IsOn ? "auto" : "demand";
        var psi = new ProcessStartInfo("sc.exe", $"config ProxiFyre start= {mode}")
        {
            UseShellExecute = false, CreateNoWindow = true
        };
        try { Process.Start(psi)?.WaitForExit(10_000); } catch { /* surfaced on smoke host */ }
    }
}
