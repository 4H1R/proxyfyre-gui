using System.Text;

namespace ProxiFyre.Core.Logs;

// Tracks a byte offset into a growing log file and returns only newly appended, newline-terminated lines.
//
// Rotation caveat: only truncate-in-place rotation is detected (the offset resets when the file
// shrinks). Rename-and-recreate rotation is not detected — callers that switch to a new file path
// (e.g. a UI that picks the newest log file each poll) should construct a new LogTailer for it.
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

        if (stream.Length < _offset) _offset = 0; // truncate-style rotation

        stream.Seek(_offset, SeekOrigin.Begin);
        var available = stream.Length - _offset;
        if (available <= 0) yield break;

        var buffer = new byte[available];
        var read = stream.Read(buffer, 0, buffer.Length);

        // Only consume through the last newline; carry any partial remainder to next poll.
        var lastNewline = Array.LastIndexOf(buffer, (byte)'\n', read - 1);
        if (lastNewline < 0) yield break; // no complete line yet

        var text = Encoding.UTF8.GetString(buffer, 0, lastNewline + 1);
        _offset += lastNewline + 1;

        foreach (var line in text.Split('\n'))
            if (line.Length > 0)
                yield return line.TrimEnd('\r');
    }
}
