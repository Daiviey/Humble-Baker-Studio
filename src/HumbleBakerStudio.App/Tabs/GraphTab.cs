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

    private readonly Panel _inspector = new() { Dock = DockStyle.Right, Width = 336, BackColor = Theme.Panel, Padding = new Padding(10) };
    private readonly Label _selTitle = new() { Dock = DockStyle.Top, Height = 38, ForeColor = Theme.Text, Font = Theme.UiBold, Text = "Select a module" };
    private readonly ComboBox _module = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.PanelAlt, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat };
    private readonly CheckBox _bypass = new() { Dock = DockStyle.Top, Height = 26, Text = "Bypass module", ForeColor = Theme.Text };
    private readonly FlowLayoutPanel _knobs = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Panel, Padding = new Padding(2) };
    private readonly Label _moduleHint = new() { Dock = DockStyle.Fill, ForeColor = Theme.Muted, Font = Theme.Small, TextAlign = ContentAlignment.MiddleCenter, Text = "Select an FX module to edit its parameters." };
    private bool _syncing;

    private sealed class ModuleChoice
    {
        public string Label { get; }
        public string Type { get; }
        public ModuleChoice(string label, string type) { Label = label; Type = type; }
    }

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

        var head = new Label { Dock = DockStyle.Top, Height = 22, Text = "MODULE", ForeColor = Theme.Accent, Font = Theme.Section };
        _module.DisplayMember = "Label";
        _module.Items.Add(new ModuleChoice("(choose a module)", ""));
        foreach (var entry in PatchGraphControl.FxCatalog)
            _module.Items.Add(new ModuleChoice(entry[0], entry[1]));
        _module.SelectedIndex = 0;

        _inspector.Controls.Add(_knobs);
        _inspector.Controls.Add(_moduleHint);
        _inspector.Controls.Add(_bypass);
        _inspector.Controls.Add(_module);
        _inspector.Controls.Add(_selTitle);
        _inspector.Controls.Add(head);

        Controls.Add(_graph);
        Controls.Add(_empty);
        Controls.Add(_inspector);
        Controls.Add(bar);

        _graph.GraphChanged += (_, _) => { _host.BeginEdit("graph"); _host.MarkDirty(); };
        _graph.SelectionChanged += (_, _) => UpdateInspector();
        _module.SelectedIndexChanged += (_, _) =>
        {
            if (_syncing) return;
            var node = _graph.Selected;
            if (node == null || node.Kind != "fx") return;
            var choice = _module.SelectedItem as ModuleChoice;
            node.Module = choice == null ? "" : choice.Type;
            _host.BeginEdit("graph");
            _host.MarkDirty();
            _graph.Invalidate();
            BuildModuleKnobs(node);
        };
        _bypass.CheckedChanged += (_, _) =>
        {
            if (_syncing) return;
            var node = _graph.Selected;
            if (node == null) return;
            _host.BeginEdit("graph");
            node.Bypass = _bypass.Checked;
            _host.MarkDirty();
            _graph.Invalidate();
        };
        UpdateInspector();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) return;
        EnsureGraph();
        _graph.Proj = _host.Project;
        UpdateInfo();
        UpdateInspector();
        if (IsHandleCreated) BeginInvoke(new Action(() => _graph.FrameAll()));
    }

    public void RefreshView()
    {
        _graph.Proj = _host.Project;
        if (Visible) EnsureGraph();
        UpdateInfo();
        UpdateInspector();
        if (Visible && IsHandleCreated) BeginInvoke(new Action(() => _graph.FrameAll()));
    }

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

    private void UpdateInspector()
    {
        _syncing = true;
        try
        {
            var node = _graph.Selected;
            if (node == null)
            {
                _selTitle.Text = "Select a module";
                _module.Visible = false;
                _bypass.Visible = false;
                _knobs.Visible = false;
                _moduleHint.Visible = true;
                _moduleHint.Text = "Select a module on the graph. FX nodes can be edited here.";
                _knobs.Controls.Clear();
                return;
            }

            _moduleHint.Visible = false;
            _knobs.Visible = true;
            _bypass.Visible = node.Kind == "fx" || node.Kind == "bus";
            _bypass.Checked = node.Bypass;

            if (node.Kind == "fx")
            {
                _selTitle.Text = string.IsNullOrEmpty(node.Label) ? "FX module" : node.Label;
                _module.Visible = true;
                int idx = 0;
                if (!string.IsNullOrEmpty(node.Module))
                {
                    for (int i = 0; i < _module.Items.Count; i++)
                        if (_module.Items[i] is ModuleChoice c && c.Type == node.Module) { idx = i; break; }
                }
                _module.SelectedIndex = idx;
                BuildModuleKnobs(node);
            }
            else
            {
                _module.Visible = false;
                _selTitle.Text = node.Kind == "track" ? "Track: " + node.Label : node.Kind == "out" ? "Master out" : node.Label;
                _knobs.Controls.Clear();
                if (node.Kind == "track")
                {
                    var hint = new Label
                    {
                        AutoSize = false,
                        Width = 300,
                        Height = 80,
                        ForeColor = Theme.Muted,
                        Font = Theme.Small,
                        Text = "This is the track's entry point. Edit effects on its FX module or on the Effects tab."
                    };
                    _knobs.Controls.Add(hint);
                }
                else
                {
                    KnobPanel.Build(node, _knobs, (_, _) => { _host.MarkDirty(); _graph.Invalidate(); }, Theme.Panel, Theme.Text, Theme.Accent);
                }
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private void BuildModuleKnobs(PatchNode node)
    {
        if (node == null || node.Kind != "fx") return;
        if (string.IsNullOrEmpty(node.Module))
        {
            _knobs.Controls.Clear();
            _knobs.Controls.Add(new Label
            {
                AutoSize = false,
                Width = 300,
                Height = 60,
                ForeColor = Theme.Muted,
                Font = Theme.Small,
                Text = "Choose an effect module above to show its parameters."
            });
            return;
        }
        KnobPanel.Build(node.Fx, _knobs, (_, _) =>
        {
            _host.MarkDirty();
            _graph.Invalidate();
        }, Theme.Panel, Theme.Text, Theme.Accent, node.Module);
    }

    private void AutoPatch()
    {
        _host.BeginEdit("graph");
        PatchGraphControl.AutoPatch(_host.Project);
        _graph.Proj = _host.Project;
        _graph.FrameAll();
        _host.MarkDirty();
        UpdateInfo();
        UpdateInspector();
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
