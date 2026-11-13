namespace HumbleBakerStudio.Audio;

/// <summary>Artist-facing parameters for the deterministic pattern composer (no AI).</summary>
public sealed class PatternRequest
{
    public string Role = "Lead";
    public string Style = "Any";
    public string Mood = "Any";
    public int Bars = 4;
    public double Energy = 0.6;
    public double Complexity = 0.5;
    public double Density = 0.6;
    public int Seed = 1;
    public int Key = 0;
    public string Scale = "minor";
    public double BarBeats = 4;
    public bool SnapToScale = true;
    public bool Humanize = true;
}

public sealed class PatternResult
{
    public string Name = "";
    public string Style = "";
    public string Mood = "";
    public string Description = "";
    public int Seed;
    public List<Note> Notes = new();
}

/// <summary>A pre-written MIDI example with metadata used by the selector.</summary>
public sealed class PatternExample
{
    public string Id = "";
    public string Name = "";
    public string Role = "";
    public string Style = "";
    public string Mood = "";
    public string Description = "";
    public int Bars = 1;
    public double Energy = 0.5;
    public double Complexity = 0.5;
    public double Density = 0.5;
    public bool Drums;
    public string Scale = "minor";
    public int Root = 0;             // patterns are written in C
    public List<Note> Notes = new();
}

/// <summary>
/// Selects from a curated library of pre-programmed MIDI examples and adapts the chosen
/// example to the project key, bars, density, energy and humanisation. Deterministic for a
/// given seed; no model, network or external service is involved.
/// </summary>
public static class PatternComposer
{
    public static IReadOnlyList<PatternExample> Patterns => Library;

