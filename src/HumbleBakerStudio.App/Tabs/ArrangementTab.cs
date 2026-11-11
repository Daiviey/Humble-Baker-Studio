using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

/// <summary>Patterns, the song arrangement and timeline markers, plus MIDI and SuperCollider export.</summary>
internal sealed class ArrangementTab : UserControl
{
    private readonly MainForm _host;
    private readonly SimpleList _patterns = new() { Dock = DockStyle.Fill };
    private readonly SimpleList _song = new() { Dock = DockStyle.Fill };
    private readonly SimpleList _markers = new() { Dock = DockStyle.Fill };
    private readonly TextBox _patternName = new() { Width = 160 };
    private readonly NumericUpDown _patternBars = new() { Minimum = 1, Maximum = 64, Width = 56 };
    private readonly NumericUpDown _repeats = new() { Minimum = 1, Maximum = 128, Width = 56, Value = 4 };
    private readonly NumericUpDown _markerBeat = new() { Minimum = 0, Maximum = 9999, DecimalPlaces = 2, Increment = 4, Width = 70 };
    private readonly TextBox _markerName = new() { Width = 120, Text = "Section" };
    private readonly NumericUpDown _markerEnergy = new() { Minimum = 1, Maximum = 10, Width = 46, Value = 5 };
    private readonly Label _songInfo = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly CheckBox _exportSong = new() { Text = "Use song arrangement", AutoSize = true, Checked = true };

