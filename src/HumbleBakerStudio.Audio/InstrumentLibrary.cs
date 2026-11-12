using System.Text.Json.Nodes;

namespace HumbleBakerStudio.Audio;


/// <summary>
/// Reads the studio's saved instrument library: synth patches, morphs, spectral canvases and
/// sampled one-shots (the Foley / Orchestral / Guitar hit libraries). The same JSON files are
/// written by the PowerShell studio, so a C# install can use an existing library unchanged.
/// </summary>
public sealed class InstrumentDef
{
    public string FilePath = "";
    public string Name = "";
    public string Kind = "patch";       // patch | morph | spectral | sample
    public string Role = "any";
    public string Notes = "";
    public int Octave = 4;
    public int Root = 60;
    public bool Gate;
    public double Seconds = 4;
    public string HitMode = "";         // first | isolate | full
    public string SampleFile = "";
    public string SampleBase64 = "";
    public JsonObject? Patch;
    public JsonObject? Fx;
    public JsonObject? Grid;
    public JsonObject? Spectral;

    /// <summary>Set for entries that do not come from a JSON file (drum kits).</summary>
    public string? CategoryOverride { get; set; }

    public string Category
    {
        get
        {
            if (!string.IsNullOrEmpty(CategoryOverride)) return CategoryOverride!;
            string f = (SampleFile ?? "").Replace('/', '\\');
            if (f.IndexOf("FoleyPercussion", StringComparison.OrdinalIgnoreCase) >= 0) return "Foley";
            if (f.IndexOf("OrchestralHits", StringComparison.OrdinalIgnoreCase) >= 0) return "Orchestral";
            if (f.IndexOf("GuitarHits", StringComparison.OrdinalIgnoreCase) >= 0) return "Guitar";
            if (string.Equals(Kind, "sample", StringComparison.OrdinalIgnoreCase)) return "Samples";
            if (string.Equals(Kind, "spectral", StringComparison.OrdinalIgnoreCase)) return "Spectral";
            return "Patches";
        }
    }

    public string TypeLabel => Kind switch
    {
        "sample" => "sampled",
        "spectral" => "spectral",
        "morph" => "morph",
        _ => "patch"
    };
}

