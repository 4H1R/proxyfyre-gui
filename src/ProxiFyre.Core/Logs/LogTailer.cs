using System.Text;

namespace ProxiFyre.Core.Logs;

// Tracks a byte offset into a growing log file and returns only newly appended lines.
public sealed class LogTailer
{
    private readonly string _path;
    private long _offset;

    public LogTailer(string path) => _path = path;

    public IEnumerable<string> ReadNew()
    {
        if (!File.Exists(_path)) yield break;

        using var stream = new FileStream(
            _path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        if (stream.Length < _offset) _offset = 0; // file was truncated/rotated
        stream.Seek(_offset, SeekOrigin.Begin);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            yield return line;

        _offset = stream.Position;
    }
}
