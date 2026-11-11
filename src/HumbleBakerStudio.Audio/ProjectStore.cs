using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace HumbleBakerStudio.Audio;

/// <summary>
/// Reads and writes the studio project format (<c>*.hbstudio.json</c>).
/// The schema is deliberately identical to the PowerShell build so projects stay portable
/// in both directions while the C# port matures.
/// </summary>
public static class ProjectStore
{
    private static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static Project CreateDefault()
    {
        var p = new Project();
        p.Tracks.Add(NewTrack("Drums", "drums"));
        p.Tracks.Add(NewTrack("Acid", "acid"));
        p.Tracks.Add(NewTrack("Pad", "pad"));
        for (int i = 0; i < 4; i++) p.Buses.Add(new Bus("Bus " + (char)('A' + i)));
        AddDemoContent(p);
        p.Patterns.Add(Pattern.Capture(p, "Pattern 1"));
        p.Song.Add(new SongItem(0, 4));
        p.Markers.Add(new Marker(0, "Intro", 4));
        return p;
    }

    /// <summary>A short, musical starting point so the first press of Play does something.</summary>
    private static void AddDemoContent(Project p)
    {
        p.Bpm = 124;
        p.Key = 9;              // A
        p.Scale = "minor";
        p.Bars = 4;

        var drums = p.Tracks[0];
        for (int bar = 0; bar < p.Bars; bar++)
        {
            double b = bar * 4;
            for (int beat = 0; beat < 4; beat++)
                drums.Notes.Add(new Note(36, b + beat, 0.22, 118));          // four on the floor
            drums.Notes.Add(new Note(38, b + 1, 0.22, 108));                 // backbeat
            drums.Notes.Add(new Note(38, b + 3, 0.22, 110));
            for (int eighth = 0; eighth < 8; eighth++)
                drums.Notes.Add(new Note(42, b + eighth * 0.5, 0.09, eighth % 2 == 0 ? 78 : 54));
            if (bar == 3) drums.Notes.Add(new Note(46, b + 3.5, 0.4, 95));
        }

        var acid = p.Tracks[1];
        int[] riff = { 33, 33, 36, 33, 40, 33, 31, 33, 33, 36, 40, 43, 40, 36, 33, 31 };
        for (int bar = 0; bar < p.Bars; bar++)
        {
            for (int step = 0; step < riff.Length; step++)
            {
                if (bar % 2 == 1 && step % 4 == 3) continue;
                acid.Notes.Add(new Note(riff[step], bar * 4 + step * 0.25, 0.19, step % 4 == 0 ? 112 : 88));
            }
        }

        var pad = p.Tracks[2];
        int[][] chords = { new[] { 60, 63, 67 }, new[] { 56, 60, 63 }, new[] { 58, 61, 65 }, new[] { 55, 58, 62 } };
        for (int bar = 0; bar < p.Bars; bar++)
        {
            foreach (int pitch in chords[bar % chords.Length])
                pad.Notes.Add(new Note(pitch, bar * 4, 3.85, 74));
        }
    }

    public static Track NewTrack(string name, string synth)
    {
        var t = new Track { Name = name, Synth = synth };
        switch (synth)
        {
            case "drums": t.Vol = 0.9; break;
            case "pad": t.Cutoff = 1800; t.Vol = 0.6; t.Atk = 0.2; t.Rel = 0.6; t.EnvAmt = 0.1; break;
            case "pluck": t.Vol = 0.8; t.Rel = 0.3; break;
            case "sine": t.Vol = 0.9; t.Rel = 0.2; break;
        }
        if (synth == "patch") t.Patch = new Patch();
        return t;
    }

