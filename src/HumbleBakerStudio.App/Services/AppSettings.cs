using System.Text.Json;
using System.Text.Json.Serialization;

namespace HumbleBakerStudio.Services;

/// <summary>User settings, stored next to the app data rather than inside the repository.</summary>
internal sealed class AppSettings
{
    public string OllamaUrl { get; set; } = "http://127.0.0.1:11434";
    public string OllamaModel { get; set; } = "";
    public string OutputDirectory { get; set; } = "";
    public string FfmpegPath { get; set; } = "";
    public string SuperColliderPath { get; set; } = "";
    public List<string> SampleRoots { get; set; } = new();
    public string LastProject { get; set; } = "";
    public bool OpenLastProject { get; set; } = true;
    public bool MasterOnPlayback { get; set; } = true;

    [JsonIgnore]
    public static string DataDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HumbleBakerStudio");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    [JsonIgnore]
    public static string SettingsPath => Path.Combine(DataDirectory, "settings.json");

    [JsonIgnore]
    public string ResolvedOutputDirectory =>
        string.IsNullOrWhiteSpace(OutputDirectory) ? Path.Combine(DataDirectory, "renders") : OutputDirectory;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) return s.Normalize();
            }
        }
        catch
        {
            // A corrupt settings file must never stop the studio from opening.
        }
        return new AppSettings().Normalize();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Keep going; settings are a convenience, not a requirement.
        }
    }

    private AppSettings Normalize()
    {
        SampleRoots ??= new List<string>();
        if (string.IsNullOrWhiteSpace(OllamaUrl)) OllamaUrl = "http://127.0.0.1:11434";
        Directory.CreateDirectory(ResolvedOutputDirectory);
        return this;
    }
}
