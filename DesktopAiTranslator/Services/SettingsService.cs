using System.IO;
using System.Text.Json;
using DesktopAiTranslator.Models;

namespace DesktopAiTranslator.Services;

public sealed class SettingsService
{
    private readonly LoggingService _logger;
    private readonly string _settingsPath;

    public AppSettings Current { get; private set; } = new();

    public SettingsService(LoggingService logger)
    {
        _logger = logger;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopAiTranslator");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions()) ?? new AppSettings();
            }
            else
            {
                Current = new AppSettings();
                Save();
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Settings file is invalid. Falling back to defaults.", ex);
            Current = new AppSettings();
        }

        return Current;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, JsonOptions());
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings.", ex);
        }
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
