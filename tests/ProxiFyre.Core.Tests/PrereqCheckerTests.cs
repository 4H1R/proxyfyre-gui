using ProxiFyre.Core.Prereq;
using Xunit;

public class PrereqCheckerTests
{
    private sealed class FakeProbe : ISystemProbe
    {
        public bool Driver, Runtime;
        public bool IsWinpkFilterInstalled() => Driver;
        public bool IsVcRuntimeInstalled() => Runtime;
    }

    [Fact]
    public void AllPresent_IsReady()
    {
        var s = new PrereqChecker(new FakeProbe { Driver = true, Runtime = true }).Check();
        Assert.True(s.DriverInstalled);
        Assert.True(s.RuntimeInstalled);
        Assert.True(s.AllSatisfied);
        Assert.Empty(s.Missing);
    }

    [Fact]
    public void MissingDriver_ListsIt()
    {
        var s = new PrereqChecker(new FakeProbe { Driver = false, Runtime = true }).Check();
        Assert.False(s.AllSatisfied);
        Assert.Contains(s.Missing, m => m.Contains("Packet Filter", StringComparison.OrdinalIgnoreCase));
    }
}
