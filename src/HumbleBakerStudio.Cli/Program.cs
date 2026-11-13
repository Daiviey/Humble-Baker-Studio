using HumbleBakerStudio.Audio;

// Cross-platform headless renderer. The same code path runs on Windows, macOS and Linux;
// it is the first milestone of the native macOS port.

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintUsage();
    return 0;
}

string command = args[0].ToLowerInvariant();
try
{
    switch (command)
    {
        case "demo":
            return RenderDemo(args);
        case "render":
            return RenderProject(args);
        case "info":
            return ProjectInfo(args);
        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("error: " + ex.Message);
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("hbs-render - cross-platform Humble Baker Studio renderer");
    Console.WriteLine();
    Console.WriteLine("  hbs-render demo [out.wav]                 render a default demo project");
    Console.WriteLine("  hbs-render info <project.json>            print project summary");
    Console.WriteLine("  hbs-render render <project.json> <out.wav> [--song] [--no-master] [--repeats N]");
    Console.WriteLine();
    Console.WriteLine("The project format is the same *.hbstudio.json file used by the desktop apps.");
}

static int RenderDemo(string[] args)
{
    string output = args.Length > 1 ? args[1] : "hbs-demo.wav";
    var project = ProjectStore.CreateDefault();
    Render(project, output, song: false, master: true, repeats: 1);
    return 0;
}

static int RenderProject(string[] args)
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("usage: hbs-render render <project.json> <out.wav> [--song] [--no-master] [--repeats N]");
        return 2;
    }

    string input = args[1], output = args[2];
    bool song = args.Contains("--song", StringComparer.OrdinalIgnoreCase);
    bool master = !args.Contains("--no-master", StringComparer.OrdinalIgnoreCase);
    int repeats = 1;
    int repeatIndex = Array.FindIndex(args, a => string.Equals(a, "--repeats", StringComparison.OrdinalIgnoreCase));
    if (repeatIndex >= 0 && repeatIndex + 1 < args.Length && int.TryParse(args[repeatIndex + 1], out int parsed))
        repeats = Math.Max(1, parsed);

    var project = ProjectStore.Load(input);
    Render(project, output, song, master, repeats);
    return 0;
}

static int ProjectInfo(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("usage: hbs-render info <project.json>");
        return 2;
    }

    var p = ProjectStore.Load(args[1]);
    Console.WriteLine($"BPM {p.Bpm:0.##}, {p.Bars} bars, {p.BeatsPerBar}/{p.BeatUnit}, key {p.Key} {p.Scale}");
    Console.WriteLine($"Tracks: {p.Tracks.Count}, patterns: {p.Patterns.Count}, song entries: {p.Song.Count}, buses: {p.Buses.Count}");
    foreach (var t in p.Tracks)
        Console.WriteLine($"  {t.Name,-24} {t.Synth,-8} notes {t.Notes.Count,4}  automation {t.Auto.Count}");
    return 0;
}

static void Render(Project project, string output, bool song, bool master, int repeats)
{
    string full = Path.GetFullPath(output);
    string? dir = Path.GetDirectoryName(full);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

    var pcm = ExportService.RenderPcm(project, song, loop: !song, master: master, repeats: repeats);
    Dsp.WriteWav(full, pcm);

    double seconds = pcm.Length / 2.0 / Dsp.SR;
    int peak = 0;
    foreach (short sample in pcm) peak = Math.Max(peak, Math.Abs((int)sample));
    Console.WriteLine($"Rendered {seconds:0.0} s, {pcm.Length / 2} frames, peak {peak} -> {full}");
}
