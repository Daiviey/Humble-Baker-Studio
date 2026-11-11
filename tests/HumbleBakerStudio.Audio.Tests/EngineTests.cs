using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Audio.Tests;

public class EngineTests
{
    private static Project BuildProject()
    {
        var p = ProjectStore.CreateDefault();
        var acid = p.Tracks[1];
        acid.Synth = "patch";
        acid.Patch = new Patch { Attack = 0.005, Decay = 0.15, Sustain = 0.2, Release = 0.1, Cutoff = 900, Resonance = 0.6 };
        for (int i = 0; i < 16; i++)
            acid.Notes.Add(new Note(36 + (i % 5) * 2, i * 0.25, 0.2, 90 + (i % 3) * 10));

        var drums = p.Tracks[0];
        for (int bar = 0; bar < p.Bars; bar++)
        {
            drums.Notes.Add(new Note(36, bar * 4, 0.25, 120));
            drums.Notes.Add(new Note(38, bar * 4 + 1, 0.25, 110));
            drums.Notes.Add(new Note(42, bar * 4 + 0.5, 0.1, 70));
            drums.Notes.Add(new Note(42, bar * 4 + 1.5, 0.1, 70));
        }

        var pad = p.Tracks[2];
        for (int bar = 0; bar < p.Bars; bar++)
        {
            pad.Notes.Add(new Note(60, bar * 4, 4, 80));
            pad.Notes.Add(new Note(63, bar * 4, 4, 78));
            pad.Notes.Add(new Note(67, bar * 4, 4, 76));
        }
        return p;
    }

    [Fact]
    public void DefaultProject_RendersStereoAudio()
    {
        var p = BuildProject();
        var pcm = Dsp.Render(p, loop: true, master: false);

        double spb = 60.0 / p.Bpm;
        int expectedFrames = (int)Math.Round(p.LoopBeats * spb * Dsp.SR);
        Assert.Equal(expectedFrames * 2, pcm.Length);

        Assert.Contains(pcm, s => s != 0);
        Assert.All(pcm, s => Assert.InRange(s, (short)-32767, (short)32767));
    }

    [Fact]
    public void Render_IsDeterministic()
    {
        var a = Dsp.Render(BuildProject(), loop: true, master: false);
        var b = Dsp.Render(BuildProject(), loop: true, master: false);
        Assert.Equal(a, b);
    }

    [Fact]
    public void MasterChain_ChangesPeakButStaysInsideRange()
    {
        var p = BuildProject();
        p.Master.Enabled = true;
        p.Master.Limiter = true;
        p.Master.InputGain = 6;
        var pcm = Dsp.Render(p, loop: true, master: true);
        Assert.Contains(pcm, s => s != 0);
        Assert.All(pcm, s => Assert.InRange(s, (short)-32767, (short)32767));
    }

    [Fact]
    public void ProjectStore_RoundTripsTheWholeProject()
    {
        var original = BuildProject();
        original.Bpm = 128.5;
        original.Bars = 6;
        original.Key = 3;
        original.Scale = "dorian";
        original.Tracks[1].Fx.DelayMix = 0.33;
        original.Tracks[1].Patch!.FmAmount = 2.5;
        original.Patterns[0].Name = "Verse";
        original.Patterns[0].Notes.Clear();
        original.Patterns[0].Notes.Add(new List<Note> { new(40, 0, 0.5, 100) });
        original.Graph.Initialized = true;
        original.Graph.Nodes.Add(new PatchNode { Id = "n1", Kind = "track", Type = "track", Label = "Acid", X = 40, Y = 60 });

        var path = Path.Combine(Path.GetTempPath(), "hbs-test-" + Guid.NewGuid().ToString("N") + ".hbstudio.json");
        try
        {
            ProjectStore.Save(original, path);
            Assert.True(File.Exists(path));

            var loaded = ProjectStore.Load(path);
            Assert.Equal(128.5, loaded.Bpm, 3);
            Assert.Equal(6, loaded.Bars);
            Assert.Equal(3, loaded.Key);
            Assert.Equal("dorian", loaded.Scale);
            Assert.Equal(original.Tracks.Count, loaded.Tracks.Count);
            Assert.Equal(original.Tracks[0].Notes.Count, loaded.Tracks[0].Notes.Count);
            Assert.Equal(0.33, loaded.Tracks[1].Fx.DelayMix, 3);
            Assert.Equal(2.5, loaded.Tracks[1].Patch!.FmAmount, 3);
            Assert.Equal("Verse", loaded.Patterns[0].Name);
            Assert.True(loaded.Graph.Initialized);
            Assert.Single(loaded.Graph.Nodes);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void WavWriter_ProducesValidHeaderAndPayload(int bits)
    {
        var l = new float[1000];
        var r = new float[1000];
        for (int i = 0; i < 1000; i++) { l[i] = (float)Math.Sin(i * 0.05) * 0.5f; r[i] = -l[i]; }

        var path = Path.Combine(Path.GetTempPath(), $"hbs-wav-{bits}-{Guid.NewGuid():N}.wav");
        try
        {
            ExportService.WriteWav(path, l, r, bits);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.Equal("WAVE", System.Text.Encoding.ASCII.GetString(bytes, 8, 4));
            Assert.Equal("data", System.Text.Encoding.ASCII.GetString(bytes, 36, 4));
            Assert.Equal(44 + 1000 * 2 * (bits / 8), bytes.Length);
            int formatTag = BitConverter.ToInt16(bytes, 20);
            Assert.Equal(bits == 32 ? 3 : 1, formatTag);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Export_WritesAudibleWav()
    {
        var p = BuildProject();
        var path = Path.Combine(Path.GetTempPath(), "hbs-export-" + Guid.NewGuid().ToString("N") + ".wav");
        try
        {
            ExportService.Export(p, path, ExportService.Formats[0], song: false, loop: true, repeats: 1,
                normalize: true, targetDb: -1, fadeInMs: 10, fadeOutMs: 10, ffmpegPath: null);
            var info = new FileInfo(path);
            Assert.True(info.Length > 44);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.Contains(bytes.Skip(44), b => b != 0);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ScalesAndHarmony_AreConsistent()
    {
        var mask = Scales.Mask(0, "minor");
        Assert.True(mask[0]);
        Assert.True(mask[3]);
        Assert.True(mask[7]);
        Assert.False(mask[1]);
        Assert.Equal(60, Scales.Snap(61, mask));
    }

    [Fact]
    public void MidiWriter_WritesAFile()
    {
        var p = BuildProject();
        var path = Path.Combine(Path.GetTempPath(), "hbs-" + Guid.NewGuid().ToString("N") + ".mid");
        try
        {
            MidiWriter.Write(p, path);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal("MThd", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
