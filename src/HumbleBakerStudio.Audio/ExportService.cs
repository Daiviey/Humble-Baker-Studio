using System.Diagnostics;
using System.Globalization;
using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Audio;

public sealed record ExportFormat(string Name, string Extension, string? FfmpegArgs)
{
    public bool NeedsFfmpeg => FfmpegArgs != null;
    public override string ToString() => Name;
}

/// <summary>Offline rendering and file export.</summary>
public static class ExportService
{
    public static readonly ExportFormat[] Formats =
    {
        new("WAV 16-bit", ".wav", null),
        new("WAV 24-bit", ".wav", null),
        new("WAV 32-bit float", ".wav", null),
        new("MP3 320 kbps", ".mp3", "-c:a libmp3lame -b:a 320k"),
        new("MP3 192 kbps", ".mp3", "-c:a libmp3lame -b:a 192k"),
        new("MP3 V0 (VBR)", ".mp3", "-c:a libmp3lame -q:a 0"),
        new("OGG Vorbis q6", ".ogg", "-c:a libvorbis -q:a 6"),
        new("OGG Vorbis q9", ".ogg", "-c:a libvorbis -q:a 9"),
        new("FLAC lossless", ".flac", "-c:a flac")
    };

    public static (float[] L, float[] R) Render(Project p, bool song, bool loop, bool master)
    {
        Dsp.Mix(p, song, loop, out var l, out var r);
        if (master && p.Master is { Enabled: true }) Mastering.Process(l, r, p.Master);
        return (l, r);
    }

    public static short[] RenderPcm(Project p, bool song, bool loop, bool master, int repeats = 1)
    {
        var (l, r) = Render(p, song, loop, master);
        if (repeats > 1 && !song)
        {
            l = Repeat(l, repeats);
            r = Repeat(r, repeats);
        }
        return ToPcm(l, r, master && p.Master is { Enabled: true });
    }

    public static short[] ToPcm(float[] l, float[] r, bool mastered)
    {
        int n = Math.Min(l.Length, r.Length);
        var pcm = new short[n * 2];
        for (int i = 0; i < n; i++)
        {
            double a = mastered ? l[i] : Math.Tanh(l[i]);
            double b = mastered ? r[i] : Math.Tanh(r[i]);
            double sc = mastered ? 32767.0 : 30000.0;
            pcm[2 * i] = (short)Math.Max(-32767, Math.Min(32767, Math.Round(a * sc)));
            pcm[2 * i + 1] = (short)Math.Max(-32767, Math.Min(32767, Math.Round(b * sc)));
        }
        return pcm;
    }

