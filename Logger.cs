namespace MultiAudioOutput;

/// <summary>
/// Minimal file logger. Errors that would otherwise be silently swallowed are
/// written to %AppData%\MultiAudioOutput\app.log so user bug reports are actionable.
/// </summary>
public static class Logger
{
    private static readonly object Sync = new();

    public static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MultiAudioOutput",
        "app.log"
    );

    // Start the log over once it grows past this, so it can never fill a disk
    private const long MaxLogBytes = 1024 * 1024;

    public static void Log(string message, Exception? ex = null)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

                var info = new FileInfo(LogPath);
                if (info.Exists && info.Length > MaxLogBytes)
                    info.Delete();

                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
                if (ex != null)
                    line += $" | {ex.GetType().Name}: {ex.Message}";

                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never take the app down
        }
    }
}