    public static Project Load(string path)
    {
        var text = File.ReadAllText(path);
        var root = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        }) as JsonObject ?? throw new InvalidDataException("The project file is not a JSON object.");

        var p = new Project
        {
            Bpm = GetDouble(root, "bpm", 140),
            BeatsPerBar = GetInt(root, "beatsPerBar", 4),
            BeatUnit = GetInt(root, "beatUnit", 4),
            PhraseBars = GetInt(root, "phraseBars", 4),
            Bars = GetInt(root, "bars", 4),
            Swing = GetDouble(root, "swing", 0),
            DelayMix = GetDouble(root, "delayMix", 0.2),
            DelayBeats = GetDouble(root, "delayBeats", 0.75),
            DelayFb = GetDouble(root, "delayFb", 0.4),
            Key = GetInt(root, "key", 0),
            Scale = GetString(root, "scale", "minor"),
            Click = GetBool(root, "click", false),
            ScSessionId = GetString(root, "scSessionId", "")
        };

        if (root["master"] is JsonObject master) ApplyMap(p.Master, master);

        if (root["markers"] is JsonArray markers)
            foreach (var m in markers.OfType<JsonObject>())
                p.Markers.Add(new Marker(GetDouble(m, "beat", 0), GetString(m, "name", ""), GetInt(m, "energy", 5)));

        if (root["mix"] is JsonArray mix)
            foreach (var m in mix.OfType<JsonObject>())
            {
                var rule = new MixRule();
                if (ApplyMap(rule, m) > 0) p.Mix.Add(rule);
            }

        if (root["buses"] is JsonArray buses)
            foreach (var b in buses.OfType<JsonObject>())
            {
                var bus = new Bus(GetString(b, "name", "Bus"));
                bus.Enabled = GetBool(b, "enabled", true);
                if (b["fx"] is JsonObject fx) ApplyMap(bus.Fx, fx);
                bus.Auto = ReadAutoLanes(b["auto"], 0);
                p.Buses.Add(bus);
            }
        while (p.Buses.Count < 4)
            p.Buses.Add(new Bus("Bus " + (char)('A' + p.Buses.Count)));

        if (root["graph"] is JsonObject gd) ReadGraph(p, gd);

        if (root["patterns"] is JsonArray patterns)
            foreach (var pd in patterns.OfType<JsonObject>())
            {
                var pat = new Pattern
                {
                    Name = GetString(pd, "name", "Pattern"),
                    Bars = GetInt(pd, "bars", p.Bars)
                };
                if (pd["notes"] is JsonArray notes)
                    foreach (var s in notes)
                        pat.Notes.Add(ParseNotes(s?.GetValue<string>() ?? ""));
                if (pd["mutes"] is JsonArray mutes)
                    foreach (var m in mutes) pat.Mutes.Add(m?.GetValue<bool>() ?? false);
                while (pat.Mutes.Count < pat.Notes.Count) pat.Mutes.Add(false);
                if (pd["auto"] is JsonArray auto)
                    foreach (var ad in auto.OfType<JsonObject>())
                    {
                        var lane = new AutoLane(GetInt(ad, "track", 0), GetString(ad, "param", "vol"));
                        ReadPoints(ad["points"], lane);
                        pat.Auto.Add(lane);
                    }
                p.Patterns.Add(pat);
            }

        if (root["song"] is JsonArray song)
            foreach (var s in song.OfType<JsonObject>())
                p.Song.Add(new SongItem(GetInt(s, "pattern", 0), GetInt(s, "repeats", 1)));

        if (root["tracks"] is JsonArray tracks)
            foreach (var td in tracks.OfType<JsonObject>())
            {
                var t = new Track
                {
                    Name = GetString(td, "name", "Track"),
                    Synth = GetString(td, "synth", "acid"),
                    Vol = GetDouble(td, "vol", 0.8),
                    Pan = GetDouble(td, "pan", 0),
                    Cutoff = GetDouble(td, "cutoff", 2400),
                    Res = GetDouble(td, "res", 0.3),
                    EnvAmt = GetDouble(td, "envamt", 0.6),
                    Atk = GetDouble(td, "atk", 0.005),
                    Rel = GetDouble(td, "rel", 0.15),
                    Crush = GetDouble(td, "crush", 16),
                    Mute = GetBool(td, "mute", false),
                    Solo = GetBool(td, "solo", false),
                    Bus = GetInt(td, "bus", 0),
                    Uid = GetString(td, "uid", Guid.NewGuid().ToString("N")),
                    DrumKit = GetString(td, "drumKit", "Standard"),
                    InstrumentName = GetString(td, "instrumentName", ""),
                    SampleRoot = GetInt(td, "sampleRoot", 60),
                    SampleGate = GetBool(td, "sampleGate", false),
                    SampleMode = GetInt(td, "sampleMode", 0)
                };
                if (td["patch"] is JsonObject patch)
                {
                    t.Patch = new Patch();
                    ApplyMap(t.Patch, patch);
                }
                if (t.Synth == "patch" && t.Patch == null) t.Patch = new Patch();
                if (td["fx"] is JsonObject fx) ApplyMap(t.Fx, fx);
                if (td["sample"] is JsonValue sampleValue && sampleValue.TryGetValue<string>(out var b64) && !string.IsNullOrEmpty(b64))
                    t.Sample = Dsp.SampleFromBase64(b64);
                if (td["notes"] is JsonArray notes)
                    foreach (var n in notes)
                    {
                        if (n is not JsonArray a || a.Count < 4) continue;
                        t.Notes.Add(new Note(
                            (int)a[0]!.GetValue<double>(),
                            a[1]!.GetValue<double>(),
                            a[2]!.GetValue<double>(),
                            (int)a[3]!.GetValue<double>()));
                    }
                t.Auto = ReadAutoLanes(td["auto"], 0);
                p.Tracks.Add(t);
            }

        if (p.Tracks.Count == 0) p.Tracks.Add(NewTrack("Track 1", "acid"));
        if (p.Patterns.Count == 0) p.Patterns.Add(Pattern.Capture(p, "Pattern 1"));
        return p;
    }

    public static void Save(Project p, string path)
    {
        var root = new JsonObject
        {
            ["bpm"] = p.Bpm,
            ["beatsPerBar"] = p.BeatsPerBar,
            ["beatUnit"] = p.BeatUnit,
            ["phraseBars"] = p.PhraseBars,
            ["bars"] = p.Bars,
            ["swing"] = p.Swing,
            ["delayMix"] = p.DelayMix,
            ["delayBeats"] = p.DelayBeats,
            ["delayFb"] = p.DelayFb,
            ["key"] = p.Key,
            ["scale"] = p.Scale,
            ["click"] = p.Click,
            ["scSessionId"] = p.ScSessionId,
            ["master"] = MapFromObject(p.Master),
            ["markers"] = new JsonArray(p.Markers.Select(m => (JsonNode)new JsonObject
            {
                ["beat"] = m.Beat,
                ["name"] = m.Name,
                ["energy"] = m.Energy
            }).ToArray()),
            ["mix"] = new JsonArray(p.Mix.Select(m => (JsonNode)MapFromObject(m)).ToArray()),
            ["buses"] = new JsonArray(p.Buses.Select(b => (JsonNode)new JsonObject
            {
                ["name"] = b.Name,
                ["enabled"] = b.Enabled,
                ["fx"] = MapFromObject(b.Fx),
                ["auto"] = WriteAutoLanes(b.Auto)
            }).ToArray()),
            ["graph"] = WriteGraph(p.Graph),
            ["patterns"] = new JsonArray(p.Patterns.Select(pat => (JsonNode)new JsonObject
            {
                ["name"] = pat.Name,
                ["bars"] = pat.Bars,
                ["mutes"] = new JsonArray(pat.Mutes.Select(x => (JsonNode)JsonValue.Create(x)).ToArray()),
                ["notes"] = new JsonArray(pat.Notes.Select(l => (JsonNode)JsonValue.Create(NoteString(l))).ToArray()),
                ["auto"] = new JsonArray(pat.Auto.Select(a => (JsonNode)new JsonObject
                {
                    ["track"] = a.Track,
                    ["param"] = a.Param,
                    ["points"] = WritePoints(a)
                }).ToArray())
            }).ToArray()),
            ["song"] = new JsonArray(p.Song.Select(s => (JsonNode)new JsonObject
            {
                ["pattern"] = s.Pattern,
                ["repeats"] = s.Repeats
            }).ToArray()),
            ["tracks"] = new JsonArray(p.Tracks.Select(t => (JsonNode)new JsonObject
            {
                ["name"] = t.Name,
                ["synth"] = t.Synth,
                ["vol"] = t.Vol,
                ["pan"] = t.Pan,
                ["cutoff"] = t.Cutoff,
                ["res"] = t.Res,
                ["envamt"] = t.EnvAmt,
                ["atk"] = t.Atk,
                ["rel"] = t.Rel,
                ["crush"] = t.Crush,
                ["mute"] = t.Mute,
                ["solo"] = t.Solo,
                ["bus"] = t.Bus,
                ["uid"] = t.Uid,
                ["drumKit"] = t.DrumKit,
                ["instrumentName"] = t.InstrumentName,
                ["patch"] = t.Patch != null ? MapFromObject(t.Patch) : null,
                ["fx"] = MapFromObject(t.Fx),
                ["sample"] = t.Sample != null ? Dsp.SampleToBase64(t.Sample) : null,
                ["sampleRoot"] = t.SampleRoot,
                ["sampleGate"] = t.SampleGate,
                ["sampleMode"] = t.SampleMode,
                ["notes"] = new JsonArray(t.Notes.Select(n => (JsonNode)new JsonArray
                {
                    n.Pitch, n.Start, n.Len, n.Vel
                }).ToArray()),
                ["auto"] = WriteAutoLanes(t.Auto)
            }).ToArray())
        };

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, root.ToJsonString(Pretty));
    }

    // ---- helpers ---------------------------------------------------------

    public static string NoteString(IEnumerable<Note> notes)
    {
        var ci = CultureInfo.InvariantCulture;
        return string.Join(";", notes.Select(n =>
            $"{n.Pitch},{n.Start.ToString("R", ci)},{n.Len.ToString("R", ci)},{n.Vel}"));
    }

    public static List<Note> ParseNotes(string text)
    {
        var ci = CultureInfo.InvariantCulture;
        var list = new List<Note>();
        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var f = part.Split(',');
            if (f.Length < 4) continue;
            if (int.TryParse(f[0], NumberStyles.Integer, ci, out var pitch) &&
                double.TryParse(f[1], NumberStyles.Float, ci, out var start) &&
                double.TryParse(f[2], NumberStyles.Float, ci, out var len) &&
                int.TryParse(f[3], NumberStyles.Integer, ci, out var vel))
                list.Add(new Note(pitch, start, len, vel));
        }
        return list;
    }

    private static JsonObject MapFromObject(object o)
    {
        var obj = new JsonObject();
        foreach (var pi in o.GetType().GetProperties())
        {
            if (!pi.CanWrite || pi.GetIndexParameters().Length > 0) continue;
            var v = pi.GetValue(o);
            obj[pi.Name] = v switch
            {
                null => null,
                bool b => JsonValue.Create(b),
                Enum e => JsonValue.Create(e.ToString()),
                string s => JsonValue.Create(s),
                double d => JsonValue.Create(d),
                float f => JsonValue.Create((double)f),
                int i => JsonValue.Create((double)i),
                long l => JsonValue.Create((double)l),
                _ => JsonValue.Create(Convert.ToString(v, CultureInfo.InvariantCulture))
            };
        }
        return obj;
    }

    private static int ApplyMap(object target, JsonObject obj)
    {
        int n = 0;
        foreach (var (key, node) in obj)
        {
            if (node == null) continue;
            if (node is JsonObject or JsonArray) continue;
            string? text = node is JsonValue v
                ? (v.TryGetValue<string>(out var s) ? s : node.ToJsonString())
                : node.ToJsonString();
            if (PropIO.Set(target, key, text)) n++;
        }
        return n;
    }

    private static JsonArray WriteAutoLanes(List<AutoLane> lanes)
    {
        return new JsonArray(lanes.Select(l => (JsonNode)new JsonObject
        {
            ["param"] = l.Param,
            ["points"] = WritePoints(l)
        }).ToArray());
    }

    private static JsonArray WritePoints(AutoLane lane)
    {
        return new JsonArray(lane.Points.Select(pt => (JsonNode)new JsonArray { pt[0], pt[1] }).ToArray());
    }

    private static List<AutoLane> ReadAutoLanes(JsonNode? node, int track)
    {
        var lanes = new List<AutoLane>();
        if (node is not JsonArray arr) return lanes;
        foreach (var ad in arr.OfType<JsonObject>())
        {
            var lane = new AutoLane(GetInt(ad, "track", track), GetString(ad, "param", "vol"));
            ReadPoints(ad["points"], lane);
            lanes.Add(lane);
        }
        return lanes;
    }

    private static void ReadPoints(JsonNode? node, AutoLane lane)
    {
        if (node is not JsonArray pts) return;
        foreach (var pt in pts)
        {
            if (pt is not JsonArray pair || pair.Count < 2) continue;
            lane.Points.Add(new[] { pair[0]!.GetValue<double>(), pair[1]!.GetValue<double>() });
        }
    }

    private static JsonObject WriteGraph(PatchGraph g)
    {
        return new JsonObject
        {
            ["initialized"] = g.Initialized,
            ["panX"] = g.PanX,
            ["panY"] = g.PanY,
            ["nodes"] = new JsonArray(g.Nodes.Select(n => (JsonNode)new JsonObject
            {
                ["id"] = n.Id,
                ["kind"] = n.Kind,
                ["type"] = n.Type,
                ["label"] = n.Label,
                ["x"] = n.X,
                ["y"] = n.Y,
                ["trackUid"] = n.TrackUid,
                ["gain"] = n.Gain,
                ["pan"] = n.Pan,
                ["bypass"] = n.Bypass,
                ["fx"] = MapFromObject(n.Fx),
                ["auto"] = WriteAutoLanes(n.Auto)
            }).ToArray()),
            ["links"] = new JsonArray(g.Links.Select(l => (JsonNode)new JsonObject
            {
                ["from"] = l.From,
                ["to"] = l.To
            }).ToArray()),
            ["groups"] = new JsonArray(g.Groups.Select(gr => (JsonNode)new JsonObject
            {
                ["id"] = gr.Id,
                ["name"] = gr.Name,
                ["nodeId"] = gr.NodeId,
                ["collapsed"] = gr.Collapsed,
                ["color"] = gr.ColorIndex,
                ["members"] = new JsonArray(gr.Members.Select(m => (JsonNode)JsonValue.Create(m)).ToArray())
            }).ToArray())
        };
    }

    private static void ReadGraph(Project p, JsonObject gd)
    {
        var g = p.Graph;
        g.Initialized = GetBool(gd, "initialized", false);
        g.PanX = GetDouble(gd, "panX", 0);
        g.PanY = GetDouble(gd, "panY", 0);

        if (gd["nodes"] is JsonArray nodes)
            foreach (var nd in nodes.OfType<JsonObject>())
            {
                var n = new PatchNode
                {
                    Id = GetString(nd, "id", Guid.NewGuid().ToString("N")),
                    Kind = GetString(nd, "kind", "fx"),
                    Type = GetString(nd, "type", ""),
                    Label = GetString(nd, "label", ""),
                    X = GetDouble(nd, "x", 0),
                    Y = GetDouble(nd, "y", 0),
                    TrackUid = GetString(nd, "trackUid", ""),
                    Gain = GetDouble(nd, "gain", 1),
                    Pan = GetDouble(nd, "pan", 0),
                    Bypass = GetBool(nd, "bypass", false)
                };
                if (nd["fx"] is JsonObject fx) ApplyMap(n.Fx, fx);
                n.Auto = ReadAutoLanes(nd["auto"], 0);
                g.Nodes.Add(n);
            }

        if (gd["links"] is JsonArray links)
            foreach (var ln in links.OfType<JsonObject>())
                g.Links.Add(new PatchLink(GetString(ln, "from", ""), GetString(ln, "to", "")));

        if (gd["groups"] is JsonArray groups)
            foreach (var grd in groups.OfType<JsonObject>())
            {
                var gr = new PatchGroup
                {
                    Id = GetString(grd, "id", Guid.NewGuid().ToString("N")),
                    Name = GetString(grd, "name", "Group"),
                    NodeId = GetString(grd, "nodeId", ""),
                    Collapsed = GetBool(grd, "collapsed", false),
                    ColorIndex = GetInt(grd, "color", 0)
                };
                if (grd["members"] is JsonArray members)
                    foreach (var m in members)
                        if (m != null) gr.Members.Add(m.GetValue<string>());
                if (!string.IsNullOrEmpty(gr.NodeId)) g.Groups.Add(gr);
            }

        // Match the PowerShell loader: keep graph FX modules bound to the live track/bus FX objects.
        foreach (var n in g.Nodes)
        {
            if (n.Kind != "fx") continue;
            if (string.Equals(n.Type, "trackfx", StringComparison.OrdinalIgnoreCase))
            {
                PatchNode? up = null;
                foreach (var ln in g.Links)
                    if (ln.To == n.Id)
                    {
                        var cand = g.Find(ln.From);
                        if (cand != null && cand.Kind == "track") { up = cand; break; }
                    }
                if (up != null)
                {
                    var tr = p.Tracks.FirstOrDefault(t => t.Uid == up.TrackUid);
                    if (tr != null) n.Fx = tr.Fx;
                }
            }
        }
    }

    private static string GetString(JsonObject o, string key, string fallback) =>
        o[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : fallback;

    private static double GetDouble(JsonObject o, string key, double fallback) =>
        o[key] is JsonValue v && v.TryGetValue<double>(out var d) && double.IsFinite(d) ? d : fallback;

    private static int GetInt(JsonObject o, string key, int fallback) =>
        o[key] is JsonValue v && v.TryGetValue<double>(out var d) && double.IsFinite(d) ? (int)d : fallback;

    private static bool GetBool(JsonObject o, string key, bool fallback)
    {
        if (o[key] is not JsonValue v) return fallback;
        if (v.TryGetValue<bool>(out var b)) return b;
        if (v.TryGetValue<string>(out var s)) return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
        return fallback;
    }
}