    public static PatternResult Compose(PatternRequest request)
    {
        var rnd = new Random(request.Seed == 0 ? 1 : request.Seed);
        var candidates = Library.Where(p => p.Role.Equals(request.Role, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0) candidates = Library.ToList();

        var scored = candidates.Select(p => (Pattern: p, Score: Score(p, request, rnd))).ToList();
        var best = scored.OrderByDescending(x => x.Score).First().Pattern;

        var result = new PatternResult
        {
            Name = best.Name,
            Style = best.Style,
            Mood = best.Mood,
            Seed = request.Seed,
            Description = $"{best.Name} - {best.Style} / {best.Mood} (energy {best.Energy:0.00}, complexity {best.Complexity:0.00})"
        };

        int repeats = Math.Max(1, (int)Math.Ceiling(request.Bars / (double)Math.Max(1, best.Bars)));
        double timeScale = request.BarBeats / 4.0;
        double totalBeats = request.Bars * request.BarBeats;
        bool[]? mask = request.SnapToScale && !best.Drums ? Scales.Mask(request.Key, request.Scale) : null;
        int transpose = best.Drums ? 0 : request.Key - best.Root;
        var human = new Random((request.Seed == 0 ? 1 : request.Seed) * 7919 + 17);

        for (int rep = 0; rep < repeats; rep++)
        {
            double offset = rep * best.Bars * request.BarBeats;
            foreach (var note in best.Notes)
            {
                double start = note.Start * timeScale + offset;
                if (start >= totalBeats - 1e-6) continue;

                if (!best.Drums && request.Density < 0.5 && human.NextDouble() < (0.5 - request.Density) * 1.2)
                    continue;

                int pitch = note.Pitch;
                if (!best.Drums)
                {
                    pitch = Math.Clamp(pitch + transpose, 0, 127);
                    if (mask != null) pitch = Scales.Snap(pitch, mask);
                }

                int velocity = (int)Math.Round(note.Vel * (0.65 + request.Energy * 0.55));
                if (request.Humanize)
                {
                    start += (human.NextDouble() - 0.5) * 0.02;
                    velocity += human.Next(5) - 2;
                }

                result.Notes.Add(new Note(
                    pitch,
                    Math.Max(0, start),
                    Math.Max(0.02, note.Len * timeScale),
                    Math.Clamp(velocity, 1, 127)));
            }
        }

        result.Notes.Sort((a, b) => a.Start.CompareTo(b.Start));
        return result;
    }

    private static double Score(PatternExample pattern, PatternRequest request, Random rnd)
    {
        double score = 0;
        if (!string.Equals(request.Style, "Any", StringComparison.OrdinalIgnoreCase) &&
            pattern.Style.Equals(request.Style, StringComparison.OrdinalIgnoreCase)) score += 3;
        if (!string.Equals(request.Mood, "Any", StringComparison.OrdinalIgnoreCase) &&
            pattern.Mood.Equals(request.Mood, StringComparison.OrdinalIgnoreCase)) score += 2;
        score += (1 - Math.Abs(pattern.Energy - request.Energy)) * 3;
        score += (1 - Math.Abs(pattern.Complexity - request.Complexity)) * 2;
        score += (1 - Math.Abs(pattern.Density - request.Density)) * 2;
        if (pattern.Bars == request.Bars) score += 1;
        score += rnd.NextDouble() * 0.75;
        return score;
    }

    // ------------------------------------------------------------------ library

    private static readonly List<PatternExample> Library = BuildLibrary();

    private static PatternExample Make(
        string id, string name, string role, string style, string mood, int bars,
        double energy, double complexity, double density, bool drums, string description)
    {
        return new PatternExample
        {
            Id = id,
            Name = name,
            Role = role,
            Style = style,
            Mood = mood,
            Bars = bars,
            Energy = energy,
            Complexity = complexity,
            Density = density,
            Drums = drums,
            Description = description
        };
    }

    private static void N(PatternExample p, int pitch, double start, double len, int vel)
        => p.Notes.Add(new Note(pitch, start, len, vel));

    private static List<PatternExample> BuildLibrary()
    {
        var list = new List<PatternExample>();

        // ---- drums ----
        var techno = Make("drums-techno", "Techno four-on-floor", "Drums", "Techno", "Hypnotic", 1, 0.85, 0.3, 0.7, true,
            "Kick on every beat, offbeat hats and a backbeat clap.");
        for (int i = 0; i < 4; i++) N(techno, 36, i, 0.25, 122);
        for (int i = 0; i < 8; i++) N(techno, 42, i * 0.5 + 0.5, 0.08, i % 2 == 0 ? 76 : 58);
        N(techno, 39, 1, 0.2, 104); N(techno, 39, 3, 0.2, 108);
        N(techno, 46, 3.5, 0.25, 84);
        list.Add(techno);

        var house = Make("drums-house", "House shuffle", "Drums", "House", "Uplifting", 1, 0.75, 0.45, 0.75, true,
            "Four-on-floor with offbeat hats and a light shaker layer.");
        for (int i = 0; i < 4; i++) N(house, 36, i, 0.25, 118);
        for (int i = 0; i < 16; i++) N(house, i % 4 == 0 ? 39 : 42, i * 0.25 + 0.25, 0.07, i % 4 == 0 ? 92 : 58);
        N(house, 46, 3.5, 0.2, 82);
        list.Add(house);

        var breakbeat = Make("drums-break", "Breakbeat", "Drums", "Breakbeat", "Aggressive", 2, 0.9, 0.7, 0.85, true,
            "Syncopated kick, backbeat snare and fast hats across two bars.");
        double[] kicks = { 0, 1.5, 3.75, 4, 5.5, 7.5 };
        foreach (double k in kicks) N(breakbeat, 36, k, 0.22, 118);
        for (int bar = 0; bar < 2; bar++) { N(breakbeat, 38, bar * 4 + 1, 0.2, 112); N(breakbeat, 38, bar * 4 + 3, 0.2, 116); }
        for (int i = 0; i < 16; i++) N(breakbeat, 42, i * 0.5, 0.07, i % 2 == 0 ? 80 : 52);
        list.Add(breakbeat);

        var boombap = Make("drums-boombap", "Boom bap", "Drums", "Hip-hop", "Melancholic", 2, 0.55, 0.45, 0.6, true,
            "Laid-back kick and snare with swung eighth hats.");
        foreach (double k in new[] { 0.0, 0.75, 2.5, 4.0, 4.75, 6.5 }) N(boombap, 36, k, 0.25, 114);
        for (int bar = 0; bar < 2; bar++) { N(boombap, 38, bar * 4 + 1, 0.2, 110); N(boombap, 38, bar * 4 + 3, 0.2, 112); }
        for (int i = 0; i < 16; i++) N(boombap, 42, i * 0.5, 0.07, i % 2 == 0 ? 74 : 48);
        list.Add(boombap);

        var halftime = Make("drums-halftime", "Half-time", "Drums", "Bass", "Dark", 2, 0.7, 0.35, 0.5, true,
            "Slow snare backbeat with sparse kick and hats.");
        foreach (double k in new[] { 0.0, 2.75, 4.0, 6.75 }) N(halftime, 36, k, 0.28, 120);
        N(halftime, 38, 2, 0.25, 116); N(halftime, 38, 6, 0.25, 116);
        for (int i = 0; i < 8; i++) N(halftime, 42, i * 1.0 + 0.5, 0.08, 60);
        list.Add(halftime);

        // ---- bass ----
        var rolling = Make("bass-rolling", "Rolling techno bass", "Bassline", "Techno", "Hypnotic", 1, 0.8, 0.55, 0.85, false,
            "Driving sixteenth-note root pattern with small octave lifts.");
        int[] rollingSteps = { 0, 0, 0, 0, 0, 0, 12, 0, 0, 0, 0, 0, 7, 0, 0, 0 };
        for (int i = 0; i < 16; i++) N(rolling, 36 + rollingSteps[i], i * 0.25, 0.18, i % 4 == 0 ? 112 : 84);
        list.Add(rolling);

        var acid = Make("bass-acid", "Acid bass", "Bassline", "Acid", "Aggressive", 1, 0.9, 0.75, 0.9, false,
            "Squelchy sixteenth-note acid line with octave jumps.");
        int[] acidSteps = { 0, 0, 3, 0, 7, 0, -2, 0, 0, 10, 0, 3, 0, 0, 5, 0 };
        for (int i = 0; i < 16; i++) N(acid, 36 + acidSteps[i], i * 0.25, 0.2, i % 4 == 0 ? 120 : 88);
        list.Add(acid);

        var sub = Make("bass-sub", "Sub pulse", "Bassline", "Any", "Dark", 2, 0.35, 0.2, 0.35, false,
            "Long root notes that leave space for the kick.");
        for (int bar = 0; bar < 2; bar++) { N(sub, 33, bar * 4, 1.8, 100); N(sub, 33, bar * 4 + 2, 1.8, 94); }
        list.Add(sub);

        var funk = Make("bass-funk", "Funk bass", "Bassline", "Funk", "Uplifting", 2, 0.7, 0.8, 0.65, false,
            "Syncopated funk line with an octave answer.");
        double[] funkStarts = { 0, 0.75, 1.5, 2.25, 2.75, 4, 4.75, 5.5, 6.25, 6.75 };
        int[] funkPitches = { 36, 36, 39, 43, 36, 36, 43, 39, 36, 43 };
        for (int i = 0; i < funkStarts.Length; i++) N(funk, funkPitches[i], funkStarts[i], 0.2, 92 + (i % 3) * 8);
        list.Add(funk);

        // ---- chords / pads ----
        var darkPad = Make("chords-dark", "Dark pad", "Chords", "Techno", "Dark", 4, 0.4, 0.45, 0.5, false,
            "Slow minor chords with an added seventh on the turnaround.");
        int[][] darkBars = { new[] { 48, 51, 55 }, new[] { 48, 51, 55 }, new[] { 46, 50, 53 }, new[] { 48, 51, 55, 58 } };
        for (int bar = 0; bar < 4; bar++) foreach (int pitch in darkBars[bar]) N(darkPad, pitch, bar * 4, 4, 76);
        list.Add(darkPad);

        var brightPad = Make("chords-bright", "Uplifting pad", "Chords", "House", "Uplifting", 4, 0.65, 0.45, 0.65, false,
            "Four-bar major progression with open voicings.");
        int[][] brightBars = { new[] { 48, 52, 55 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 }, new[] { 57, 60, 64 } };
        for (int bar = 0; bar < 4; bar++) foreach (int pitch in brightBars[bar]) N(brightPad, pitch, bar * 4, 4, 82);
        list.Add(brightPad);

        var ambient = Make("chords-ambient", "Ambient fifths", "Chords", "Ambient", "Dreamy", 4, 0.25, 0.25, 0.3, false,
            "Sparse open fifths that ring across the whole section.");
        for (int bar = 0; bar < 4; bar++)
        {
            int root = bar % 2 == 0 ? 36 : 43;
            N(ambient, root, bar * 4, 4, 64);
            N(ambient, root + 7, bar * 4, 4, 58);
            N(ambient, root + 12, bar * 4, 4, 52);
        }
        list.Add(ambient);

        var stabs = Make("chords-stabs", "Offbeat stabs", "Chords", "House", "Hypnotic", 2, 0.75, 0.6, 0.55, false,
            "Short minor stabs on the offbeats.");
        for (int bar = 0; bar < 2; bar++)
            foreach (double off in new[] { 0.5, 1.5, 2.5, 3.5 })
                foreach (int pitch in new[] { 48, 51, 55 })
                    N(stabs, pitch, bar * 4 + off, 0.18, 88);
        list.Add(stabs);

        // ---- leads / arps / counter ----
        var arp = Make("lead-arp", "Hypnotic arp", "Arpeggio", "Techno", "Hypnotic", 2, 0.7, 0.65, 0.85, false,
            "Sixteenth-note arpeggio cycling a minor seventh chord.");
        int[] arpPitches = { 60, 63, 67, 70, 67, 63, 60, 63 };
        for (int i = 0; i < 32; i++) N(arp, arpPitches[i % arpPitches.Length], i * 0.25, 0.18, i % 4 == 0 ? 96 : 74);
        list.Add(arp);

        var lead = Make("lead-melancholy", "Melancholic lead", "Lead", "Techno", "Melancholic", 2, 0.55, 0.5, 0.4, false,
            "Sparse descending motif with long held notes.");
        double[] leadStarts = { 0, 1, 2.5, 4, 5, 6.5 };
        int[] leadPitches = { 67, 70, 72, 70, 67, 63 };
        for (int i = 0; i < leadStarts.Length; i++) N(lead, leadPitches[i], leadStarts[i], i % 2 == 0 ? 0.8 : 0.5, 88);
        list.Add(lead);

        var counter = Make("lead-counter", "Pluck counter", "Counter", "House", "Uplifting", 2, 0.6, 0.4, 0.5, false,
            "Offbeat plucks that answer the main chord.");
        for (int bar = 0; bar < 2; bar++)
            foreach (double off in new[] { 0.5, 1.5, 2.5, 3.5 })
                N(counter, 72 + (int)(off * 2) % 5, bar * 4 + off, 0.2, 78);
        list.Add(counter);

        var ostinato = Make("lead-ostinato", "Cinematic ostinato", "Lead", "Cinematic", "Dark", 4, 0.65, 0.6, 0.8, false,
            "Driving eighth-note ostinato that builds through the section.");
        int[] ostinatoPitches = { 48, 55, 60, 63, 60, 55, 48, 55 };
        for (int i = 0; i < 32; i++) N(ostinato, ostinatoPitches[i % ostinatoPitches.Length], i * 0.5, 0.4, 88 + (i % 4) * 6);
        list.Add(ostinato);

        return list;
    }
}
