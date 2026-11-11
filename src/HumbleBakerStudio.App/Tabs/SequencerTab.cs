using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

internal sealed class SequencerTab : UserControl
{
    private readonly MainForm _host;
    private readonly PianoRoll _roll = new() { Dock = DockStyle.Fill };
    private readonly TextBox _name = new() { Width = 190 };
    private readonly ComboBox _synth = new() { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _kit = new() { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _root = new() { Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _scale = new() { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _grid = new() { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _snapScale = new() { Text = "Snap to scale", AutoSize = true };
    private readonly Label _info = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly CheckBox _mute = new() { Text = "Mute", AutoSize = true };
    private readonly CheckBox _solo = new() { Text = "Solo", AutoSize = true };
    private bool _syncing;

    public SequencerTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var side = new Panel { Dock = DockStyle.Right, Width = 236, BackColor = Theme.Panel, Padding = new Padding(10), AutoScroll = true };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Theme.Panel };
        side.Controls.Add(stack);

        stack.Controls.Add(Theme.Label("TRACK", Theme.Muted, Theme.UiBold));
        stack.Controls.Add(_name);
        stack.Controls.Add(Theme.Label("Engine", Theme.Muted));
        _synth.Items.AddRange(new object[] { "acid", "saw", "square", "tri", "sine", "pad", "fm", "pluck", "noise", "drums", "patch", "sample" });
        stack.Controls.Add(_synth);
        stack.Controls.Add(Theme.Label("Drum kit", Theme.Muted));
        _kit.Items.AddRange(SampleLibrary.BuiltInKits);
        stack.Controls.Add(_kit);
        stack.Controls.Add(Spacer());
        stack.Controls.Add(Theme.Label("KEY / SCALE", Theme.Muted, Theme.UiBold));
        var keyRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Panel };
        _root.Items.AddRange(new object[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" });
        keyRow.Controls.Add(_root);
        keyRow.Controls.Add(_scale);
        stack.Controls.Add(keyRow);
        _scale.Items.AddRange(Scales.Names);
        stack.Controls.Add(_snapScale);
        stack.Controls.Add(Spacer());
        stack.Controls.Add(Theme.Label("GRID", Theme.Muted, Theme.UiBold));
        _grid.Items.AddRange(new object[] { "1/4", "1/8", "1/16", "1/32" });
        _grid.SelectedIndex = 2;
        stack.Controls.Add(_grid);
        stack.Controls.Add(Spacer());
        var flags = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Panel };
        flags.Controls.Add(_mute);
        flags.Controls.Add(_solo);
        stack.Controls.Add(flags);
        stack.Controls.Add(Spacer());
        stack.Controls.Add(Theme.Button("Quantize", (_, _) => Quantize()));
        stack.Controls.Add(Theme.Button("Clear notes", (_, _) => ClearNotes()));
        stack.Controls.Add(Theme.Button("Play / stop (Space)", (_, _) => _host.TogglePlayback()));
        stack.Controls.Add(Spacer());
        stack.Controls.Add(_info);
        stack.Controls.Add(Theme.Label("Tip: drag to add notes, drag the right edge to resize, right-click to delete.", Theme.Muted, Theme.Small));

        Controls.Add(_roll);
        Controls.Add(side);

        _roll.Changed += (_, _) =>
        {
            UpdateInfo();
            _host.MarkDirty();
        };
        _roll.BeginEdit += (_, _) => { };
        _roll.SpaceRequested += (_, _) => _host.TogglePlayback();

        _name.TextChanged += (_, _) =>
        {
            if (_syncing || _host.SelectedTrack == null) return;
            _host.SelectedTrack.Name = _name.Text.Trim();
            _host.MarkDirty();
        };
        _synth.SelectedIndexChanged += (_, _) =>
        {
            if (_syncing || _host.SelectedTrack == null) return;
            var t = _host.SelectedTrack;
            t.Synth = (string)_synth.SelectedItem!;
            if (t.Synth == "patch" && t.Patch == null) t.Patch = new Patch();
            _host.MarkDirty();
            _host.SelectTrack(t, fromList: true);
        };
        _kit.SelectedIndexChanged += (_, _) =>
        {
            if (_syncing || _host.SelectedTrack == null) return;
            _host.SelectedTrack.DrumKit = (string)_kit.SelectedItem!;
            _host.MarkDirty();
        };
        _root.SelectedIndexChanged += (_, _) => ApplyKey();
        _scale.SelectedIndexChanged += (_, _) => ApplyKey();
        _snapScale.CheckedChanged += (_, _) =>
        {
            _roll.SnapScale = _snapScale.Checked;
            _roll.Invalidate();
        };
        _grid.SelectedIndexChanged += (_, _) =>
        {
            _roll.Grid = _grid.SelectedIndex switch { 0 => 1.0, 1 => 0.5, 2 => 0.25, _ => 0.125 };
        };
        _mute.CheckedChanged += (_, _) => { if (!_syncing && _host.SelectedTrack != null) { _host.SelectedTrack.Mute = _mute.Checked; _host.MarkDirty(); _host.NotifyTracksChanged(); } };
        _solo.CheckedChanged += (_, _) => { if (!_syncing && _host.SelectedTrack != null) { _host.SelectedTrack.Solo = _solo.Checked; _host.MarkDirty(); _host.NotifyTracksChanged(); } };
    }

    private static Control Spacer() => new Panel { Height = 8, Width = 8, BackColor = Theme.Panel };

    public void SetProject(Project project, Track? track)
    {
        _roll.Proj = project;
        _roll.Zoom = 96;
        _roll.RowH = 15;
        _roll.UpdateScroll();
        SetTrack(track);
    }

    public void SetTrack(Track? track)
    {
        _syncing = true;
        try
        {
            _roll.Cur = track!;
            if (track != null)
            {
                _name.Text = track.Name;
                _synth.SelectedItem = track.Synth;
                _kit.SelectedItem = track.DrumKit;
                _kit.Enabled = track.Synth == "drums";
                _mute.Checked = track.Mute;
                _solo.Checked = track.Solo;
                _root.SelectedIndex = ((_host.Project.Key % 12) + 12) % 12;
                _scale.SelectedItem = _host.Project.Scale;
                _roll.Root = _host.Project.Key;
                _roll.Mask = Scales.Mask(_host.Project.Key, _host.Project.Scale);
                _roll.ScrollToPitch(track.Synth == "drums" ? 42 : 60);
            }
            UpdateInfo();
            _roll.Invalidate();
        }
        finally
        {
            _syncing = false;
        }
    }

    public void RefreshRoll() => _roll.Invalidate();

    /// <summary>Re-applies scroll and zoom once the control has its final size.</summary>
    public void ScrollToContent()
    {
        _roll.UpdateScroll();
        _roll.ScrollToPitch(_host.SelectedTrack?.Synth == "drums" ? 42 : 60);
        _roll.Invalidate();
    }

    public void SetPlayhead(double beat) { _roll.Playhead = beat; _roll.Invalidate(); }

    private void ApplyKey()
    {
        if (_syncing) return;
        _host.Project.Key = Math.Max(0, _root.SelectedIndex);
        _host.Project.Scale = _scale.SelectedItem as string ?? "minor";
        _roll.Root = _host.Project.Key;
        _roll.Mask = Scales.Mask(_host.Project.Key, _host.Project.Scale);
        _roll.Invalidate();
        _host.MarkDirty();
    }

    private void UpdateInfo()
    {
        var t = _host.SelectedTrack;
        if (t == null) { _info.Text = ""; return; }
        double avg = t.Notes.Count > 0 ? t.Notes.Average(n => n.Pitch) : 0;
        _info.Text = $"{t.Notes.Count} notes · {t.Notes.Count(x => x.Pitch < avg):0} low / {t.Notes.Count(x => x.Pitch >= avg):0} high\n" +
                     $"bars {_host.Project.Bars} · loop {_host.Project.LoopBeats:0.##} beats";
    }

    private void Quantize()
    {
        var t = _host.SelectedTrack;
        if (t == null) return;
        double g = _roll.Grid;
        foreach (var n in t.Notes) n.Start = Math.Round(n.Start / g) * g;
        _roll.Invalidate();
        _host.MarkDirty();
    }

    private void ClearNotes()
    {
        var t = _host.SelectedTrack;
        if (t == null || t.Notes.Count == 0) return;
        if (MessageBox.Show(this, $"Remove all {t.Notes.Count} notes from \"{t.Name}\"?", "Clear notes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        t.Notes.Clear();
        _roll.Invalidate();
        _host.MarkDirty();
    }
}
