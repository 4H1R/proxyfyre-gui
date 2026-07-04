namespace ProxiFyre.Core.Locate;

public interface ILocatorService
{
    string ExePath { get; }
    string ConfigPath { get; }
    string LogsDir { get; }
    bool ExeExists { get; }
    bool ConfigExists { get; }
}
