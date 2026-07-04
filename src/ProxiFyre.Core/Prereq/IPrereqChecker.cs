namespace ProxiFyre.Core.Prereq;

public interface ISystemProbe
{
    bool IsWinpkFilterInstalled();
    bool IsVcRuntimeInstalled();
}

public interface IPrereqChecker
{
    PrereqStatus Check();
}
