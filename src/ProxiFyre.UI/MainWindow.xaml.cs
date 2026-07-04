using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.ViewModels;
using ProxiFyre.UI.Views;

namespace ProxiFyre.UI;

public sealed partial class MainWindow : Window
{
    public ShellViewModel ShellVm { get; }

    public MainWindow()
    {
        ShellVm = App.Services.GetRequiredService<ShellViewModel>();
        InitializeComponent();
        Title = "ProxiFyre";
        // x:Bind converters can't resolve StaticResources on a Window (Window isn't a
        // FrameworkElement), so drive the status dot from code-behind instead.
        ShellVm.PropertyChanged += OnShellPropertyChanged;
        UpdateStatusDot();
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.IsRunning)) UpdateStatusDot();
    }

    private void UpdateStatusDot() =>
        StatusDot.Fill = new SolidColorBrush(ShellVm.IsRunning ? Colors.LimeGreen : Colors.Gray);

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        var page = tag switch
        {
            "dashboard" => typeof(DashboardPage),
            "rules"     => typeof(RulesPage),
            "excludes"  => typeof(ExcludesPage),
            "logs"      => typeof(LogsPage),
            "settings"  => typeof(SettingsPage),
            _           => typeof(DashboardPage)
        };
        ContentFrame.Navigate(page);
    }
}
