using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

/// <summary>
/// Sample import, waveform selection, trimming, slicing and audio-to-notes transcription.
/// This is also where a generated song file is brought into the studio.
/// </summary>
internal sealed class SampleTab : UserControl
{
    private readonly MainForm _host;
    private readonly WaveView _wave = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 24, Padding = new Padding(10, 4, 4, 0), ForeColor = Theme.Muted, Font = Theme.Small, BackColor = Theme.Panel };
    private readonly SimpleList _slices = new() { Dock = DockStyle.Fill };
    private readonly Label _heading = new() { Dock = DockStyle.Top, Height = 30, Padding = new Padding(12, 7, 0, 0), ForeColor = Theme.Text, Font = Theme.UiBold, BackColor = Theme.Panel };
    private readonly NumericUpDown _sensitivity = new() { Minimum = 0.05m, Maximum = 0.95m, DecimalPlaces = 2, Increment = 0.05m, Value = 0.5m, Width = 60 };
    private readonly NumericUpDown _chopCount = new() { Minimum = 2, Maximum = 64, Value = 8, Width = 56 };

    private float[]? _buffer;
    private string _fileName = "";
    private int[] _markers = Array.Empty<int>();
    private readonly List<(int Start, int End)> _sliceRanges = new();

    public SampleTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar1 = Row();
        bar1.Controls.Add(Theme.Button("Open WAV…", (_, _) => OpenWav(), true));
        bar1.Controls.Add(Theme.Button("Play", (_, _) => PlayWhole()));
        bar1.Controls.Add(Theme.Button("Save selection…", (_, _) => SaveSelection()));
        bar1.Controls.Add(Theme.Button("Trim to selection", (_, _) => TrimToSelection()));
        bar1.Controls.Add(Theme.Button("Normalize", (_, _) => Normalize()));
        bar1.Controls.Add(Theme.Button("Reverse", (_, _) => Reverse()));
        bar1.Controls.Add(Theme.Button("Create track", (_, _) => CreateTrackFromSelection()));

        var bar2 = Row();
        bar2.Height = 104;
        bar2.WrapContents = true;
        bar2.AutoScroll = false;
        bar2.Controls.Add(Theme.Label("Onset sensitivity", Theme.Muted));
        bar2.Controls.Add(_sensitivity);
        bar2.Controls.Add(Theme.Button("Auto-slice", (_, _) => AutoSlice()));
        bar2.Controls.Add(Theme.Label("Chops", Theme.Muted));
        bar2.Controls.Add(_chopCount);
        bar2.Controls.Add(Theme.Button("Equal chop", (_, _) => EqualChop()));
        bar2.Controls.Add(Theme.Button("Add selection as slice", (_, _) => AddSelectionSlice()));
        bar2.Controls.Add(Theme.Button("Clear slices", (_, _) => ClearSlices()));
        bar2.Controls.Add(Separator());
        bar2.Controls.Add(Theme.Button("Transcribe melody", (_, _) => TranscribeMelody()));
        bar2.Controls.Add(Theme.Button("Transcribe drums", (_, _) => TranscribeDrums()));
        bar2.Controls.Add(Theme.Button("Detect sections", (_, _) => DetectSections()));
        bar2.Controls.Add(Theme.Button("Import song & transcribe", (_, _) => ImportSong(), true));

        var right = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Theme.Panel, Padding = new Padding(8) };
        var sliceButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 96, WrapContents = true, BackColor = Theme.Panel };
        sliceButtons.Controls.Add(Theme.Button("Audition", (_, _) => AuditionSlice()));
        sliceButtons.Controls.Add(Theme.Button("Use as track", (_, _) => SliceToTrack(single: true)));
        sliceButtons.Controls.Add(Theme.Button("All as tracks", (_, _) => SliceToTrack(single: false)));
        sliceButtons.Controls.Add(Theme.Button("Export slices…", (_, _) => ExportSlices()));
        right.Controls.Add(_slices);
        right.Controls.Add(sliceButtons);

        Controls.Add(_wave);
        Controls.Add(right);
        Controls.Add(bar2);
        Controls.Add(bar1);
        Controls.Add(_heading);
        Controls.Add(_status);

        _wave.SelectionChanged += (_, _) => UpdateStatus();
        _slices.DoubleClick += (_, _) => AuditionSlice();
    }

    private static FlowLayoutPanel Row() => new()
    {
        Dock = DockStyle.Top,
        Height = 38,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(8, 5, 4, 3),
        BackColor = Theme.Panel
    };

    private static Control Separator() => new Panel { Width = 1, Height = 24, BackColor = Theme.Border, Margin = new Padding(8, 3, 8, 3) };

    public void RefreshView()
    {
        _heading.Text = "Sample lab" + (string.IsNullOrEmpty(_fileName) ? "" : " — " + _fileName);
        UpdateStatus();
    }

    private void OpenWav()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Audio (*.wav;*.aif;*.aiff)|*.wav;*.aif;*.aiff|All files (*.*)|*.*",
            Title = "Open a sample or song"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        LoadFile(dlg.FileName, announce: true);
    }

    private bool LoadFile(string path, bool announce)
    {
        var data = WavIn.ReadMono(path, out _);
        if (data == null || data.Length < 64)
        {
            MessageBox.Show(this, "That file could not be read as audio. Use a PCM WAV (16/24/32-bit or float).", "Sample lab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        _buffer = data;
        _fileName = Path.GetFileName(path);
        _markers = Array.Empty<int>();
        _sliceRanges.Clear();
        _slices.Clear();
        _wave.Samples = data;
        _wave.SelStart = 0;
        _wave.SelEnd = data.Length;
        _wave.Markers = _markers;
        _wave.Invalidate();
        RefreshView();
        if (announce) _host.SetStatus($"Loaded {_fileName} — {data.Length / (double)Dsp.SR:0.00} s");
        return true;
    }

    private void UpdateStatus()
    {
        if (_buffer == null)
        {
            _status.Text = "Open a WAV to edit, slice, or transcribe it. Everything stays local.";
            return;
        }
        double seconds = _buffer.Length / (double)Dsp.SR;
        double a = Math.Min(_wave.SelStart, _wave.SelEnd) / (double)Dsp.SR;
        double b = Math.Max(_wave.SelStart, _wave.SelEnd) / (double)Dsp.SR;
        _status.Text = $"{_fileName} · {seconds:0.000} s · {Dsp.SR} Hz mono · selection {a:0.000}–{b:0.000} s · {_sliceRanges.Count} slice(s)";
    }

    private (float[] Data, int Start, int End)? SelectionOrWhole()
    {
        if (_buffer == null) return null;
        int a = Math.Min(_wave.SelStart, _wave.SelEnd);
        int b = Math.Max(_wave.SelStart, _wave.SelEnd);
        if (b - a < 64) { a = 0; b = _buffer.Length; }
        var copy = new float[b - a];
        Array.Copy(_buffer, a, copy, 0, b - a);
        return (copy, a, b);
    }

    private void PlayWhole()
    {
        if (_buffer == null) return;
        var sel = SelectionOrWhole();
        if (sel == null) return;
        PlayMono(sel.Value.Data);
    }

    private void PlayMono(float[] mono)
    {
        if (mono.Length < 2) return;
        var pcm = new short[mono.Length * 2];
        for (int i = 0; i < mono.Length; i++)
        {
            double v = Math.Clamp(mono[i] * 0.95, -1, 1) * 30000;
            var s = (short)v;
            pcm[2 * i] = s;
            pcm[2 * i + 1] = s;
        }
        _host.Player.Play(pcm, Dsp.SR);
    }

    private void SaveSelection()
    {
        var sel = SelectionOrWhole();
        if (sel == null) return;
        using var dlg = new SaveFileDialog
        {
            Filter = "WAV (*.wav)|*.wav",
            FileName = Path.GetFileNameWithoutExtension(_fileName) + "-edit.wav",
            InitialDirectory = _host.Settings.ResolvedOutputDirectory
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        Dsp.WriteWavMono(dlg.FileName, sel.Value.Data);
        _host.SetStatus("Saved " + Path.GetFileName(dlg.FileName));
    }

    private void TrimToSelection()
    {
        var sel = SelectionOrWhole();
        if (sel == null) return;
        _buffer = sel.Value.Data;
        _markers = Array.Empty<int>();
        _sliceRanges.Clear();
        _slices.Clear();
        _wave.Samples = _buffer;
        _wave.SelStart = 0;
        _wave.SelEnd = _buffer.Length;
        _wave.Markers = _markers;
        _wave.Invalidate();
        UpdateStatus();
    }

    private void Normalize()
    {
        if (_buffer == null) return;
        Dsp.Normalize(_buffer, 0.95f);
        _wave.Invalidate();
        UpdateStatus();
    }

    private void Reverse()
    {
        if (_buffer == null) return;
        Array.Reverse(_buffer);
        _wave.Invalidate();
        UpdateStatus();
    }

    private void CreateTrackFromSelection()
    {
        var sel = SelectionOrWhole();
        if (sel == null) return;
        if (sel.Value.Data.Length > Dsp.SR * 60)
        {
            if (MessageBox.Show(this, "That sample is longer than a minute. Add it anyway?", "Sample lab", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        }
        _host.BeginEdit("sample");
        var name = string.IsNullOrEmpty(_fileName) ? "Sample" : Path.GetFileNameWithoutExtension(_fileName);
        var t = ProjectStore.NewTrack(name, "sample");
        t.Sample = sel.Value.Data;
        t.SampleGate = true;
        t.SampleRoot = 60;
        t.SampleMode = 0;
        t.Notes.Add(new Note(60, 0, 2, 110));
        _host.Project.Tracks.Add(t);
        _host.NotifyTracksChanged(t);
        _host.SetStatus("Added sample track \"" + t.Name + "\"");
    }

    private void AutoSlice()
    {
        if (_buffer == null) return;
        int minGap = (int)(Dsp.SR * 0.03);
        _markers = SampleSlicer.Boundaries(_buffer, (double)_sensitivity.Value, minGap);
        BuildSlicesFromBoundaries(_markers);
    }

    private void EqualChop()
    {
        if (_buffer == null) return;
        int n = (int)_chopCount.Value;
        var bounds = new int[n + 1];
        for (int i = 0; i <= n; i++) bounds[i] = (int)((long)_buffer.Length * i / n);
        _markers = bounds.Take(n).ToArray();
        BuildSlicesFromBoundaries(bounds);
    }

    private void BuildSlicesFromBoundaries(int[] bounds)
    {
        _sliceRanges.Clear();
        _slices.Clear();
        if (_buffer != null)
        {
            for (int i = 0; i < bounds.Length; i++)
            {
                int start = bounds[i];
                int end = i + 1 < bounds.Length ? bounds[i + 1] : _buffer.Length;
                if (end - start < 32) continue;
                _sliceRanges.Add((start, end));
                _slices.Add($"Slice {_sliceRanges.Count:00} · {start / (double)Dsp.SR:0.000}–{end / (double)Dsp.SR:0.000} s");
            }
        }
        _wave.Markers = bounds;
        _wave.Invalidate();
        UpdateStatus();
    }

    private void AddSelectionSlice()
    {
        if (_buffer == null) return;
        int a = Math.Min(_wave.SelStart, _wave.SelEnd);
        int b = Math.Max(_wave.SelStart, _wave.SelEnd);
        if (b - a < 32) return;
        _sliceRanges.Add((a, b));
        _slices.Add($"Slice {_sliceRanges.Count:00} · {a / (double)Dsp.SR:0.000}–{b / (double)Dsp.SR:0.000} s");
        UpdateStatus();
    }

    private void ClearSlices()
    {
        _sliceRanges.Clear();
        _slices.Clear();
        _markers = Array.Empty<int>();
        _wave.Markers = _markers;
        _wave.Invalidate();
        UpdateStatus();
    }

    private void AuditionSlice()
    {
        if (_buffer == null || _slices.SelectedIndex < 0 || _slices.SelectedIndex >= _sliceRanges.Count) return;
        var (a, b) = _sliceRanges[_slices.SelectedIndex];
        var copy = new float[b - a];
        Array.Copy(_buffer, a, copy, 0, b - a);
        PlayMono(copy);
    }

    private void SliceToTrack(bool single)
    {
        if (_buffer == null) return;
        var ranges = single
            ? (_slices.SelectedIndex >= 0 && _slices.SelectedIndex < _sliceRanges.Count ? new List<(int, int)> { _sliceRanges[_slices.SelectedIndex] } : new())
            : _sliceRanges.Take(24).ToList();
        if (ranges.Count == 0) return;

        _host.BeginEdit("sample");
        var baseName = string.IsNullOrEmpty(_fileName) ? "Slice" : Path.GetFileNameWithoutExtension(_fileName);
        int added = 0;
        foreach (var (a, b) in ranges)
        {
            var data = new float[b - a];
            Array.Copy(_buffer, a, data, 0, b - a);
            var t = ProjectStore.NewTrack($"{baseName} {added + 1}", "sample");
            t.Sample = data;
            t.SampleGate = true;
            t.SampleRoot = 60;
            t.Notes.Add(new Note(60, 0, 2, 110));
            _host.Project.Tracks.Add(t);
            added++;
        }
        _host.NotifyTracksChanged(_host.Project.Tracks[^1]);
        _host.SetStatus($"Added {added} sample track(s)");
    }

    private void ExportSlices()
    {
        if (_buffer == null || _sliceRanges.Count == 0) return;
        using var dlg = new FolderBrowserDialog { Description = "Choose a folder for the slices" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var baseName = ExportService.SanitizeFileName(Path.GetFileNameWithoutExtension(_fileName));
        int n = 0;
        foreach (var (a, b) in _sliceRanges)
        {
            var data = new float[b - a];
            Array.Copy(_buffer, a, data, 0, b - a);
            Dsp.WriteWavMono(Path.Combine(dlg.SelectedPath, $"{baseName}-slice-{++n:00}.wav"), data);
        }
        _host.SetStatus($"Exported {n} slice(s)");
    }

    private void TranscribeMelody()
    {
        if (_buffer == null) return;
        try
        {
            _host.SetStatus("Transcribing melody…");
            var p = _host.Project;
            var notes = Transcriber.Monophonic(_buffer, p.Bpm, p.Key, p.Scale, 55, 2000);
            if (notes.Count == 0) { _host.SetStatus("No clear melodic content found"); return; }
            _host.BeginEdit("transcribe");
            var t = ProjectStore.NewTrack("Transcription", "patch");
            t.Notes.AddRange(notes);
            _host.Project.Tracks.Add(t);
            _host.NotifyTracksChanged(t);
            _host.SetStatus($"Transcribed {notes.Count} notes to \"" + t.Name + "\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Transcribe melody", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void TranscribeDrums()
    {
        if (_buffer == null) return;
        try
        {
            _host.SetStatus("Transcribing drums…");
            var notes = Transcriber.Drums(_buffer, _host.Project.Bpm);
            if (notes.Count == 0) { _host.SetStatus("No clear drum hits found"); return; }
            _host.BeginEdit("transcribe");
            var t = ProjectStore.NewTrack("Transcribed drums", "drums");
            t.Notes.AddRange(notes);
            _host.Project.Tracks.Add(t);
            _host.NotifyTracksChanged(t);
            _host.SetStatus($"Transcribed {notes.Count} drum hits");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Transcribe drums", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void DetectSections()
    {
        if (_buffer == null) return;
        try
        {
            _host.SetStatus("Detecting sections…");
            var p = _host.Project;
            var sections = Transcriber.Sections(_buffer, p.Bpm, p.BeatsPerBar, p.BeatUnit);
            if (sections.Count == 0) { _host.SetStatus("No sections detected"); return; }
            _host.BeginEdit("sections");
            p.Markers.Clear();
            foreach (var s in sections)
                p.Markers.Add(new Marker(s.Beat, string.IsNullOrWhiteSpace(s.Name) ? "Section" : s.Name, Math.Clamp(s.Energy, 1, 10)));
            _host.MarkDirty();
            _host.SetStatus($"Added {sections.Count} marker(s) from sections");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Detect sections", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>One action for bringing a generated or recorded song into the project.</summary>
    private void ImportSong()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Audio (*.wav)|*.wav|All files (*.*)|*.*",
            Title = "Import a song"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!LoadFile(dlg.FileName, announce: false)) return;

        _host.BeginEdit("import song");
        var p = _host.Project;
        var name = Path.GetFileNameWithoutExtension(dlg.FileName);

        // Keep the original audio as a sample track.
        var audioTrack = ProjectStore.NewTrack(name + " (audio)", "sample");
        audioTrack.Sample = (float[])_buffer!.Clone();
        audioTrack.SampleGate = true;
        audioTrack.SampleRoot = 60;
        audioTrack.Vol = 0.6;
        var beats = _buffer.Length / (double)Dsp.SR / (60.0 / p.Bpm);
        audioTrack.Notes.Add(new Note(60, 0, Math.Max(0.25, beats), 110));
        p.Tracks.Add(audioTrack);

        // A melodic sketch and a drum sketch, as separate editable tracks.
        var melody = Transcriber.Monophonic(_buffer, p.Bpm, p.Key, p.Scale, 55, 2000);
        if (melody.Count > 0)
        {
            var mt = ProjectStore.NewTrack(name + " melody", "patch");
            mt.Notes.AddRange(melody);
            p.Tracks.Add(mt);
        }
        var drums = Transcriber.Drums(_buffer, p.Bpm);
        if (drums.Count > 0)
        {
            var dt = ProjectStore.NewTrack(name + " drums", "drums");
            dt.Notes.AddRange(drums);
            p.Tracks.Add(dt);
        }

        var sections = Transcriber.Sections(_buffer, p.Bpm, p.BeatsPerBar, p.BeatUnit);
        if (sections.Count > 0)
        {
            p.Markers.Clear();
            foreach (var s in sections)
                p.Markers.Add(new Marker(s.Beat, string.IsNullOrWhiteSpace(s.Name) ? "Section" : s.Name, Math.Clamp(s.Energy, 1, 10)));
        }

        _host.NotifyTracksChanged(audioTrack);
        _host.SetStatus($"Imported {name}: audio + {melody.Count} melody notes + {drums.Count} drum hits + {sections.Count} sections");
    }
}
