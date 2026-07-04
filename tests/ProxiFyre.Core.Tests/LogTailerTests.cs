using ProxiFyre.Core.Logs;
using Xunit;

public class LogTailerTests
{
    [Fact]
    public void ReadNew_ReturnsOnlyLinesAppendedSinceLastCall()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
        File.WriteAllText(path, "line1\nline2\n");
        var tailer = new LogTailer(path);

        var first = tailer.ReadNew().ToList();
        Assert.Equal(new[] { "line1", "line2" }, first);

        File.AppendAllText(path, "line3\n");
        var second = tailer.ReadNew().ToList();
        Assert.Equal(new[] { "line3" }, second);

        File.Delete(path);
    }

    [Fact]
    public void ReadNew_MissingFile_ReturnsEmpty()
    {
        var tailer = new LogTailer(Path.Combine(Path.GetTempPath(), "nope.log"));
        Assert.Empty(tailer.ReadNew());
    }
}
