using Newtonsoft.Json;

namespace MultiAudioOutput;

public class DeviceSettings
{
    public string DeviceId { get; set; } = "";
    public string CustomName { get; set; } = "";
    public int ChannelMode { get; set; } = 0;
    public bool IsSelected { get; set; } = false;
}

public class AppSettings
{
    public string Language { get; set; } = "en";
    public string SourceDeviceId { get; set; } = "";
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool AutoStart { get; set; } = false;
    public List<DeviceSettings> Devices { get; set; } = new();
    
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
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }
    
    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
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
        catch { }
    }
}
