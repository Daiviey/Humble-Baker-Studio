using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

internal sealed class GraphTab : UserControl
{
    private readonly MainForm _host;
    private readonly PatchGraphControl _graph = new() { Dock = DockStyle.Fill };
    private readonly Label _info = new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(8, 8, 3, 3) };
    private readonly Label _empty = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.Back,
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI Semibold", 11f),
        Text = "No graph yet.\r\n\r\nPress Auto-patch to wire the tracks, buses and master out.",
        Visible = false
    };

    public GraphTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.Panel, Padding = new Padding(10, 8, 6, 4), WrapContents = false };
        bar.Controls.Add(Theme.Button("Auto-patch", (_, _) => AutoPatch(), true, "Wire tracks, FX modules, buses and the master out"));
        bar.Controls.Add(Theme.Button("Add track nodes", (_, _) => AddTrackNodes()));
        bar.Controls.Add(Theme.Button("Tidy", (_, _) => { _graph.Tidy(); _host.MarkDirty(); }));
        bar.Controls.Add(Theme.Button("Frame all", (_, _) => _graph.FrameAll()));
        bar.Controls.Add(Theme.Button("Group selection", (_, _) => { _graph.GroupSelectionInteractive(); _host.MarkDirty(); }));
        bar.Controls.Add(Theme.Button("Delete selected", (_, _) => { _graph.DeleteSelected(); _host.MarkDirty(); }));
        bar.Controls.Add(_info);

        Controls.Add(_graph);
        Controls.Add(_empty);
        Controls.Add(bar);

        _graph.GraphChanged += (_, _) => { _host.BeginEdit("graph"); _host.MarkDirty(); };
        _graph.SelectionChanged += (_, _) => UpdateInfo();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) return;
        EnsureGraph();
        _graph.Proj = _host.Project;
        UpdateInfo();
        if (IsHandleCreated) BeginInvoke(new Action(() => _graph.FrameAll()));
    }

    public void RefreshView()
    {
        _graph.Proj = _host.Project;
        if (Visible) EnsureGraph();
        UpdateInfo();
        if (Visible && IsHandleCreated) BeginInvoke(new Action(() => _graph.FrameAll()));
    }

    /// <summary>First visit wires the graph exactly like the PowerShell studio does.</summary>
    private void EnsureGraph()
    {
        if (_host.Project.Graph.Initialized || _host.Project.Tracks.Count == 0) return;
        PatchGraphControl.AutoPatch(_host.Project);
        _graph.Proj = _host.Project;
    }

    private void UpdateInfo()
    {
        var g = _host.Project.Graph;
        _info.Text = $"{g.Nodes.Count} nodes · {g.Links.Count} links · {(PatchRender.Active(_host.Project) ? "graph drives the mix" : "mixer drives the mix")}";
        _empty.Visible = g.Nodes.Count == 0;
        if (_empty.Visible) _empty.BringToFront();
    }

    private void AutoPatch()
    {
        _host.BeginEdit("graph");
        PatchGraphControl.AutoPatch(_host.Project);
        _graph.Proj = _host.Project;
        _graph.FrameAll();
        _host.MarkDirty();
        UpdateInfo();
    }

    private void AddTrackNodes()
    {
        _host.BeginEdit("graph");
        foreach (var t in _host.Project.Tracks)
            PatchGraphControl.AddTrackNode(_host.Project, t);
        _graph.Proj = _host.Project;
        _host.MarkDirty();
        UpdateInfo();
    }
}
