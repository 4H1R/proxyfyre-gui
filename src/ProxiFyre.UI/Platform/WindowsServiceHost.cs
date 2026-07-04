using System.Diagnostics;
using System.ServiceProcess;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Service;

namespace ProxiFyre.UI.Platform;

// ProxiFyre registers its Windows service under this name (verify on the smoke host; adjust if it differs).
public sealed class WindowsServiceHost : IServiceHost
{
    private const string ServiceName = "ProxiFyre";
    private readonly ILocatorService _locator;
    public WindowsServiceHost(ILocatorService locator) => _locator = locator;

    public ServiceState Query()
    {
        try
        {
            using var sc = new System.ServiceProcess.ServiceController(ServiceName);
            return sc.Status switch
            {
                ServiceControllerStatus.Running or ServiceControllerStatus.StartPending
                    => ServiceState.Running,
                ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending
                    => ServiceState.Stopped,
                _ => ServiceState.Unknown
            };
        }
        catch (InvalidOperationException)
        {
            return ServiceState.NotInstalled; // service not found
        }
    }

    public void Install()   => RunExe("install");
    public void Uninstall() => RunExe("uninstall");
    public void Start()     => RunExe("start");
    public void Stop()      => RunExe("stop");

    private void RunExe(string verb)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _locator.ExePath,
            Arguments = verb,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(_locator.ExePath)!
        };
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to launch ProxiFyre.exe {verb}");
        proc.WaitForExit(30_000);
        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                $"ProxiFyre.exe {verb} failed (exit {proc.ExitCode}): {proc.StandardError.ReadToEnd()}");
    }
}
