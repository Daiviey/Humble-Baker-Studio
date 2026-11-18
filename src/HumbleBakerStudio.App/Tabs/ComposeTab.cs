using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

internal sealed class ComposeTab : UserControl
{
    private readonly MainForm _host;
    private readonly ComposeLanes _lanes = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _param = new() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _hint;

    public ComposeTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        foreach (var d in AutoCatalog.All) _param.Items.Add($"{d.Module} - {d.Name}");
        _param.SelectedIndex = 0;

        _hint = Theme.Label("Select a track on the left. Click a lane to add a point, drag to move, right-click a point to delete. Ctrl+wheel zooms.", Theme.Muted, Theme.Small);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            BackColor = Theme.Panel,
            Padding = new Padding(10, 6, 10, 6)
        };
        bar.Controls.Add(Theme.Label("Add lane", Theme.Muted));
        bar.Controls.Add(_param);
        bar.Controls.Add(Theme.Button("Add lane", (_, _) => AddLane()));
        bar.Controls.Add(Theme.Button("Remove last", (_, _) => RemoveLast()));
        bar.Controls.Add(Theme.Button("Fit", (_, _) => _lanes.Fit()));
        bar.Controls.Add(_hint);

        Controls.Add(_lanes);
        Controls.Add(bar);

        _lanes.Changed += (_, _) =>
        {
            _host.BeginEdit("automation");
            _host.MarkDirty();
        };
    }

    public void RefreshView()
    {
        _lanes.Proj = _host.Project;
        _lanes.Invalidate();
    }

    private void AddLane()
    {
        var t = _host.SelectedTrack;
        if (t == null) { _host.SetStatus("Select a track on the left first."); return; }
        int i = _param.SelectedIndex;
        if (i < 0 || i >= AutoCatalog.All.Count) return;
        var param = AutoCatalog.All[i].Param;
        double trackBeats = _host.Project.Song.Count > 0 ? Arranger.SongBeats(_host.Project) : _host.Project.LoopBeats;
        if (trackBeats <= 0) trackBeats = _host.Project.LoopBeats;
        var lane = new AutoLane(_host.Project.Tracks.IndexOf(t), param);
        double neut = AutoRange.Neutral(param);
        if (double.IsNaN(neut)) neut = AutoRange.MinOf(param);
        lane.Points.Add(new double[] { 0, neut });
        lane.Points.Add(new double[] { trackBeats, neut });
        t.Auto.Add(lane);
        _host.BeginEdit("automation");
        _host.MarkDirty();
        _lanes.Invalidate();
        _host.SetStatus($"Added a {param} lane to \"{t.Name}\".");
    }

    private void RemoveLast()
    {
        var t = _host.SelectedTrack;
        if (t == null) { _host.SetStatus("Select a track on the left first."); return; }
        if (t.Auto.Count == 0) { _host.SetStatus($"\"{t.Name}\" has no automation lanes."); return; }
        var lane = t.Auto[t.Auto.Count - 1];
        t.Auto.RemoveAt(t.Auto.Count - 1);
        _host.BeginEdit("automation");
        _host.MarkDirty();
        _lanes.Invalidate();
        _host.SetStatus($"Removed the last {lane.Param} lane from \"{t.Name}\".");
    }
}
