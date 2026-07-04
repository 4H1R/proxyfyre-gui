using Microsoft.UI.Xaml;
using ProxiFyre.UI.Services;

namespace ProxiFyre.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = AppServices.Build();
        _window = new MainWindow();
        _window.Activate();
    }
}
