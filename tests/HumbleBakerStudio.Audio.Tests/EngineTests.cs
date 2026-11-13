using System.Text.Json.Nodes;
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

public class PatchGraphSyncTests
{
    [Fact]
    public void AddedTrackGetsAGraphNode_AndPruneRemovesOrphans()
    {
        var p = ProjectStore.CreateDefault();
        PatchGraphControl.AutoPatch(p);
        var t = ProjectStore.NewTrack("Lead", "sine");
        p.Tracks.Add(t);

        PatchGraphControl.AddTrackNode(p, t);
        Assert.Contains(p.Graph.Nodes, n => n.Kind == "track" && n.TrackUid == t.Uid);
        Assert.Contains(p.Graph.Nodes, n => n.Kind == "fx" && n.Label == "Lead FX");

        p.Tracks.Remove(t);
        int removed = PatchGraphControl.PruneTrackNodes(p);
        Assert.True(removed > 0);
        Assert.DoesNotContain(p.Graph.Nodes, n => n.Kind == "track" && n.TrackUid == t.Uid);
    }
}

public class NotePreviewTests
{
    [Fact]
    public void OneNotePreviewRendersAudio()
    {
        var p = ProjectStore.CreateDefault();
        var source = p.Tracks[2];
        var preview = new Project
        {
            Bpm = p.Bpm,
            BeatsPerBar = p.BeatsPerBar,
            BeatUnit = p.BeatUnit,
            Bars = 1,
            Swing = 0
        };
        var copy = new Track();
        foreach (var f in typeof(Track).GetFields())
            if (f.Name != "Notes" && f.Name != "Auto") f.SetValue(copy, f.GetValue(source));
        copy.Bus = 0;
        copy.Mute = false;
        copy.Solo = false;
        copy.Notes = new List<Note> { new(66, 0, 0.5, 100) };
        preview.Tracks.Add(copy);
        preview.Master.Enabled = false;

        var pcm = Dsp.Render(preview, loop: true, master: false);
        Assert.Contains(pcm, s => s != 0);
        Assert.True(pcm.Length > 1000);
    }
}