    public static void Export(Project project, string path, ExportFormat format,
        bool song, bool loop, int repeats, bool normalize, double targetDb,
        int fadeInMs, int fadeOutMs, string? ffmpegPath)
    {
        var (l, r) = Render(project, song, loop, master: true);
        if (repeats > 1 && !song)
        {
            l = Repeat(l, repeats);
            r = Repeat(r, repeats);
        }

        ApplyGainAndFades(l, r, normalize, targetDb, fadeInMs, fadeOutMs, Dsp.SR);

        if (!format.NeedsFfmpeg)
        {
            int bits = format.Name.Contains("24") ? 24 : format.Name.Contains("32") ? 32 : 16;
            WriteWav(path, l, r, bits);
            return;
        }

        var ffmpeg = FindFfmpeg(ffmpegPath);
        if (ffmpeg == null)
            throw new InvalidOperationException("FFmpeg is needed for this format. Set its path in Settings, or export WAV.");

        var temp = Path.Combine(Path.GetTempPath(), "hbs-export-" + Guid.NewGuid().ToString("N") + ".wav");
        try
        {
            WriteWav(temp, l, r, bits: 16);
            var args = "-y -hide_banner -loglevel error -i " + Quote(temp) + " " + format.FfmpegArgs + " " + Quote(path);
            var psi = new ProcessStartInfo(ffmpeg, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Could not start FFmpeg.");
            string err = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new InvalidOperationException("FFmpeg failed: " + err.Trim());
        }
        finally
        {
            try { File.Delete(temp); } catch { }
        }
    }

    public static string? FindFfmpeg(string? configured, string? dataDir = null)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configured)) candidates.Add(configured!);
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg.exe"));
        if (!string.IsNullOrEmpty(dataDir)) candidates.Add(Path.Combine(dataDir, "ffmpeg", "ffmpeg.exe"));
        candidates.Add(@"Z:\LocalAI\Tools\ffmpeg.exe");
        candidates.Add(@"Z:\LocalAI\Audio\studio\ffmpeg\ffmpeg.exe");
        foreach (var c in candidates)
            if (File.Exists(c)) return c;

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var exe = Path.Combine(dir.Trim('"'), "ffmpeg.exe");
                if (File.Exists(exe)) return exe;
            }
            catch { }
        }
        return null;
    }

    public static void WriteWav(string path, float[] l, float[] r, int bits)
    {
        int frames = Math.Min(l.Length, r.Length);
        int channels = 2;
        int bytesPerSample = bits / 8;
        int blockAlign = channels * bytesPerSample;
        int dataBytes = frames * blockAlign;

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);
        int formatTag = bits == 32 ? 3 : 1;

        bw.Write(new[] { 'R', 'I', 'F', 'F' });
        bw.Write(36 + dataBytes);
        bw.Write(new[] { 'W', 'A', 'V', 'E' });
        bw.Write(new[] { 'f', 'm', 't', ' ' });
        bw.Write(16);
        bw.Write((short)formatTag);
        bw.Write((short)channels);
        bw.Write(Dsp.SR);
        bw.Write(Dsp.SR * blockAlign);
        bw.Write((short)blockAlign);
        bw.Write((short)bits);
        bw.Write(new[] { 'd', 'a', 't', 'a' });
        bw.Write(dataBytes);

        for (int i = 0; i < frames; i++)
        {
            WriteSample(bw, l[i], bits);
            WriteSample(bw, r[i], bits);
        }
    }

    private static void WriteSample(BinaryWriter bw, float value, int bits)
    {
        double v = Math.Max(-1.0, Math.Min(1.0, value));
        if (bits == 16)
            bw.Write((short)Math.Round(v * 32767));
        else if (bits == 24)
        {
            int s = (int)Math.Round(v * 8388607);
            bw.Write((byte)(s & 0xFF));
            bw.Write((byte)((s >> 8) & 0xFF));
            bw.Write((byte)((s >> 16) & 0xFF));
        }
        else
            bw.Write((float)v);
    }

    private static float[] Repeat(float[] src, int loops)
    {
        loops = Math.Max(1, loops);
        if (loops == 1) return src;
        var dst = new float[src.Length * loops];
        for (int i = 0; i < loops; i++) Array.Copy(src, 0, dst, i * src.Length, src.Length);
        return dst;
    }

    private static void ApplyGainAndFades(float[] l, float[] r, bool normalize, double targetDb, int fadeInMs, int fadeOutMs, int sr)
    {
        if (normalize)
        {
            double peak = 0;
            for (int i = 0; i < l.Length; i++) peak = Math.Max(peak, Math.Max(Math.Abs(l[i]), Math.Abs(r[i])));
            if (peak > 1e-9)
            {
                double target = Math.Pow(10, targetDb / 20.0);
                float g = (float)(target / peak);
                for (int i = 0; i < l.Length; i++) { l[i] *= g; r[i] *= g; }
            }
        }
        if (fadeInMs > 0)
        {
            int n = Math.Min(l.Length, (int)(fadeInMs * (double)sr / 1000));
            for (int i = 0; i < n; i++)
            {
                float g = (float)i / n;
                l[i] *= g; r[i] *= g;
            }
        }
        if (fadeOutMs > 0)
        {
            int n = Math.Min(l.Length, (int)(fadeOutMs * (double)sr / 1000));
            for (int i = 0; i < n; i++)
            {
                float g = (float)i / n;
                int idx = l.Length - 1 - i;
                l[idx] *= g; r[idx] *= g;
            }
        }
    }

    public static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        name = name.Trim();
        return string.IsNullOrEmpty(name) ? "Untitled" : name;
    }

    private static string Quote(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";
}

public static class DurationText
{
    public static string Format(TimeSpan t) =>
        t.TotalHours >= 1
            ? t.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : t.ToString(@"m\:ss", CultureInfo.InvariantCulture);
}
