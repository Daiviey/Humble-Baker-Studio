using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Services;

/// <summary>
/// Finds the AI drum/hit libraries and registers them with the sampler. Mirrors the discovery order
/// of the PowerShell build so a C# install can sit beside an existing package.
/// </summary>
internal static class SampleLibrary
{
    public static List<string> Discover(AppSettings settings)
    {
        var candidates = new List<string>();
        void Add(string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            try
            {
                var full = Path.GetFullPath(dir!).TrimEnd('\\');
                if (Directory.Exists(full) && !candidates.Contains(full, StringComparer.OrdinalIgnoreCase))
                    candidates.Add(full);
            }
            catch
            {
                // Ignore malformed paths.
            }
        }

        var env = Environment.GetEnvironmentVariable("HUMBLEBAKER_SAMPLES");
        Add(env);
        Add(AppContext.BaseDirectory);
        Add(Path.GetDirectoryName(Environment.ProcessPath));
        foreach (var root in settings.SampleRoots) Add(root);

        // Look beside the executable, then a couple of well-known package layouts.
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 3 && !string.IsNullOrEmpty(dir); i++)
        {
            Add(Path.Combine(dir, "samples"));
            dir = Path.GetDirectoryName(dir);
        }
        Add(@"Z:\LocalAI\App");
        Add(@"Z:\LocalAI\Humble Baker Studio Dist");

        foreach (var root in candidates) DrumSamples.AddRoot(root);
        DrumSamples.Reset();
        return candidates;
    }

    public static string[] KitDirectories(IReadOnlyList<string> roots)
    {
        var kits = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
        {
            var dk = Path.Combine(root, "DrumKits");
            if (!Directory.Exists(dk)) continue;
            foreach (var d in Directory.GetDirectories(dk))
                kits.Add(Path.GetFileName(d));
        }
        return kits.ToArray();
    }

    public static string[] BuiltInKits => new[]
    {
        "Standard", "808", "909", "Acoustic", "Lo-Fi", "Jazz", "Rock", "Techno",
        "House", "Hip-Hop", "Trap", "DnB", "Jungle", "Industrial", "Electro", "Latin", "Ambient"
    };
}
