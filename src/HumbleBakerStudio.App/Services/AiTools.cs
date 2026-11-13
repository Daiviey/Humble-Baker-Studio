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

    /// <summary>
    /// Paints a spectrogram from a text description. The model returns line, curve, blob, band
    /// and harmonic shapes in 0..1 canvas space; they are stamped into the grid and then the
    /// normal additive resynthesis turns the picture into sound.
    /// </summary>
    public async Task<int> DrawSpectralAsync(SpectralGrid grid, SpectralSettings settings, string description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Describe the sound or picture first.");
        string doc = PropIO.Describe(typeof(SpectralSettings));
        string system =
            "You design sounds by drawing on a spectrogram. The canvas is 0..1 in both axes: " +
            "x = time (0 start, 1 end), y = pitch (0 lowest, 1 highest, logarithmic). " +
            "Reply with ONE JSON object and nothing else (no markdown):\n" +
            "{\"shapes\":[...],\"settings\":{...optional...}}\n" +
            "Shape types (all numbers 0..1 except hz, count):\n" +
            "{\"type\":\"line\",\"x0\":0,\"y0\":0.2,\"x1\":1,\"y1\":0.8,\"width\":0.3,\"amp\":0.9} a pitch sweep\n" +
            "{\"type\":\"curve\",\"points\":[[0,0.3],[0.3,0.7],[0.6,0.4],[1,0.5]],\"width\":0.2,\"amp\":0.9} a melodic contour\n" +
            "{\"type\":\"harmonics\",\"hz\":110,\"count\":10,\"rolloff\":0.75,\"x0\":0,\"x1\":1,\"amp\":0.8} a tone with overtones\n" +
            "{\"type\":\"blob\",\"x\":0.5,\"y\":0.5,\"r\":0.2,\"amp\":0.7} a soft patch of sound\n" +
            "{\"type\":\"band\",\"x0\":0.4,\"x1\":0.6,\"y0\":0.6,\"y1\":0.9,\"amp\":0.6} a burst of noise\n" +
            "Use 4 to 14 shapes that match the description. Optional settings (numbers inside ranges):\n" + doc;

        var reply = await _ollama.ChatAsync(Model, system, "Sound wanted: " + description, json: true, ct);
        var obj = ParseObject(reply);
        if (obj["shapes"] is not JsonArray shapes || shapes.Count < 3)
            throw new InvalidOperationException("The model did not return a usable shapes array.");

        grid.Clear();
        int count = ApplyShapes(grid, shapes, settings);
        if (count == 0) throw new InvalidOperationException("None of the shapes were recognised.");
        if (obj["settings"] is JsonObject map) ApplyMap(settings, map);
        return count;
    }

    /// <summary>Designs four related patches for the four-corner morph synth.</summary>
    public async Task<int> DesignMorphAsync(Patch[] slots, string description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Describe the journey first.");
        if (slots == null || slots.Length < 4) throw new ArgumentException("Four slots are required.", nameof(slots));

        string pdoc = PropIO.Describe(typeof(Patch));
        string system =
            "You are a synthesizer sound designer building a morphing instrument from 4 patches. " +
            "Patch A is the start, B and C are the middle stages, D is the end. Make the four patches " +
            "clearly different from each other but related to the description. Reply with ONE JSON object " +
            "and nothing else (no markdown): {\"patches\":[{A},{B},{C},{D}]}. Each patch uses only these " +
            "parameter names with numeric values inside their ranges and enum names as strings; set only " +
            "6 to 14 parameters per patch and keep the reply short. Example: {\"patches\":[{\"Osc1\":\"sine\"," +
            "\"Cutoff\":800,\"Attack\":0.4},{\"Osc1\":\"saw\",\"Cutoff\":3000,\"Drive\":0.5},{\"Osc1\":\"square\"," +
            "\"Detune2\":9},{\"Osc1\":\"noise\",\"Resonance\":0.8}]}\n" + pdoc;

        var reply = await _ollama.ChatAsync(Model, system, "Morph wanted: " + description, json: true, ct);
        var obj = ParseObject(reply);
        if (obj["patches"] is not JsonArray patches || patches.Count < 2)
            throw new InvalidOperationException("The JSON must have a patches array of 4 patch objects.");

        int applied = 0;
        for (int i = 0; i < 4; i++)
        {
            var patch = new Patch();
            if (patches[Math.Min(i, patches.Count - 1)] is JsonObject src) applied += ApplyMap(patch, src);
            slots[i] = patch;
        }
        return applied;
    }

    static int ApplyShapes(SpectralGrid g, JsonArray shapes, SpectralSettings settings)
    {
        int cw = g.Cols - 1, rh = g.Rows - 1, n = 0;
        foreach (var item in shapes)
        {
            if (item is not JsonObject sh) continue;
            string type = GetText(sh, "type").ToLowerInvariant();
            double amp = Clamp01(GetNum(sh, "amp", 0.8));
            double width = sh.ContainsKey("width") ? 0.6 + Clamp01(GetNum(sh, "width", 0.5)) * 6 : 1.5;

            if (type is "line" or "sweep")
            {
                g.Line(Clamp01(GetNum(sh, "x0", 0)) * cw, Clamp01(GetNum(sh, "y0", 0)) * rh,
                      Clamp01(GetNum(sh, "x1", 1)) * cw, Clamp01(GetNum(sh, "y1", 1)) * rh, width, amp, false);
                n++;
            }
            else if (type is "curve" or "path")
            {
                if (sh["points"] is JsonArray pts)
                {
                    for (int k = 1; k < pts.Count; k++)
                    {
                        if (pts[k - 1] is not JsonArray a || pts[k] is not JsonArray b || a.Count < 2 || b.Count < 2) continue;
                        g.Line(Clamp01(Num(a[0])) * cw, Clamp01(Num(a[1])) * rh,
                              Clamp01(Num(b[0])) * cw, Clamp01(Num(b[1])) * rh, width, amp, false);
                    }
                }
                n++;
            }
            else if (type is "blob" or "circle")
            {
                g.Dab(Clamp01(GetNum(sh, "x", 0.5)) * cw, Clamp01(GetNum(sh, "y", 0.5)) * rh,
                      1 + Clamp01(GetNum(sh, "r", 0.5)) * rh * 0.5, amp, false);
                n++;
            }
            else if (type is "band" or "noise")
            {
                int x0 = (int)(Clamp01(GetNum(sh, "x0", 0)) * cw);
                int x1 = (int)(Clamp01(GetNum(sh, "x1", 1)) * cw);
                int r0 = (int)(Clamp01(GetNum(sh, "y0", 0)) * rh);
                int r1 = (int)(Clamp01(GetNum(sh, "y1", 1)) * rh);
                if (r1 < r0) (r0, r1) = (r1, r0);
                var rnd = new Random(3);
                for (int c = Math.Max(0, x0); c <= Math.Min(g.Cols - 1, x1); c++)
                    for (int r = Math.Max(0, r0); r <= Math.Min(g.Rows - 1, r1); r++)
                        g.A[c * g.Rows + r] = Math.Max(g.A[c * g.Rows + r], (float)(amp * (0.3 + 0.7 * rnd.NextDouble())));
                n++;
            }
            else if (type.StartsWith("harmonic"))
            {
                double hz = Math.Max(20, GetNum(sh, "hz", 110));
                int count = (int)Math.Clamp(GetNum(sh, "count", 8), 1, 40);
                double rolloff = Math.Clamp(GetNum(sh, "rolloff", 0.75), 0.3, 1);
                int x0 = sh.ContainsKey("x0") ? (int)(Clamp01(GetNum(sh, "x0", 0)) * cw) : 0;
                int x1 = sh.ContainsKey("x1") ? (int)(Clamp01(GetNum(sh, "x1", 1)) * cw) : cw;
                g.Harmonics(hz, x0, x1, count, rolloff, amp, settings.MinHz, settings.MaxHz);
                n++;
            }
        }
        return n;
    }

    static double Clamp01(double v) => Math.Max(0, Math.Min(1, v));
    static double Num(JsonNode? node)
    {
        if (node is JsonValue v && v.TryGetValue<double>(out var d)) return d;
        if (node is JsonValue s && s.TryGetValue<string>(out var text) &&
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return 0;
    }

    static string GetText(JsonObject o, string key)
    {
        if (o[key] is JsonValue v)
        {
            if (v.TryGetValue<string>(out var s)) return s;
            return v.ToJsonString().Trim('"');
        }
        return "";
    }

    static int ApplyMap(object target, JsonObject obj)
    {
        int n = 0;
        foreach (var (key, node) in obj)
        {
            if (node == null || node is JsonObject or JsonArray) continue;
            string text = node is JsonValue v
                ? (v.TryGetValue<string>(out var s) ? s : node.ToJsonString())
                : node.ToJsonString();
            if (PropIO.Set(target, key, text)) n++;
        }
        return n;
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
