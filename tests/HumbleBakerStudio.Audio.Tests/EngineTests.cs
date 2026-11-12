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

public class UndoHistoryTests
{
    [Fact]
    public void UndoAndRedo_RestoreProjectState()
    {
        var p = ProjectStore.CreateDefault();
        var undo = new UndoHistory();
        undo.Reset(p);

        p.Bpm = 180;
        undo.Record(p, "tempo");
        p.Tracks[0].Name = "Kit";
        undo.Record(p, "rename");

        Assert.True(undo.CanUndo);
        var once = undo.Undo(p)!;
        Assert.NotEqual("Kit", once.Tracks[0].Name);   // the rename is undone first
        Assert.Equal(180, once.Bpm, 3);

        var twice = undo.Undo(once)!;
        Assert.Equal(124, twice.Bpm, 3);               // then the tempo change

        Assert.True(undo.CanRedo);
        var forward = undo.Redo(twice)!;
        Assert.Equal(180, forward.Bpm, 3);
    }

    [Fact]
    public void Record_CoalescesRapidEditsWithTheSameLabel()
    {
        var p = ProjectStore.CreateDefault();
        var undo = new UndoHistory { CoalesceMilliseconds = 5000 };
        undo.Reset(p);

        for (int i = 0; i < 20; i++)
        {
            p.Tracks[0].Vol = i / 20.0;
            undo.Record(p, "volume");
        }

        var restored = undo.Undo(p)!;
        Assert.Equal(0.9, restored.Tracks[0].Vol, 3); // original default for the drum track
        Assert.False(undo.CanUndo);                    // one gesture, one step
    }

    [Fact]
    public void Reset_ClearsHistory()
    {
        var p = ProjectStore.CreateDefault();
        var undo = new UndoHistory();
        undo.Reset(p);
        p.Bpm = 99;
        undo.Record(p, "tempo");
        Assert.True(undo.CanUndo);
        undo.Reset(p);
        Assert.False(undo.CanUndo);
        Assert.False(undo.CanRedo);
    }
}

