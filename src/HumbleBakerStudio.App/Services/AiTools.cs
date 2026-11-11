using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Services;

/// <summary>
/// Local-AI helpers. Every answer is treated as untrusted text: it is parsed as JSON,
/// clamped to the instrument/project ranges, and never executed as code.
/// </summary>
internal sealed class AiTools
{
    private readonly OllamaClient _ollama;
    public string Model { get; set; } = "";

    public AiTools(OllamaClient ollama) => _ollama = ollama;

    private const string SystemPrompt =
        "You are a music production assistant inside Humble Baker Studio, an offline DAW. " +
        "You answer with a single JSON object and nothing else. No markdown fences, no commentary. " +
        "Notes use MIDI pitch (0-127), positions and lengths in beats, velocity 1-127. " +
        "Keep everything musical, playable and inside the requested range.";

    private static string ProjectBrief(Project p, Track t)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Project: {p.Bpm:0.##} BPM, {p.BeatsPerBar}/4, {p.Bars} bars, key {NoteName(p.Key)} {p.Scale}, swing {p.Swing:0.##}.");
        sb.AppendLine($"Track: \"{t.Name}\" using the {t.Synth} engine.");
        sb.AppendLine($"Loop length: {p.LoopBeats:0.##} beats ({p.Bars} bars).");
        if (t.Synth == "drums")
            sb.AppendLine("Drum pitches: 36 kick, 38 snare, 39 clap, 42 closed hat, 46 open hat, 45/47 toms, 49 crash, 51 ride, 37 rim.");
        sb.AppendLine("Return { \"notes\": [ { \"pitch\": 60, \"start\": 0, \"len\": 0.5, \"vel\": 100 } ] }.");
        return sb.ToString();
    }

    public async Task<List<Note>> GenerateNotesAsync(Project p, Track t, string style, int bars, CancellationToken ct)
    {
        string role = t.Synth switch
        {
            "drums" => "a drum pattern",
            "acid" => "a bass line",
            "pad" => "chord voicings / pad harmony",
            "pluck" => "an arpeggiated pluck line",
            _ => "a melodic line"
        };
        if (t.Synth == "drums") role = "a drum pattern";

        var prompt = new StringBuilder(ProjectBrief(p, t));
        prompt.AppendLine($"Write {role} for {bars} bar(s) in the style: {style}. ");
        prompt.AppendLine(t.Synth == "drums"
            ? "Use only the drum pitches listed above."
            : "Stay in the project key/scale. Use short rests; leave room for other instruments.");
        prompt.AppendLine("Do not repeat an existing pattern verbatim; create something with a clear motif.");

        var reply = await _ollama.ChatAsync(Model, SystemPrompt, prompt.ToString(), json: true, ct);
        return ParseNotes(reply, p.LoopBeats);
    }

    public async Task<List<Note>> GenerateDrumsAsync(Project p, Track t, string style, int bars, CancellationToken ct)
    {
        var prompt = new StringBuilder(ProjectBrief(p, t));
        prompt.AppendLine($"Write a {style} drum groove for {bars} bar(s).");
        prompt.AppendLine("Keep kick/snare/hat interplay clear and danceable; use velocity for groove (ghost notes 35-60, accents 110-127).");
        var reply = await _ollama.ChatAsync(Model, SystemPrompt, prompt.ToString(), json: true, ct);
        return ParseNotes(reply, p.LoopBeats);
    }

    public async Task DesignPatchAsync(Patch patch, string description, CancellationToken ct)
    {
        string spec =
            "Allowed keys and ranges:\n" +
            "- Osc1, Osc2, Osc3, SubShape: saw|square|pulse|tri|sine|noise|additive|vowel|pluck|bell|sync\n" +
            "- Level1, Level2, Level3, SubLevel, NoiseLevel: 0..1\n" +
            "- Detune2: -1200..1200 cents; Semi2, Semi3: -36..36 semitones\n" +
            "- Filter: off|lowpass|highpass|bandpass|notch|lowpass24|comb|vowel\n" +
            "- Cutoff: 30..16000 Hz; Resonance: 0..0.95; FilterEnv: -1..1; FilterDecay: 0.01..8\n" +
            "- Attack 0.001..5; Decay 0.01..8; Sustain 0..1; Release 0.005..8 (seconds)\n" +
            "- FmAmount 0..10; FmRatio 0.25..16; Drive 0..1; Fold 0..1; Bits 2..16\n" +
            "- Unison 1..7; UnisonSpread 0..100; LfoRate 0.05..40; LfoPitch 0..1200; LfoFilter 0..1; LfoAmp 0..1\n" +
            "- Gain 0..1.5; Morph 0..1; Harmonics 1..48; Tilt 0..3";

        string user =
            $"Design a synth patch for this description: \"{description}\".\n" +
            spec + "\n" +
            "Return a JSON object with only the keys you want to change, values as numbers or exact enum names.";

        var reply = await _ollama.ChatAsync(Model, SystemPrompt, user, json: true, ct);
        var obj = ParseObject(reply);
        int applied = 0;
        foreach (var (key, node) in obj)
        {
            if (node == null) continue;
            string text = node is JsonValue v && v.TryGetValue<string>(out var s) ? s : node.ToJsonString();
            if (PropIO.Set(patch, key, text)) applied++;
        }
        if (applied == 0) throw new InvalidOperationException("The model did not return any usable patch parameters.");
    }

