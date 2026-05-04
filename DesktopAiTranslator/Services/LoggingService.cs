using System.IO;

namespace DesktopAiTranslator.Services;

public sealed class LoggingService : IDisposable
{
    private readonly object _lock = new();
    private readonly string _logPath;

    public LoggingService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopAiTranslator",
            "logs");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "app.log");
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        var detail = exception == null ? message : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", detail);
    }

    private void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logPath, $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never break user interaction.
        }
    }

    public void Dispose()
    {
    }
}
