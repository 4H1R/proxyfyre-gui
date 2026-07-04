using System.Text;
using Microsoft.UI.Xaml;
using ProxiFyre.UI.Services;

namespace ProxiFyre.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private Window? _window;

    public App()
    {
        // Capture any startup crash to disk — an unhandled exception in OnLaunched
        // otherwise kills the process silently with no window and no console.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogCrash("AppDomain", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
            LogCrash("UnobservedTask", e.Exception);

        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            LogCrash("XamlUnhandled", e.Exception);
            e.Handled = true; // let the log flush; app will still likely close
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Services = AppServices.Build();
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            LogCrash("OnLaunched", ex);
            throw;
        }
    }

    private static void LogCrash(string source, Exception? ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProxiFyreUI");
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder()
                .AppendLine($"[{DateTime.Now:O}] {source}")
                .AppendLine(ex?.ToString() ?? "(no exception object)")
                .AppendLine(new string('-', 60));
            File.AppendAllText(Path.Combine(dir, "startup.log"), sb.ToString());
        }
        catch { /* logging must never throw */ }
    }
}