    public async Task<(string Advice, int Applied)> AdviseMasterAsync(Project p, MasterSettings master, CancellationToken ct)
    {
        var settings = PropIO.Describe(typeof(MasterSettings));
        string user =
            $"Suggest mastering settings for a {p.Bpm:0.##} BPM {p.Scale} track. Current chain: enabled={master.Enabled}, " +
            $"input {master.InputGain:0.#} dB, compressor {master.CompRatio:0.#}:1 at {master.CompThreshold:0.#} dB, target {master.TargetLufs:0.#} LUFS.\n" +
            settings + "\nReturn JSON {\"advice\":\"one paragraph\",\"settings\":{\"PropertyName\":value}}.";

        var reply = await _ollama.ChatAsync(Model, SystemPrompt, user, json: true, ct);
        var obj = ParseObject(reply);
        string advice = obj["advice"]?.GetValue<string>() ?? reply.Trim();
        int applied = 0;
        if (obj["settings"] is JsonObject settingsObj)
            foreach (var (key, node) in settingsObj)
            {
                if (node == null) continue;
                string text = node is JsonValue v && v.TryGetValue<string>(out var s) ? s : node.ToJsonString();
                if (PropIO.Set(master, key, text)) applied++;
            }
        return (advice, applied);
    }

    public async Task<string> ReviewMixAsync(Project p, CancellationToken ct)
    {
        var sb = new StringBuilder(ProjectBrief(p, p.Tracks.FirstOrDefault() ?? new Track()));
        sb.AppendLine("Tracks:");
        foreach (var t in p.Tracks)
            sb.AppendLine($"- {t.Name}: {t.Synth}, vol {t.Vol:0.##}, pan {t.Pan:0.##}, bus {t.Bus}, FX on={t.Fx.Active}");
        sb.AppendLine("Give 5 short, concrete mixing notes as JSON {\"notes\":[\"...\"]}.");
        var reply = await _ollama.ChatAsync(Model, SystemPrompt, sb.ToString(), json: true, ct);
        var obj = ParseObject(reply);
        if (obj["notes"] is JsonArray arr)
            return string.Join("\n", arr.Select(a => "• " + (a?.GetValue<string>() ?? "")));
        return reply.Trim();
    }

    // ---- parsing ---------------------------------------------------------

    public static List<Note> ParseNotes(string reply, double loopBeats)
    {
        var obj = ParseObject(reply);
        JsonArray? arr = obj["notes"] as JsonArray;
        if (arr == null) return new List<Note>();

        var notes = new List<Note>();
        foreach (var n in arr.OfType<JsonObject>())
        {
            int pitch = (int)Math.Round(GetNum(n, "pitch", 60));
            double start = GetNum(n, "start", 0);
            double len = GetNum(n, "len", 0.5);
            int vel = (int)Math.Round(GetNum(n, "vel", 100));

            pitch = Math.Clamp(pitch, 0, 127);
            start = Math.Max(0, Math.Min(start, Math.Max(0, loopBeats - 0.25)));
            start = Math.Round(start * 4) / 4; // 1/16 grid
            len = Math.Clamp(len, 0.0625, 16);
            vel = Math.Clamp(vel, 1, 127);
            notes.Add(new Note(pitch, start, len, vel));
        }
        return notes;
    }

    /// <summary>Pulls the first JSON object out of a chatty reply and closes it if the model was cut off.</summary>
    public static JsonObject ParseObject(string text)
    {
        var t = text.Trim();
        // Strip markdown fences if the model added them.
        if (t.StartsWith("```"))
        {
            int nl = t.IndexOf('\n');
            if (nl >= 0) t = t[(nl + 1)..];
            int close = t.LastIndexOf("```", StringComparison.Ordinal);
            if (close >= 0) t = t[..close];
        }
        int start = t.IndexOfAny(new[] { '{', '[' });
        if (start < 0) throw new InvalidOperationException("The model did not return JSON.");
        t = t[start..];
        t = System.Text.RegularExpressions.Regex.Replace(t, @",\s*([}\]])", "$1");

        try
        {
            var node = JsonNode.Parse(t, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            if (node is JsonObject o) return o;
            throw new InvalidOperationException("The model returned JSON that was not an object.");
        }
        catch (JsonException)
        {
            // Try to close a truncated object.
            int open = t.IndexOf('{');
            bool inStr = false, esc = false;
            var stack = new Stack<char>();
            for (int i = open; i < t.Length; i++)
            {
                char c = t[i];
                if (inStr)
                {
                    if (esc) esc = false;
                    else if (c == '\\') esc = true;
                    else if (c == '"') inStr = false;
                    continue;
                }
                if (c == '"') { inStr = true; continue; }
                if (c == '{') stack.Push('}');
                else if (c == '[') stack.Push(']');
                else if (c == '}' || c == ']') { if (stack.Count == 0) break; stack.Pop(); }
            }
            var fixedText = t + new string(stack.ToArray());
            try
            {
                var node = JsonNode.Parse(fixedText, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true });
                if (node is JsonObject o) return o;
            }
            catch (JsonException)
            {
                // Fall through to the friendly error.
            }
            throw new InvalidOperationException("The model returned malformed JSON. Try again or use a smaller model.");
        }
    }

    private static double GetNum(JsonObject o, string key, double fallback)
    {
        if (o[key] is JsonValue v && v.TryGetValue<double>(out var d) && double.IsFinite(d)) return d;
        if (o[key] is JsonValue s && s.TryGetValue<string>(out var str) &&
            double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return fallback;
    }

    public static string NoteName(int midi)
    {
        string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int n = ((midi % 12) + 12) % 12;
        return names[n] + (midi / 12 - 1);
    }
}