public class SuperColliderExportTests
{
    [Fact]
    public void Script_ContainsVoicesPatternsAndClock()
    {
        var p = ProjectStore.CreateDefault();
        p.Tracks[1].Notes.Add(new Note(36, 0, 0.25, 100));
        p.Tracks[1].Notes.Add(new Note(43, 1, 0.25, 100));
        var dir = Path.Combine(Path.GetTempPath(), "hbs-sc-" + Guid.NewGuid().ToString("N"));
        try
        {
            var script = SuperColliderExport.ToScript(p, song: false, dir, out int samples);
            Assert.Equal(0, samples);
            Assert.Contains(@"SynthDef(\hbvoice", script);
            Assert.Contains(@"SynthDef(\hbkick", script);
            Assert.Contains("Pbind(", script);
            Assert.Contains("TempoClock(", script);
            Assert.Contains("~hbplay", script);
            Assert.Contains(@"\instrument, Pseq", script);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SampleTrack_WritesBufferAndReferencesIt()
    {
        var p = ProjectStore.CreateDefault();
        var t = ProjectStore.NewTrack("Sample", "sample");
        t.Sample = Enumerable.Repeat(0.1f, 5000).ToArray();
        t.Notes.Add(new Note(60, 0, 1, 100));
        p.Tracks.Add(t);
        var dir = Path.Combine(Path.GetTempPath(), "hbs-sc-" + Guid.NewGuid().ToString("N"));
        try
        {
            var script = SuperColliderExport.ToScript(p, song: false, dir, out int samples);
            Assert.Equal(1, samples);
            Assert.True(File.Exists(Path.Combine(dir, "sample-3.wav")));
            Assert.Contains("Buffer.read", script);
            Assert.Contains(@"\hbsamp", script);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}

public class LiveMixerTests
{
    private static LiveDeck Deck(double value, int frames = 1000, double bpm = 120)
    {
        var d = new LiveDeck();
        var buf = Enumerable.Repeat((float)value, frames).ToArray();
        d.SetBuffer("test", "", buf, buf, bpm, "A minor", songMode: false);
        d.Playing = true;
        return d;
    }

    [Fact]
    public void Crossfader_BlendsDecksWithEqualPower()
    {
        var mix = new LiveMixer { Crossfade = 0, MasterGain = 1, CueLevel = 1 };
        mix.A.SetBuffer("a", "", Enumerable.Repeat(0.4f, 1000).ToArray(), Enumerable.Repeat(0.4f, 1000).ToArray(), 120, "", false);
        mix.B.SetBuffer("b", "", Enumerable.Repeat(0.2f, 1000).ToArray(), Enumerable.Repeat(0.2f, 1000).ToArray(), 120, "", false);
        mix.A.Playing = mix.B.Playing = true;

        var ml = new float[100]; var mr = new float[100]; var cl = new float[100]; var cr = new float[100];
        mix.Fill(ml, mr, cl, cr, 100);
        Assert.Equal(0.4, ml[0], 3);
        Assert.Equal(0.4, mr[0], 3);

        mix.Crossfade = 1;
        mix.Fill(ml, mr, cl, cr, 100);
        Assert.Equal(0.2, ml[0], 3);
    }

    [Fact]
    public void CueBus_IsIndependentOfTheCrossfader()
    {
        var mix = new LiveMixer { Crossfade = 1, MasterGain = 1, CueLevel = 0.5 };
        mix.A.SetBuffer("a", "", Enumerable.Repeat(0.4f, 1000).ToArray(), Enumerable.Repeat(0.4f, 1000).ToArray(), 120, "", false);
        mix.A.Playing = true;
        mix.A.Cue = true;

        var ml = new float[100]; var mr = new float[100]; var cl = new float[100]; var cr = new float[100];
        mix.Fill(ml, mr, cl, cr, 100);
        Assert.Equal(0, ml[0], 3);       // A is fully faded out of master
        Assert.Equal(0.2, cl[0], 3);     // but still audible in cue
    }

    [Fact]
    public void Deck_LoopsAndAdvancesByRate()
    {
        var d = Deck(0.3, frames: 500);
        d.Rate = 2;
        var l = new float[600]; var r = new float[600];
        d.Render(l, r, 0, 600);
        Assert.True(d.Playing);
        // 600 rendered frames at rate 2 = 1200 source frames, wraps once around 500.
        Assert.InRange(d.PositionSeconds * Dsp.SR, 199, 201);
    }

    [Fact]
    public void Deck_SongModeStopsAtTheEnd()
    {
        var d = Deck(0.3, frames: 100);
        d.Loop = false;
        var l = new float[200]; var r = new float[200];
        d.Render(l, r, 0, 200);
        Assert.True(d.Ended);
        Assert.False(d.Playing);
    }

    [Fact]
    public void Sync_MatchesTempo()
    {
        var mix = new LiveMixer();
        mix.A.Bpm = 120;
        mix.B.Bpm = 150;
        mix.Sync(mix.B);
        Assert.Equal(0.8, mix.B.Rate, 3);
    }

    [Fact]
    public void Filter_StaysFinite()
    {
        var d = Deck(0.5, frames: 2000);
        d.Filter = -1;
        var l = new float[500]; var r = new float[500];
        d.Render(l, r, 0, 500);
        Assert.All(l, v => Assert.True(float.IsFinite(v)));
        Assert.All(r, v => Assert.True(float.IsFinite(v)));
    }
}

public class PatchGraphTests
{
    [Fact]
    public void AutoPatch_WiresTracksBusesAndMaster()
    {
        var p = ProjectStore.CreateDefault();
        p.Tracks[0].Bus = 1;
        p.Tracks[1].Bus = 1;
        p.Tracks[2].Bus = 0;

        PatchGraphControl.AutoPatch(p);

        Assert.True(p.Graph.Initialized);
        Assert.Contains(p.Graph.Nodes, n => n.Kind == "out");
        Assert.Contains(p.Graph.Nodes, n => n.Kind == "track");
        Assert.Contains(p.Graph.Nodes, n => n.Kind == "fx" && n.Type == "busfx");
        Assert.Contains(p.Graph.Nodes, n => n.Kind == "fx" && n.Type == "trackfx");
        Assert.NotEmpty(p.Graph.Links);
        Assert.True(PatchRender.Active(p));
    }
}

public class LiveCueMixTests
{
    [Fact]
    public void CueMix_BlendsCuedDeckWithMaster()
    {
        var mix = new LiveMixer { Crossfade = 0, MasterGain = 1, CueLevel = 1, CueMonitorMix = 0 };
        mix.A.SetBuffer("a", "", Enumerable.Repeat(0.4f, 1000).ToArray(), Enumerable.Repeat(0.4f, 1000).ToArray(), 120, "", false);
        mix.B.SetBuffer("b", "", Enumerable.Repeat(0.2f, 1000).ToArray(), Enumerable.Repeat(0.2f, 1000).ToArray(), 120, "", false);
        mix.A.Playing = mix.B.Playing = true;
        mix.B.Cue = true;

        var ml = new float[100]; var mr = new float[100]; var cl = new float[100]; var cr = new float[100];
        mix.Fill(ml, mr, cl, cr, 100);
        Assert.Equal(0.2, cl[0], 3); // cue only: the cued deck

        mix.CueMonitorMix = 1;
        mix.Fill(ml, mr, cl, cr, 100);
        Assert.Equal(0.4, cl[0], 3); // full master blend: deck A, which is live
    }
}
