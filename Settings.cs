using System.Text.Json;

namespace MultiAudioOutput;

public class DeviceSettings
{
    public string DeviceId { get; set; } = "";
    public string CustomName { get; set; } = "";
    public int ChannelMode { get; set; } = 0;
    public bool IsSelected { get; set; } = false;
    public float Volume { get; set; } = 1.0f;
}

public class AppSettings
{
    public string Language { get; set; } = "en";
    public string SourceDeviceId { get; set; } = "";
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool AutoStart { get; set; } = false;
    public List<DeviceSettings> Devices { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MultiAudioOutput",
        "settings.json"
    );

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to load settings, using defaults", ex);
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to save settings", ex);
        }
    }

    public void SetStartWithWindows(bool enabled)
    {
        StartWithWindows = enabled;

        try
        {
            var keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, true);

            if (key == null) return;

            if (enabled)
            {
                var exePath = Application.ExecutablePath;
                key.SetValue("MultiAudioOutput", $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue("MultiAudioOutput", false);
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to update Start with Windows registry entry", ex);
        }
    }
}