public static class InstrumentLibrary
{
    /// <summary>Instrument folders, in discovery order. Later duplicates are ignored.</summary>
    public static List<string> FindRoots(IEnumerable<string> sampleRoots)
    {
        var roots = new List<string>();
        void Add(string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            try
            {
                string full = Path.GetFullPath(dir!).TrimEnd('\\');
                if (Directory.Exists(full) && !roots.Contains(full, StringComparer.OrdinalIgnoreCase))
                    roots.Add(full);
            }
            catch { }
        }

        Add(Environment.GetEnvironmentVariable("HUMBLEBAKER_INSTRUMENTS"));
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Add(Path.Combine(local, "HumbleBakerAudio", "studio", "instruments"));
        Add(Path.Combine(local, "HumbleBakerStudio", "instruments"));
        Add(@"Z:\LocalAI\Audio\studio\instruments");

        foreach (string root in sampleRoots ?? Array.Empty<string>())
        {
            Add(Path.Combine(root, "studio", "instruments"));
            Add(Path.Combine(root, "instruments"));
        }

        // Find an Audio/studio/instruments folder next to or above the executable.
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5 && !string.IsNullOrEmpty(dir); i++)
        {
            Add(Path.Combine(dir, "Audio", "studio", "instruments"));
            Add(Path.Combine(dir, "studio", "instruments"));
            Add(Path.Combine(dir, "instruments"));
            dir = Path.GetDirectoryName(dir);
        }
        return roots;
    }

    public static List<InstrumentDef> LoadAll(IEnumerable<string> roots)
    {
        var all = new List<InstrumentDef>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // The studio library is often mirrored in LocalAppData and the dev tree; show each voice once.
        var seenVoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in roots ?? Array.Empty<string>())
        {
            string[] files;
            try { files = Directory.GetFiles(root, "*.json"); }
            catch { continue; }
            foreach (string file in files)
            {
                if (!seen.Add(file)) continue;
                InstrumentDef? def = LoadFile(file);
                if (def == null) continue;
                if (!seenVoices.Add(def.Kind + "|" + def.Name)) continue;
                all.Add(def);
            }
        }
        all.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return all;
    }

    public static InstrumentDef? LoadFile(string path)
    {
        try
        {
            JsonNode? node = JsonNode.Parse(File.ReadAllText(path));
            if (node is not JsonObject o) return null;
            var def = new InstrumentDef
            {
                FilePath = path,
                Name = GetString(o, "name", Path.GetFileNameWithoutExtension(path)),
                Kind = GetString(o, "kind", "patch").ToLowerInvariant(),
                Role = GetString(o, "role", "any"),
                Notes = GetString(o, "notes", ""),
                Octave = GetInt(o, "octave", 4),
                Root = GetInt(o, "root", 60),
                Gate = GetBool(o, "gate", false),
                Seconds = GetDouble(o, "seconds", 4),
                HitMode = GetString(o, "hitMode", ""),
                SampleFile = GetString(o, "sampleFile", ""),
                SampleBase64 = GetString(o, "sample", ""),
                Patch = o["patch"] as JsonObject,
                Fx = o["fx"] as JsonObject,
                Grid = o["grid"] as JsonObject,
                Spectral = o["spectral"] as JsonObject
            };
            return def;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Gives a track the sound of an instrument; notes are left alone.</summary>
    public static bool Apply(InstrumentDef def, Track track, IEnumerable<string> sampleRoots, out string error)
    {
        error = "";
        if (def == null || track == null) { error = "Nothing selected."; return false; }
        track.Fx = new Fx();
        track.InstrumentName = def.Name;

        if (def.Kind is "patch" or "morph")
        {
            track.Patch = new Patch();
            if (def.Patch != null) ApplyMap(track.Patch, def.Patch);
            track.Synth = "patch";
            track.Sample = null;
        }
        else if (def.Kind == "spectral")
        {
            float[]? audio = RenderSpectral(def);
            if (audio == null || audio.Length < 32) { error = "This spectral instrument could not be rendered."; return false; }
            track.Synth = "sample";
            track.Sample = audio;
            track.SampleRoot = def.Root;
            track.SampleGate = def.Gate;
            track.SampleMode = 0;
        }
        else
        {
            float[]? audio = LoadSample(def, sampleRoots);
            if (audio == null || audio.Length < 32) { error = "The sample for this instrument could not be found or read."; return false; }
            track.Synth = "sample";
            track.Sample = audio;
            track.SampleRoot = def.Root;
            track.SampleGate = def.Gate;
            track.SampleMode = 0;
        }

        if (def.Fx != null) ApplyMap(track.Fx, def.Fx);
        return true;
    }

    public static float[]? LoadSample(InstrumentDef def, IEnumerable<string> sampleRoots)
    {
        if (def == null) return null;
        if (!string.IsNullOrWhiteSpace(def.SampleFile))
        {
            string? file = ResolveSampleFile(def, sampleRoots);
            if (file != null)
            {
                int rate;
                float[]? s = WavIn.ReadMono(file, out rate);
                if (s != null && s.Length > 64)
                {
                    if (rate > 0 && rate != Dsp.SR) s = WavIn.Resample(s, rate, Dsp.SR);
                    s = WavIn.Trim(s);
                    if (s != null)
                    {
                        double cap = def.Seconds > 0 ? def.Seconds : 4.0;
                        s = def.HitMode switch
                        {
                            "isolate" => WavIn.ExtractHit(s, cap),
                            "first" => WavIn.ExtractFirstHit(s, cap),
                            _ => WavIn.TrimTail(s, cap)
                        };
                        if (s != null) s = WavIn.NormalizePeak(s, 0.95f);
                    }
                    if (s != null && s.Length >= 16) return s;
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(def.SampleBase64))
        {
            float[]? embedded = Dsp.SampleFromBase64(def.SampleBase64);
            if (embedded != null && embedded.Length >= 16) return embedded;
        }
        return null;
    }

    public static string? ResolveSampleFile(InstrumentDef def, IEnumerable<string> sampleRoots)
    {
        string rel = def.SampleFile ?? "";
        if (string.IsNullOrWhiteSpace(rel)) return null;
        if (Path.IsPathRooted(rel)) return File.Exists(rel) ? rel : null;

        foreach (string root in sampleRoots ?? Array.Empty<string>())
        {
            try
            {
                string p = Path.Combine(root, rel);
                if (File.Exists(p)) return p;
            }
            catch { }
        }

        // A relative hit path also works from the instrument file's own library tree.
        try
        {
            string? dir = Path.GetDirectoryName(def.FilePath);
            for (int i = 0; i < 4 && !string.IsNullOrEmpty(dir); i++)
            {
                string p = Path.Combine(dir, rel);
                if (File.Exists(p)) return p;
                dir = Path.GetDirectoryName(dir);
            }
        }
        catch { }
        return null;
    }

    public static float[]? RenderSpectral(InstrumentDef def)
    {
        if (def?.Grid == null) return null;
        try
        {
            int cols = GetInt(def.Grid, "cols", 160);
            int rows = GetInt(def.Grid, "rows", 64);
            var grid = new SpectralGrid(cols, rows);
            string b64 = GetString(def.Grid, "data", "");
            if (!string.IsNullOrEmpty(b64))
            {
                byte[] data = Convert.FromBase64String(b64);
                int n = Math.Min(data.Length, grid.A.Length);
                for (int i = 0; i < n; i++) grid.A[i] = data[i] / 255f;
            }
            var settings = new SpectralSettings();
            if (def.Spectral != null) ApplyMap(settings, def.Spectral);
            return Spectral.Render(grid, settings);
        }
        catch
        {
            return null;
        }
    }

    static int ApplyMap(object target, JsonObject obj)
    {
        int n = 0;
        foreach (var (key, node) in obj)
        {
            if (node == null) continue;
            if (node is JsonObject or JsonArray) continue;
            string text = node is JsonValue v
                ? (v.TryGetValue<string>(out var s) ? s : node.ToJsonString())
                : node.ToJsonString();
            if (PropIO.Set(target, key, text)) n++;
        }
        return n;
    }

    static string GetString(JsonObject o, string key, string fallback)
    {
        try
        {
            JsonNode? node = o[key];
            if (node is JsonValue v)
            {
                if (v.TryGetValue<string>(out var s)) return s ?? fallback;
                return v.ToJsonString().Trim('"');
            }
        }
        catch { }
        return fallback;
    }

    static int GetInt(JsonObject o, string key, int fallback)
    {
        try
        {
            JsonNode? node = o[key];
            if (node is JsonValue v)
            {
                if (v.TryGetValue<int>(out var i)) return i;
                if (v.TryGetValue<double>(out var d)) return (int)Math.Round(d);
                if (v.TryGetValue<string>(out var s) && int.TryParse(s, out var p)) return p;
            }
        }
        catch { }
        return fallback;
    }

    static double GetDouble(JsonObject o, string key, double fallback)
    {
        try
        {
            JsonNode? node = o[key];
            if (node is JsonValue v)
            {
                if (v.TryGetValue<double>(out var d)) return d;
                if (v.TryGetValue<string>(out var s) && double.TryParse(s, out var p)) return p;
            }
        }
        catch { }
        return fallback;
    }

    static bool GetBool(JsonObject o, string key, bool fallback)
    {
        try
        {
            JsonNode? node = o[key];
            if (node is JsonValue v)
            {
                if (v.TryGetValue<bool>(out var b)) return b;
                if (v.TryGetValue<string>(out var s)) return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
            }
        }
        catch { }
        return fallback;
    }
}