public class InstrumentLibraryTests
{
    [Fact]
    public void PatchInstrumentJsonAppliesToATrack()
    {
        string dir = Path.Combine(Path.GetTempPath(), "hbs-instruments-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string file = Path.Combine(dir, "test-voice.json");
            File.WriteAllText(file, """
            {"version":1,"name":"Test Voice","kind":"patch","role":"lead","octave":4,
             "patch":{"Cutoff":1234,"Attack":0.02,"Resonance":0.44},
             "fx":{"Drive":0.15,"ReverbMix":0.2}}
            """);

            var defs = InstrumentLibrary.LoadAll(new[] { dir });
            var def = Assert.Single(defs);
            Assert.Equal("Test Voice", def.Name);
            Assert.Equal("Patches", def.Category);

            var track = new Track();
            Assert.True(InstrumentLibrary.Apply(def, track, Array.Empty<string>(), out string error), error);
            Assert.Equal("patch", track.Synth);
            Assert.Equal(1234, track.Patch!.Cutoff, 3);
            Assert.Equal(0.02, track.Patch.Attack, 3);
            Assert.Equal(0.15, track.Fx.Drive, 3);
            Assert.Equal("Test Voice", track.InstrumentName);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void SavedSampleInstrumentLoadsAndRenders()
    {
        string library = @"Z:\LocalAI\Audio\studio\instruments";
        string samples = @"Z:\LocalAI\App";
        if (!Directory.Exists(library) || !Directory.Exists(samples)) return;

        var defs = InstrumentLibrary.LoadAll(new[] { library });
        var def = defs.FirstOrDefault(d => d.Kind == "sample" && d.SampleFile.Contains("FoleyPercussion", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(def);

        var track = new Track();
        Assert.True(InstrumentLibrary.Apply(def!, track, new[] { samples }, out string error), error);
        Assert.Equal("sample", track.Synth);
        Assert.NotNull(track.Sample);
        Assert.True(track.Sample!.Length > 200);

        var preview = new Project { Bpm = 120, BeatsPerBar = 4, BeatUnit = 4, Bars = 1, PhraseBars = 1, Swing = 0 };
        track.Notes.Add(new Note(Math.Clamp(def!.Root, 0, 127), 0, 2, 110));
        preview.Tracks.Add(track);
        var pcm = Dsp.Render(preview, loop: true, master: false);
        Assert.Contains(pcm, s => s != 0);
    }
}

public class PluginStatePersistenceTests
{
    [Fact]
    public void TrackAndGraphNodePluginFieldsRoundTrip()
    {
        var p = ProjectStore.CreateDefault();
        var t = p.Tracks[0];
        t.PluginFormat = "vst3";
        t.PluginPath = @"C:\VST3\Helm.vst3";
        t.PluginClass = "abc123";
        t.PluginName = "Helm";
        t.PluginInstrument = true;
        t.PluginState = "c3RhdGU=";
        t.PluginBypass = false;

        p.Graph.Initialized = true;
        var node = new PatchNode
        {
            Kind = "fx",
            Type = "vst",
            PluginFormat = "vst3",
            PluginPath = @"C:\VST3\kHs Distortion.vst3",
            PluginClass = "def456",
            PluginName = "kHs Distortion",
            PluginState = "bm9kZQ=="
        };
        p.Graph.Nodes.Add(node);

        var loaded = ProjectStore.LoadFromJson(ProjectStore.SaveToJson(p));
        var lt = loaded.Tracks[0];
        Assert.Equal("vst3", lt.PluginFormat);
        Assert.Equal("Helm", lt.PluginName);
        Assert.Equal("abc123", lt.PluginClass);
        Assert.True(lt.PluginInstrument);
        Assert.Equal("c3RhdGU=", lt.PluginState);

        var ln = loaded.Graph.Nodes.Single(n => n.Type == "vst");
        Assert.Equal("kHs Distortion", ln.PluginName);
        Assert.Equal("def456", ln.PluginClass);
        Assert.True(ln.HasPlugin);
        Assert.Equal("bm9kZQ==", ln.PluginState);
    }

    [Fact]
    public void PluginTrackIsSilentWithoutAHostAndFallsBackToSynth()
    {
        var p = ProjectStore.CreateDefault();
        p.Tracks.Clear();
        var t = ProjectStore.NewTrack("VST track", "saw");
        t.PluginFormat = "vst3";
        t.PluginPath = @"C:\missing\plugin.vst3";
        t.PluginClass = "x";
        t.PluginName = "Missing";
        t.PluginInstrument = true;
        t.Notes.Add(new Note(60, 0, 1, 100));
        p.Tracks.Add(t);
        p.Bars = 1; p.PhraseBars = 1;

        AudioPlugins.Host = null;
        var pcm = Dsp.Render(p, loop: true, master: false);
        Assert.Contains(pcm, s => s != 0);
    }
}

public class ArrangementTests
{
    private static Project SongProject()
    {
        var p = ProjectStore.CreateDefault();
        p.Bars = 4;
        p.Song.Clear();
        p.Markers.Clear();
        p.Tracks[1].Notes.Clear();
        p.Tracks[1].Notes.Add(new Note(36, 0, 1, 80));
        var pat = Pattern.Capture(p, "Verse");
        p.Patterns.Clear();
        p.Patterns.Add(pat);
        p.Song.Add(new SongItem(0, 1));
        return p;
    }

    [Fact]
    public void SongItemBarsOverride_ChangesTheLaidOutLength()
    {
        var p = SongProject();
        Assert.Equal(16, Arranger.SongBeats(p)); // 4 bars x 4 beats

        p.Song[0].Bars = 2;
        Assert.Equal(8, Arranger.SongBeats(p));
        var flat = Arranger.Flatten(p);
        Assert.Equal(2, flat.Bars);
        Assert.Equal(36, flat.Tracks[1].Notes[0].Pitch);
    }

    [Fact]
    public void MarkerEnergy_ScalesSongVelocitiesWhileNeutralFiveDoesNot()
    {
        var p = SongProject();
        var flat = Arranger.Flatten(p);
        Assert.Equal(80, flat.Tracks[1].Notes[0].Vel); // no markers: untouched

        p.Markers.Clear();
        p.Markers.Add(new Marker(0, "Intro", 10)); // 0.6 + 0.08*10 = 1.4
        p.Markers.Add(new Marker(8, "Drop", 5));   // neutral from beat 8 on
        flat = Arranger.Flatten(p);
        Assert.Equal(112, flat.Tracks[1].Notes[0].Vel);

        // The same note, laid out as a 2-bar repeat starting at beat 8, sits in the neutral section.
        p.Song[0].Bars = 2;
        p.Song.Add(new SongItem(0, 1, 2));
        flat = Arranger.Flatten(p);
        Assert.Equal(80, flat.Tracks[1].Notes.Last().Vel);
        Assert.Equal(8, flat.Tracks[1].Notes.Last().Start);
    }

    [Fact]
    public void SongItemBarsOverride_RoundTripsThroughTheProjectFile()
    {
        var p = SongProject();
        p.Song[0].Bars = 8;
        var loaded = ProjectStore.LoadFromJson(ProjectStore.SaveToJson(p));
        Assert.Equal(8, loaded.Song[0].Bars);
        loaded.Song[0].Bars = 0;
        loaded = ProjectStore.LoadFromJson(ProjectStore.SaveToJson(loaded));
        Assert.Equal(0, loaded.Song[0].Bars);
    }

    [Fact]
    public void SpectralGridData_EncodesOneBytePerCellAndRoundTrips()
    {
        var grid = new SpectralGrid(16, 8);
        for (int i = 0; i < grid.A.Length; i++) grid.A[i] = i / (float)(grid.A.Length - 1);
        var data = InstrumentLibrary.GridData(grid);

        Assert.Equal(16, data["cols"]!.GetValue<int>());
        Assert.Equal(8, data["rows"]!.GetValue<int>());
        byte[] bytes = Convert.FromBase64String(data["data"]!.GetValue<string>());
        Assert.Equal(grid.A.Length, bytes.Length);
        Assert.Equal(0, bytes[0]);
        Assert.Equal(255, bytes[^1]);
    }

    [Fact]
    public void AutomationPaint_MapsColumnHeightAndBrightnessToLaneValues()
    {
        var grid = new SpectralGrid(16, 8);
        for (int c = 0; c < grid.Cols; c++) grid.Dab(c, 4, 1, 1, false);

        var centred = AutomationPaint.Paint(grid, "vol", 0, 2, 16, "Exact shape", "Off (position only)", 1, 1);
        Assert.Equal(16, centred.Count);
        Assert.All(centred, p => Assert.InRange(p[1], 1.0, 1.3));
        Assert.All(centred, p => Assert.Equal(5, p.Length));
        Assert.All(centred, p => Assert.True(p[3] <= p[4]));
        Assert.Equal(centred.OrderBy(p => p[0]).Select(p => p[0]), centred.Select(p => p[0]));

        // Direct Z ignores the vertical position and uses the column's brightness.
        grid.Clear();
        for (int c = 0; c < grid.Cols; c++) grid.Dab(c, 0, 1, 1, false);
        var direct = AutomationPaint.Paint(grid, "vol", 0, 2, 16, "Exact shape", "Direct value", 1, 1);
        Assert.All(direct, p => Assert.Equal(2.0, p[1], 6));

        // Depth scales toward the neutral value (0 here), so a half-bright column lands halfway.
        grid.Clear();
        for (int c = 0; c < grid.Cols; c++) grid.Dab(c, 7, 1, 0.5, false);
        var depth = AutomationPaint.Paint(grid, "vol", 0, 2, 16, "Exact shape", "Depth (scale value)", 1, 0);
        Assert.All(depth, p => Assert.InRange(p[1], 0.95, 1.05));
    }

    [Fact]
    public void AutomationPaint_QuantizesToTheRequestedStepGrid()
    {
        var grid = new SpectralGrid(16, 8);
        for (int c = 0; c < grid.Cols; c++) grid.Dab(c, 3, 1, 1, false);

        var points = AutomationPaint.Paint(grid, "vol", 0, 2, 16, "Quantize steps", "Off (position only)", 1, 1);
        Assert.NotEmpty(points);
        foreach (var point in points)
        {
            double steps = point[1] / 2.0 * 16;
            Assert.Equal(Math.Round(steps), steps, 6);
        }
    }

    [Fact]
    public void AutomationPaint_StrokeDetailKeepsThePaintedVerticalExtent()
    {
        var grid = new SpectralGrid(4, 16);
        for (int c = 0; c < grid.Cols; c++)
            for (int r = 4; r <= 12; r++)
                grid.A[c * grid.Rows + r] = 1;

        var curve = AutomationPaint.Paint(grid, "vol", 0, 2, 16, "Exact shape", "Off (position only)", 1, 1);
        var stroke = AutomationPaint.Paint(grid, "vol", 0, 2, 16, "Exact shape", "Off (position only)", 1, 1, "Stroke (follow rows)");

        Assert.Equal(grid.Cols, curve.Count);
        Assert.True(stroke.Count > curve.Count);
        Assert.True(stroke.Max(p => p[1]) > curve.Max(p => p[1]));
        Assert.True(stroke.Min(p => p[1]) < curve.Min(p => p[1]));
    }

    [Fact]
    public void AutoLane_DenseLaneLookupIsExactAndFast()
    {
        var lane = new AutoLane(0, "vol");
        for (int i = 0; i < 20000; i++) lane.Points.Add(new[] { i * 0.01, (double)(i % 100) });

        Assert.Equal(1.0, lane.At(-1), 9);              // before the first point: neutral
        Assert.Equal(0.5, lane.At(50.005), 9);           // halfway between two dense points
        Assert.Equal(99.0, lane.At(1_000_000), 9);       // after the last point: held
    }

    [Fact]
    public void SpectralGrid_ResampleKeepsShapeAtHigherResolution()
    {
        var grid = new SpectralGrid(512, 128);
        grid.A[100 * grid.Rows + 40] = 1;
        grid.A[200 * grid.Rows + 80] = 0.5f;

        var high = grid.Resample(2048, 512);

        Assert.Equal(2048, high.Cols);
        Assert.Equal(512, high.Rows);
        Assert.True(high.Peak() > 0.9);
        Assert.True(high.Peak() <= 1.0);
    }

    [Fact]
    public void AutomationLayers_CrossingStrokeBuffersStaySeparate()
    {
        var a = new float[64 * 32];
        var b = new float[64 * 32];
        for (int c = 0; c < 64; c++)
        {
            a[c * 32 + 16] = 1;   // both strokes cross the same pixels
            b[c * 32 + 16] = 1;
        }

        var first = AutomationLayers.FromData(a, 64, 32);
        var second = AutomationLayers.FromData(b, 64, 32);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.NotSame(first!.Data, second!.Data); // independent stroke buffers

        var clone = first.Clone();
        Assert.NotSame(first.Data, clone.Data);
        Assert.Equal(first.Amplitude, clone.Amplitude, 9);
    }

    [Fact]
    public void AutomationLayers_ExtractSeparatePaintedWaves()
    {
        var grid = new SpectralGrid(64, 32);
        for (int c = 0; c < grid.Cols; c++) grid.Dab(c, 8, 1, 1, false);
        for (int c = 0; c < grid.Cols; c++) grid.Dab(c, 24, 1, 1, false);

        var layers = AutomationLayers.Extract(grid, 0.02, 8);

        Assert.Equal(2, layers.Count);
        Assert.True(layers[0].CentroidY > layers[1].CentroidY); // top wave first
        Assert.All(layers, l => Assert.True(l.Amplitude > 0.5));
        Assert.All(layers, l => Assert.True(l.Contour.Count > 10));
    }

    [Fact]
    public void AutomationLayers_NormalizeByAmplitudeAndMix()
    {
        var grid = new SpectralGrid(64, 32);
        for (int c = 0; c < grid.Cols; c++) grid.Dab(c, 8, 1, 1, false);
        for (int c = 0; c < grid.Cols; c++) grid.Dab(c, 24, 1, 1, false);
        var layers = AutomationLayers.Extract(grid, 0.02, 8);

        var add = AutomationLayers.Combine(layers, "vol", 0, 2, 16, "Exact shape", 1, "Curve (single value)", true, 1, "Add");
        Assert.NotEmpty(add);
        Assert.All(add, p => Assert.InRange(p[1], 1.0, 1.15));

        var max = AutomationLayers.Combine(layers, "vol", 0, 2, 16, "Exact shape", 1, "Curve (single value)", true, 1, "Max");
        Assert.All(max, p => Assert.InRange(p[1], 1.4, 1.7));

        // A quieter layer is boosted by normalisation and not by the raw amplitude weighting.
        layers[1].Amplitude = 0.5;
        var normalised = AutomationLayers.Combine(layers, "vol", 0, 2, 16, "Exact shape", 1, "Curve (single value)", true, 1, "Add");
        var raw = AutomationLayers.Combine(layers, "vol", 0, 2, 16, "Exact shape", 1, "Curve (single value)", false, 1, "Add");
        Assert.True(normalised[0][1] < raw[0][1]);
    }

    [Fact]
    public void PatternComposer_IsDeterministicForASeed()
    {
        var request = new PatternRequest
        {
            Role = "Lead",
            Style = "Any",
            Mood = "Any",
            Bars = 4,
            Energy = 0.6,
            Complexity = 0.5,
            Density = 0.6,
            Seed = 42,
            Key = 9,
            Scale = "minor",
            BarBeats = 4
        };

        var a = PatternComposer.Compose(request);
        var b = PatternComposer.Compose(request);

        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Notes.Count, b.Notes.Count);
        for (int i = 0; i < a.Notes.Count; i++)
        {
            Assert.Equal(a.Notes[i].Pitch, b.Notes[i].Pitch);
            Assert.Equal(a.Notes[i].Start, b.Notes[i].Start, 9);
            Assert.Equal(a.Notes[i].Len, b.Notes[i].Len, 9);
            Assert.Equal(a.Notes[i].Vel, b.Notes[i].Vel);
        }
    }

    [Fact]
    public void PatternComposer_DrumExamplesUseDrumPitches()
    {
        var result = PatternComposer.Compose(new PatternRequest
        {
            Role = "Drums",
            Bars = 2,
            Seed = 7,
            BarBeats = 4
        });

        Assert.NotEmpty(result.Notes);
        Assert.Contains(result.Notes, n => n.Pitch == 36); // kick
        Assert.Contains(result.Notes, n => n.Pitch == 38); // snare
        Assert.All(result.Notes, n => Assert.InRange(n.Pitch, 35, 81));
    }

    [Fact]
    public void PatternComposer_FitsTheRequestedBars()
    {
        var result = PatternComposer.Compose(new PatternRequest
        {
            Role = "Chords",
            Bars = 2,
            Seed = 3,
            BarBeats = 4
        });

        Assert.NotEmpty(result.Notes);
        Assert.All(result.Notes, n => Assert.InRange(n.Start, 0, 8));
    }

    [Fact]
    public void AutomationPreview_RoundTripsPaintingAtFullResolution()
    {
        var grid = new SpectralGrid(256, 64);
        for (int c = 0; c < grid.Cols; c++)
            for (int r = 16; r < 48; r++)
                grid.A[c * grid.Rows + r] = (float)(0.2 + 0.8 * (r - 16) / 32.0);

        var compressed = AutomationPreview.Encode(grid);
        var restored = AutomationPreview.Decode(compressed, 256, 64);

        Assert.Equal(256, restored.Cols);
        Assert.Equal(64, restored.Rows);
        for (int c = 0; c < grid.Cols; c += 17)
            for (int r = 0; r < grid.Rows; r += 7)
            {
                int i = c * grid.Rows + r;
                Assert.InRange(restored.A[i], grid.A[i] - 1 / 255f, grid.A[i] + 1 / 255f);
            }
        Assert.True(compressed.Length < grid.A.Length); // filled regions compress well
    }

    [Fact]
    public void AutomationLane_PreviewRoundTripsThroughProjectStore()
    {
        var p = ProjectStore.CreateDefault();
        var lane = new AutoLane(0, "f:Cutoff");
        lane.Points.Add(new[] { 0.0, 800.0, 1.0, 0.2, 0.8 });
        var grid = new SpectralGrid(128, 32);
        for (int i = 0; i < grid.A.Length; i++) grid.A[i] = 0.5f;
        lane.Preview = AutomationPreview.Encode(grid);
        lane.PreviewCols = 128;
        lane.PreviewRows = 32;
        p.Tracks[0].Auto.Add(lane);

        var loaded = ProjectStore.LoadFromJson(ProjectStore.SaveToJson(p));
        var lane2 = loaded.Tracks[0].Auto.Single(l => l.Param == "f:Cutoff");

        Assert.NotNull(lane2.Preview);
        Assert.Equal(128, lane2.PreviewCols);
        Assert.Equal(32, lane2.PreviewRows);
        var restored = AutomationPreview.Decode(lane2.Preview, lane2.PreviewCols, lane2.PreviewRows);
        Assert.Equal(128, restored.Cols);
        Assert.InRange(restored.A[1000], 0.49f, 0.51f);
    }

    [Fact]
    public void AutomationLane_RoundTripsPaintedShapeDetail()
    {
        var p = ProjectStore.CreateDefault();
        var lane = new AutoLane(0, "p:Cutoff");
        lane.Points.Add(new[] { 1.0, 800.0, 0.9, 0.2, 0.8 });
        lane.Points.Add(new[] { 2.0, 2400.0, 0.6, 0.4, 0.9 });
        p.Tracks[0].Auto.Add(lane);

        var loaded = ProjectStore.LoadFromJson(ProjectStore.SaveToJson(p));
        var points = loaded.Tracks[0].Auto.Single(l => l.Param == "p:Cutoff").Points;

        Assert.Equal(2, points.Count);
        Assert.Equal(5, points[0].Length);
        Assert.Equal(800.0, points[0][1], 6);
        Assert.Equal(0.9, points[0][2], 6);
        Assert.Equal(0.2, points[0][3], 6);
        Assert.Equal(0.8, points[0][4], 6);
    }

    [Fact]
    public void MorphInstrument_LoadsFourSlotsAndAppliesTheBlend()
    {
        var json = new JsonObject
        {
            ["version"] = 1,
            ["name"] = "test morph",
            ["kind"] = "morph",
            ["role"] = "pad",
            ["octave"] = 3,
            ["slots"] = new JsonArray(
                new JsonObject { ["Cutoff"] = 200, ["Sustain"] = 0.1, ["Osc1"] = "sine" },
                new JsonObject { ["Cutoff"] = 2000, ["Sustain"] = 0.9, ["Osc1"] = "saw" },
                new JsonObject { ["Cutoff"] = 400, ["Sustain"] = 0.3, ["Osc1"] = "tri" },
                new JsonObject { ["Cutoff"] = 6000, ["Sustain"] = 1.0, ["Osc1"] = "noise" }),
            ["meta"] = new JsonObject { ["X"] = 0.75, ["Y"] = 0.25 }
        };
        string path = Path.Combine(Path.GetTempPath(), "hbs-morph-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json.ToJsonString());
        try
        {
            var def = InstrumentLibrary.LoadFile(path);
            Assert.NotNull(def);
            Assert.Equal("morph", def!.Kind);
            Assert.Equal(4, def.Slots!.Count);

            var track = new Track();
            Assert.True(InstrumentLibrary.Apply(def, track, Array.Empty<string>(), out string error), error);
            Assert.Equal("patch", track.Synth);
            Assert.NotNull(track.Patch);
            Assert.InRange(track.Patch!.Cutoff, 200, 6000);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