    public ArrangementTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, BackColor = Theme.Panel, Padding = new Padding(10, 7, 6, 4), WrapContents = false };
        top.Controls.Add(Theme.Button("Export MIDI…", (_, _) => ExportMidi(), true));
        top.Controls.Add(Theme.Button("Export SuperCollider…", (_, _) => ExportSuperCollider(), true));
        top.Controls.Add(_exportSong);
        top.Controls.Add(_songInfo);

        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.Back };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        columns.Controls.Add(BuildPatterns(), 0, 0);
        columns.Controls.Add(BuildSong(), 1, 0);
        columns.Controls.Add(BuildMarkers(), 2, 0);

        Controls.Add(columns);
        Controls.Add(top);
        Load += (_, _) => BeginInvoke(new Action(RefreshLists));
    }

    private static Panel Section()
    {
        return new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel, Padding = new Padding(8), Margin = new Padding(4) };
    }

    private static void AddTitle(Panel panel, string title)
    {
        // Added last so the docked Top title is laid out before the Fill list.
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 24, ForeColor = Theme.Accent, Font = Theme.UiBold });
    }

    private Control BuildPatterns()
    {
        var panel = Section();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, WrapContents = true, BackColor = Theme.Panel };
        buttons.Controls.Add(Theme.Button("New", (_, _) => NewPattern()));
        buttons.Controls.Add(Theme.Button("Capture current", (_, _) => CaptureIntoSelected()));
        buttons.Controls.Add(Theme.Button("Apply", (_, _) => ApplySelected()));
        buttons.Controls.Add(Theme.Button("Delete", (_, _) => DeletePattern()));

        var fields = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false, BackColor = Theme.Panel };
        fields.Controls.Add(Theme.Label("Name", Theme.Muted));
        fields.Controls.Add(_patternName);
        fields.Controls.Add(Theme.Label("Bars", Theme.Muted));
        fields.Controls.Add(_patternBars);

        panel.Controls.Add(buttons);
        panel.Controls.Add(_patterns);
        panel.Controls.Add(fields);
        AddTitle(panel, "PATTERNS");

        _patterns.SelectedIndexChanged += (_, _) =>
        {
            var p = SelectedPattern();
            if (p == null) return;
            _patternName.Text = p.Name;
            _patternBars.Value = Math.Clamp(p.Bars, 1, 64);
        };
        _patternName.TextChanged += (_, _) =>
        {
            var p = SelectedPattern();
            if (p == null || p.Name == _patternName.Text) return;
            _host.BeginEdit("pattern");
            p.Name = _patternName.Text.Trim();
            _host.MarkDirty();
            RefreshLists();
        };
        _patternBars.ValueChanged += (_, _) =>
        {
            var p = SelectedPattern();
            if (p == null) return;
            _host.BeginEdit("pattern");
            p.Bars = (int)_patternBars.Value;
            _host.MarkDirty();
            RefreshLists();
        };
        return panel;
    }

    private Control BuildSong()
    {
        var panel = Section();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, WrapContents = true, BackColor = Theme.Panel };
        buttons.Controls.Add(Theme.Button("Add pattern", (_, _) => AddSongItem()));
        buttons.Controls.Add(Theme.Button("Remove", (_, _) => RemoveSongItem()));
        buttons.Controls.Add(Theme.Button("Up", (_, _) => MoveSongItem(-1)));
        buttons.Controls.Add(Theme.Button("Down", (_, _) => MoveSongItem(1)));
        var fields = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false, BackColor = Theme.Panel };
        fields.Controls.Add(Theme.Label("Repeats", Theme.Muted));
        fields.Controls.Add(_repeats);
        fields.Controls.Add(Theme.Button("Set", (_, _) => SetRepeats()));

        panel.Controls.Add(buttons);
        panel.Controls.Add(_song);
        panel.Controls.Add(fields);
        AddTitle(panel, "SONG ORDER");
        return panel;
    }

    private Control BuildMarkers()
    {
        var panel = Section();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, WrapContents = true, BackColor = Theme.Panel };
        buttons.Controls.Add(Theme.Button("Add", (_, _) => AddMarker()));
        buttons.Controls.Add(Theme.Button("At playhead", (_, _) => AddMarkerAtPlayhead()));
        buttons.Controls.Add(Theme.Button("Remove", (_, _) => RemoveMarker()));
        var fields = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 66, WrapContents = true, BackColor = Theme.Panel };
        fields.Controls.Add(Theme.Label("Beat", Theme.Muted));
        fields.Controls.Add(_markerBeat);
        fields.Controls.Add(Theme.Label("Name", Theme.Muted));
        fields.Controls.Add(_markerName);
        fields.Controls.Add(Theme.Label("Energy", Theme.Muted));
        fields.Controls.Add(_markerEnergy);

        panel.Controls.Add(buttons);
        panel.Controls.Add(_markers);
        panel.Controls.Add(fields);
        AddTitle(panel, "MARKERS");
        _markers.SelectedIndexChanged += (_, _) =>
        {
            var m = SelectedMarker();
            if (m == null) return;
            _markerBeat.Value = (decimal)Math.Clamp(m.Beat, 0, 9999);
            _markerName.Text = m.Name;
            _markerEnergy.Value = Math.Clamp(m.Energy, 1, 10);
        };
        return panel;
    }

    public void RefreshView() => RefreshLists();

    private void RefreshLists()
    {
        var p = _host.Project;
        int pi = _patterns.SelectedIndex;
        _patterns.BeginUpdate();
        _patterns.Clear();
        for (int i = 0; i < p.Patterns.Count; i++)
        {
            var pat = p.Patterns[i];
            _patterns.Add($"{i + 1}. {pat.Name} · {pat.Bars} bars · {pat.NoteCount} notes");
        }
        _patterns.EndUpdate();
        if (pi >= 0 && pi < _patterns.Items.Count) _patterns.SelectedIndex = pi;

        int si = _song.SelectedIndex;
        _song.BeginUpdate();
        _song.Clear();
        double beats = 0;
        for (int i = 0; i < p.Song.Count; i++)
        {
            var it = p.Song[i];
            string name = it.Pattern >= 0 && it.Pattern < p.Patterns.Count ? p.Patterns[it.Pattern].Name : "(missing)";
            double bars = it.Pattern >= 0 && it.Pattern < p.Patterns.Count ? p.Patterns[it.Pattern].Bars : 0;
            beats += bars * p.BarBeats * Math.Max(1, it.Repeats);
            _song.Add($"{i + 1}. {name} ×{it.Repeats}");
        }
        _song.EndUpdate();
        if (si >= 0 && si < _song.Items.Count) _song.SelectedIndex = si;
        _songInfo.Text = $"Song: {p.Song.Count} item(s), {beats / Math.Max(1, p.BarBeats):0.#} bars";

        int mi = _markers.SelectedIndex;
        _markers.BeginUpdate();
        _markers.Clear();
        foreach (var m in p.Markers.OrderBy(m => m.Beat))
            _markers.Add($"{m.Beat:0.##} — {m.Name} (energy {m.Energy})");
        _markers.EndUpdate();
        if (mi >= 0 && mi < _markers.Items.Count) _markers.SelectedIndex = mi;

    }

    private Pattern? SelectedPattern() =>
        _patterns.SelectedIndex >= 0 && _patterns.SelectedIndex < _host.Project.Patterns.Count
            ? _host.Project.Patterns[_patterns.SelectedIndex]
            : null;

    private Marker? SelectedMarker()
    {
        var sorted = _host.Project.Markers.OrderBy(m => m.Beat).ToList();
        return _markers.SelectedIndex >= 0 && _markers.SelectedIndex < sorted.Count ? sorted[_markers.SelectedIndex] : null;
    }

    private void NewPattern()
    {
        _host.BeginEdit("pattern");
        var pat = Pattern.Capture(_host.Project, "Pattern " + (_host.Project.Patterns.Count + 1));
        _host.Project.Patterns.Add(pat);
        _host.MarkDirty();
        RefreshLists();
        _patterns.SelectedIndex = _patterns.Items.Count - 1;
    }

    private void CaptureIntoSelected()
    {
        var pat = SelectedPattern();
        if (pat == null) return;
        _host.BeginEdit("pattern");
        pat.Bars = _host.Project.Bars;
        pat.Notes.Clear();
        pat.Mutes.Clear();
        foreach (var t in _host.Project.Tracks)
        {
            pat.Notes.Add(t.Notes.Select(n => n.Clone()).ToList());
            pat.Mutes.Add(t.Mute);
        }
        _host.MarkDirty();
        RefreshLists();
    }

    private void ApplySelected()
    {
        var pat = SelectedPattern();
        if (pat == null) return;
        _host.BeginEdit("pattern");
        pat.Apply(_host.Project);
        _host.MarkDirty();
        _host.NotifyTracksChanged();
        RefreshLists();
    }

    private void DeletePattern()
    {
        int i = _patterns.SelectedIndex;
        if (i < 0 || i >= _host.Project.Patterns.Count) return;
        _host.BeginEdit("pattern");
        _host.Project.Patterns.RemoveAt(i);
        var song = _host.Project.Song;
        foreach (var it in song) if (it.Pattern > i) it.Pattern--;
        song.RemoveAll(it => it.Pattern == i);
        _host.MarkDirty();
        RefreshLists();
    }

    private void AddSongItem()
    {
        int i = _patterns.SelectedIndex;
        if (i < 0 || i >= _host.Project.Patterns.Count) return;
        _host.BeginEdit("song");
        _host.Project.Song.Add(new SongItem(i, (int)_repeats.Value));
        _host.MarkDirty();
        RefreshLists();
        _song.SelectedIndex = _song.Items.Count - 1;
    }

    private void RemoveSongItem()
    {
        int i = _song.SelectedIndex;
        if (i < 0 || i >= _host.Project.Song.Count) return;
        _host.BeginEdit("song");
        _host.Project.Song.RemoveAt(i);
        _host.MarkDirty();
        RefreshLists();
    }

    private void MoveSongItem(int delta)
    {
        int i = _song.SelectedIndex;
        int j = i + delta;
        if (i < 0 || j < 0 || j >= _host.Project.Song.Count) return;
        _host.BeginEdit("song");
        var item = _host.Project.Song[i];
        _host.Project.Song.RemoveAt(i);
        _host.Project.Song.Insert(j, item);
        _host.MarkDirty();
        RefreshLists();
        _song.SelectedIndex = j;
    }

    private void SetRepeats()
    {
        int i = _song.SelectedIndex;
        if (i < 0 || i >= _host.Project.Song.Count) return;
        _host.BeginEdit("song");
        _host.Project.Song[i].Repeats = (int)_repeats.Value;
        _host.MarkDirty();
        RefreshLists();
    }

    private void AddMarker()
    {
        var name = string.IsNullOrWhiteSpace(_markerName.Text) ? "Marker" : _markerName.Text.Trim();
        AddMarkerAt((double)_markerBeat.Value, name, (int)_markerEnergy.Value);
    }

    private void AddMarkerAtPlayhead()
    {
        double beat = _host.PlayheadBeat;
        if (beat < 0) beat = 0;
        AddMarkerAt(beat, string.IsNullOrWhiteSpace(_markerName.Text) ? "Marker" : _markerName.Text.Trim(), (int)_markerEnergy.Value);
    }

    private void AddMarkerAt(double beat, string name, int energy)
    {
        _host.BeginEdit("marker");
        _host.Project.Markers.Add(new Marker(beat, name, Math.Clamp(energy, 1, 10)));
        _host.MarkDirty();
        RefreshLists();
    }

    private void RemoveMarker()
    {
        var m = SelectedMarker();
        if (m == null) return;
        _host.BeginEdit("marker");
        _host.Project.Markers.Remove(m);
        _host.MarkDirty();
        RefreshLists();
    }

    private void ExportMidi()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "MIDI (*.mid)|*.mid",
            FileName = "humble-baker.mid",
            InitialDirectory = _host.Settings.ResolvedOutputDirectory
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            MidiWriter.Write(_host.Project, dlg.FileName);
            _host.SetStatus("Exported MIDI: " + Path.GetFileName(dlg.FileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Export MIDI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportSuperCollider()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "SuperCollider (*.scd)|*.scd",
            FileName = "humble-baker.scd",
            InitialDirectory = _host.Settings.ResolvedOutputDirectory
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(dlg.FileName)) ?? _host.Settings.ResolvedOutputDirectory;
            var samples = Path.Combine(dir, Path.GetFileNameWithoutExtension(dlg.FileName) + "-samples");
            var script = SuperColliderExport.ToScript(_host.Project, _exportSong.Checked, samples, out int sampleFiles);
            File.WriteAllText(dlg.FileName, script);
            _host.SetStatus($"Exported {Path.GetFileName(dlg.FileName)}" + (sampleFiles > 0 ? $" + {sampleFiles} sample(s)" : ""));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Export SuperCollider", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
