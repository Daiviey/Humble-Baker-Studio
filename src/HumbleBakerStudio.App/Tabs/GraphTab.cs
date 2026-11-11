using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

internal sealed class GraphTab : UserControl
{
    private readonly MainForm _host;
    private readonly PatchGraphControl _graph = new() { Dock = DockStyle.Fill };
    private readonly Label _info = new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(8, 8, 3, 3) };

    public GraphTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Panel, Padding = new Padding(10, 7, 6, 4), WrapContents = false };
        bar.Controls.Add(Theme.Button("Auto-patch", (_, _) => AutoPatch()));
        bar.Controls.Add(Theme.Button("Add track nodes", (_, _) => AddTrackNodes()));
        bar.Controls.Add(Theme.Button("Tidy", (_, _) => { _graph.Tidy(); _host.MarkDirty(); }));
        bar.Controls.Add(Theme.Button("Frame all", (_, _) => _graph.FrameAll()));
        bar.Controls.Add(Theme.Button("Group selection", (_, _) => { _graph.GroupSelectionInteractive(); _host.MarkDirty(); }));
        bar.Controls.Add(Theme.Button("Delete selected", (_, _) => { _graph.DeleteSelected(); _host.MarkDirty(); }));
        bar.Controls.Add(_info);

        Controls.Add(_graph);
        Controls.Add(bar);

        _graph.GraphChanged += (_, _) => _host.MarkDirty();
        _graph.SelectionChanged += (_, _) => UpdateInfo();
    }

    public void RefreshView()
    {
        _graph.Proj = _host.Project;
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        var g = _host.Project.Graph;
        _info.Text = $"{g.Nodes.Count} nodes · {g.Links.Count} links · {(PatchRender.Active(_host.Project) ? "graph drives the mix" : "mixer drives the mix")}";
    }

    private void AutoPatch()
    {
        PatchGraphControl.AutoPatch(_host.Project);
        _graph.Proj = _host.Project;
        _graph.FrameAll();
        _host.MarkDirty();
        UpdateInfo();
    }

    private void AddTrackNodes()
    {
        foreach (var t in _host.Project.Tracks)
            PatchGraphControl.AddTrackNode(_host.Project, t);
        _graph.Proj = _host.Project;
        _host.MarkDirty();
        UpdateInfo();
    }
}
