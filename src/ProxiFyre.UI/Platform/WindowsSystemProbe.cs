using Microsoft.Win32;
using System.ServiceProcess;
using ProxiFyre.Core.Prereq;

namespace ProxiFyre.UI.Platform;

public sealed class WindowsSystemProbe : ISystemProbe
{
    // WinpkFilter installs a kernel service named "ndisrd".
    public bool IsWinpkFilterInstalled()
    {
        try
        {
            using var sc = new System.ServiceProcess.ServiceController("ndisrd");
            _ = sc.Status; // touching Status throws if the service does not exist
            return true;
        }
        catch { return false; }
    }

    // VC++ 2015-2022 x64 runtime writes this registry key with Installed=1.
    public bool IsVcRuntimeInstalled()
    {
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
        return key?.GetValue("Installed") is int installed && installed == 1;
    }
}
