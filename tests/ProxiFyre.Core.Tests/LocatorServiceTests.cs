using ProxiFyre.Core.Locate;
using Xunit;

public class LocatorServiceTests
{
    [Fact]
    public void Resolve_FromBaseDir_FindsBundledExeAndConfigAndLogs()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(baseDir);
        File.WriteAllText(Path.Combine(baseDir, "ProxiFyre.exe"), "");
        File.WriteAllText(Path.Combine(baseDir, "app-config.json"), "{}");

        var loc = new LocatorService(baseDir);

        Assert.True(loc.ExeExists);
        Assert.Equal(Path.Combine(baseDir, "ProxiFyre.exe"), loc.ExePath);
        Assert.Equal(Path.Combine(baseDir, "app-config.json"), loc.ConfigPath);
        Assert.Equal(Path.Combine(baseDir, "logs"), loc.LogsDir);

        Directory.Delete(baseDir, recursive: true);
    }

    [Fact]
    public void Resolve_MissingExe_ReportsNotFound()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(baseDir);
        var loc = new LocatorService(baseDir);
        Assert.False(loc.ExeExists);
        Directory.Delete(baseDir, recursive: true);
    }
}
