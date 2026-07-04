namespace ProxiFyre.Core.Locate;

public sealed class LocatorService : ILocatorService
{
    public LocatorService(string baseDir)
    {
        ExePath = Path.Combine(baseDir, "ProxiFyre.exe");
        ConfigPath = Path.Combine(baseDir, "app-config.json");
        LogsDir = Path.Combine(baseDir, "logs");
    }

    public LocatorService() : this(AppContext.BaseDirectory) { }

    public string ExePath { get; }
    public string ConfigPath { get; }
    public string LogsDir { get; }
    public bool ExeExists => File.Exists(ExePath);
    public bool ConfigExists => File.Exists(ConfigPath);
}
