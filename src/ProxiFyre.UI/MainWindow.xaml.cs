using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        ContentFrame.Navigate(typeof(DashboardPage));
    }

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
