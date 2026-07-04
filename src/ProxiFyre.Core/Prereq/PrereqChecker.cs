namespace ProxiFyre.Core.Prereq;

public sealed class PrereqChecker : IPrereqChecker
{
    private readonly ISystemProbe _probe;
    public PrereqChecker(ISystemProbe probe) => _probe = probe;

    public PrereqStatus Check()
    {
        var driver = _probe.IsWinpkFilterInstalled();
        var runtime = _probe.IsVcRuntimeInstalled();
        var status = new PrereqStatus { DriverInstalled = driver, RuntimeInstalled = runtime };
        if (!driver) status.Missing.Add("Windows Packet Filter (WinpkFilter) driver");
        if (!runtime) status.Missing.Add("Visual C++ 2022 Runtime");
        return status;
    }
}
