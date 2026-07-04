using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Logs;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class LogsPage : Page
{
    public LogsViewModel Vm { get; } = new();
    private readonly ILocatorService _locator;
    private LogTailer? _tailer;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public LogsPage()
    {
        _locator = App.Services.GetRequiredService<ILocatorService>();
        InitializeComponent();
        _timer.Tick += (_, _) => Poll();
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Poll()
    {
        var file = NewestLog();
        if (file is null) return;
        _tailer ??= new LogTailer(file);
        foreach (var line in _tailer.ReadNew()) Vm.Append(line);
    }

    private string? NewestLog()
    {
        if (!Directory.Exists(_locator.LogsDir)) return null;
        return new DirectoryInfo(_locator.LogsDir)
            .GetFiles("*.log").OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault()?.FullName;
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(_locator.LogsDir))
            Process.Start(new ProcessStartInfo(_locator.LogsDir) { UseShellExecute = true });
    }
}
