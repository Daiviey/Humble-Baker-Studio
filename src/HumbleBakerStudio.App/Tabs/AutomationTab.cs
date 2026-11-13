using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

/// <summary>
/// Automation painting: draw a parameter curve on a canvas, then stamp it into an automation lane.
/// The canvas maps time horizontally and the parameter range vertically; brightness becomes the
/// Z axis so a faint stroke can scale or bias the value away from the target's neutral setting.
/// Works on tracks, shared buses and patch-graph FX/group/bus modules.
/// </summary>
internal sealed class AutomationTab : UserControl
{
    private sealed class AutoTarget
    {
        public string Label = "";
        public string Kind = "track";     // track | node | bus
        public string Name = "";
        public List<AutoLane> Lanes = new();
        public Fx? Fx;
        public Patch? Patch;
        public int TrackIndex;
        public Track? Track;
    }

    private readonly MainForm _host;
    private SpectralGrid _grid = new(1024, 256);
    private readonly AutoCanvas _canvas = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { AutoSize = false, Width = 340, Height = 58, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly Label _targetLabel = new() { AutoSize = false, Width = 340, Height = 32, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly ComboBox _target = new() { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _module = new() { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _param = new() { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _brush = new() { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _map = new() { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _z = new() { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _detail = new() { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _resolution = new() { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _brushSize = new() { Minimum = 1, Maximum = 60, DecimalPlaces = 0, Increment = 1, Value = 12, Width = 64 };
    private readonly NumericUpDown _zDepth = new() { Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.05m, Value = 1, Width = 64 };

    // Layered spline extraction.
    private readonly ListBox _layers = new() { Width = 340, Height = 130, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
    private readonly ComboBox _layerParam = new() { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _normalize = new() { Text = "Normalize by amplitude", AutoSize = true, Checked = true, ForeColor = Theme.Text };
    private readonly NumericUpDown _layerGain = new() { Minimum = 0, Maximum = 2, DecimalPlaces = 2, Increment = 0.05m, Value = 1, Width = 64 };
    private readonly ComboBox _mix = new() { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _showLayers = new() { Text = "Show layer outlines", AutoSize = true, Checked = true, ForeColor = Theme.Text };
    private readonly CheckBox _separateStrokes = new() { Text = "Keep crossing strokes separate", AutoSize = true, Checked = true, ForeColor = Theme.Text };
    private readonly List<(string Label, string Param)> _paramOptions = new();

    private List<AutoTarget> _targets = new();
    private List<AutoLayer> _layersData = new();
    private bool _updating;
    private bool _updatingLayers;

    private static readonly Dictionary<string, string[]> Modules = new()
    {
        ["Synth Filter"] = new[] { "Cutoff", "Resonance", "Filter Env" },
        ["Synth Drive/Crush"] = new[] { "Drive", "Bits", "Fold" },
        ["Synth FM/Unison"] = new[] { "FM Amount", "Unison", "Noise Level" },
        ["Synth LFO"] = new[] { "LFO Rate", "LFO Filter", "LFO Pitch" },
        ["Track FX Filter"] = new[] { "Cutoff", "Resonance", "Filter LFO Depth" },
        ["Track FX Drive/Crush"] = new[] { "Drive", "Bits", "Fold" },
        ["Track FX Ring"] = new[] { "Ring Mix", "Ring Freq" },
        ["Track FX Chorus"] = new[] { "Chorus", "Chorus Rate", "Chorus Depth" },
        ["Track FX Delay"] = new[] { "Delay Mix", "Delay Beats", "Delay Feedback" },
        ["Track FX Reverb"] = new[] { "Reverb Mix", "Reverb Size" },
        ["Track FX Phaser/Flanger"] = new[] { "Phaser Mix", "Phaser Rate", "Phaser Depth", "Flanger Mix", "Flanger Rate", "Flanger Depth", "Flanger Feedback" },
        ["Track FX Tape"] = new[] { "Tape Stop", "Tape Stop Time", "Tape Delay Mix", "Tape Delay Ms", "Tape Delay Feedback", "Tape Delay Wow" },
        ["Track FX Plate/Vinyl"] = new[] { "Plate Mix", "Plate Size", "Vinyl Crackle", "Tape Hiss", "Tape Wow" },
        ["Track FX Compressor"] = new[] { "Threshold", "Ratio", "Attack", "Release", "Makeup Gain", "Sidechain Mix", "Sidechain Attack", "Sidechain Release" },
        ["Track FX EQ/Width"] = new[] { "Low Shelf", "High Shelf", "Mid Peak", "Width", "Pitch Shift" },
        ["Track Mixer"] = new[] { "Volume", "Pan", "Low-Pass (post FX)", "High-Pass (post FX)" }
    };

    public AutomationTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;
        _canvas.Grid = _grid;

        _module.Items.AddRange(Modules.Keys.Cast<object>().ToArray());
        _module.SelectedIndex = 0;
        _brush.Items.AddRange(new object[] { "Hard", "Soft", "Smooth", "Streak", "Gradient", "Smudge", "Smear" });
        _brush.SelectedIndex = 0;
        _map.Items.AddRange(new object[] { "Exact shape", "Smooth curve", "Quantize steps", "Linear ramp" });
        _map.SelectedIndex = 0;
        _z.Items.AddRange(new object[] { "Depth (scale value)", "Direct value", "Off (position only)" });
        _z.SelectedIndex = 0;
        _detail.Items.AddRange(new object[] { "Curve (single value)", "Stroke (follow rows)" });
        _detail.SelectedIndex = 0;
        _mix.Items.AddRange(new object[] { "Add", "Multiply", "Max", "Replace" });
        _mix.SelectedIndex = 0;
        _resolution.Items.AddRange(new object[]
        {
            "Normal 512 × 128",
            "High 1024 × 256",
            "Extreme 2048 × 512",
            "Ultra 4096 × 512"
        });
        _resolution.SelectedIndex = 1;

        foreach (var entry in ParamMap)
        {
            var (param, _, _) = entry.Value;
            if (_paramOptions.Any(o => o.Param == param)) continue;
            _paramOptions.Add((entry.Key.Replace('|', '/'), param));
        }

        BuildUi();
        foreach (var option in _paramOptions) _layerParam.Items.Add(option.Label);

        _brush.SelectedIndexChanged += (_, _) => _canvas.BrushKind = _brush.SelectedIndex;
        _brushSize.ValueChanged += (_, _) => _canvas.Brush = (double)_brushSize.Value;
        _module.SelectedIndexChanged += (_, _) => { if (_updating) return; UpdateParams(); SyncCanvasFromLane(); };
        _param.SelectedIndexChanged += (_, _) => { if (_updating) return; SyncCanvasFromLane(); };
        _target.SelectedIndexChanged += (_, _) => OnTargetChanged();
        _target.DropDown += (_, _) => RefreshTargets(keepSelection: true);
        _layers.SelectedIndexChanged += (_, _) => UpdateLayerParamCombo();
        _layerParam.SelectedIndexChanged += (_, _) => AssignLayerParam();
        _showLayers.CheckedChanged += (_, _) => { _canvas.ShowLayers = _showLayers.Checked; _canvas.Invalidate(); };
        _separateStrokes.CheckedChanged += (_, _) => { _canvas.CaptureStrokes = _separateStrokes.Checked; if (!_separateStrokes.Checked) _canvas.ClearStrokes(); };
        _resolution.SelectedIndexChanged += (_, _) => ChangeResolution();
        _canvas.CaptureStrokes = _separateStrokes.Checked;

        _canvas.BrushKind = 0;
        _canvas.Brush = (double)_brushSize.Value;
        RefreshTargets(keepSelection: false);
        UpdateParams();
        SyncCanvasFromLane();
    }

    private void BuildUi()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
            BackColor = Theme.Back
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var left = new CardPanel { Dock = DockStyle.Fill, BackColor = Theme.Back, Margin = new Padding(0, 0, 8, 0) };
        left.Controls.Add(_canvas);
        left.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(8, 8, 0, 0),
            Text = "Paint a parameter curve: left = start, right = end, bottom = low value, top = high value. Brightness = depth.",
            ForeColor = Theme.Muted,
            Font = Theme.Small,
            BackColor = Theme.Back
        });
        body.Controls.Add(left, 0, 0);

        var side = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Panel,
            Padding = new Padding(10)
        };

        side.Controls.Add(Theme.SectionLabel("Target"));
        side.Controls.Add(_target);
        side.Controls.Add(_targetLabel);
        side.Controls.Add(Theme.Label("FX / module", Theme.Muted));
        side.Controls.Add(_module);
        side.Controls.Add(Theme.Label("Parameter", Theme.Muted));
        side.Controls.Add(_param);

        side.Controls.Add(Theme.SectionLabel("Brush"));
        side.Controls.Add(Row(_brush, Theme.Label("Size", Theme.Muted), _brushSize));

        side.Controls.Add(Theme.SectionLabel("Mapping"));
        side.Controls.Add(Row(_map, _z));
        side.Controls.Add(Row(Theme.Label("Z depth", Theme.Muted), _zDepth));
        side.Controls.Add(Theme.Label("Shape detail", Theme.Muted));
        side.Controls.Add(_detail);
        side.Controls.Add(Theme.Label("Canvas resolution", Theme.Muted));
        side.Controls.Add(_resolution);

        side.Controls.Add(Theme.SectionLabel("Write"));
        side.Controls.Add(Row(
            Theme.Button("Write to Lane", (_, _) => ApplyPainting(), true),
            Theme.Button("Apply to Piano Roll", (_, _) => ApplyPainting())));
        side.Controls.Add(Row(
            Theme.Button("Clear Canvas", (_, _) => { _grid.Clear(); _canvas.ClearStrokes(); _canvas.Invalidate(); SetStatus("Automation canvas cleared (painting erased)."); }),
            Theme.Button("Clear Lane", (_, _) => ClearLane())));
        side.Controls.Add(Theme.Button("Reset Track Auto", (_, _) => ResetAutomation()));

        side.Controls.Add(Theme.SectionLabel("Layers (spectral splines)"));
        side.Controls.Add(Theme.Label("Paint separate waves, then extract them as layers.", Theme.Muted, Theme.Small));
        side.Controls.Add(_separateStrokes);
        side.Controls.Add(Row(
            Theme.Button("Extract layers", (_, _) => ExtractLayers(), true),
            Theme.Button("Write layers", (_, _) => WriteLayers(), true),
            Theme.Button("Clear layers", (_, _) => ClearLayers())));
        side.Controls.Add(_layers);
        side.Controls.Add(Theme.Label("Layer parameter (select a layer first)", Theme.Muted));
        side.Controls.Add(_layerParam);
        side.Controls.Add(Row(_normalize, Theme.Label("Gain", Theme.Muted), _layerGain));
        side.Controls.Add(Row(Theme.Label("Layer mix", Theme.Muted), _mix));
        side.Controls.Add(_showLayers);

        side.Controls.Add(_status);

        body.Controls.Add(side, 1, 0);
        Controls.Add(body);
    }

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Panel,
            Margin = new Padding(0, 2, 0, 2)
        };
        foreach (var c in controls) row.Controls.Add(c);
        return row;
    }

    // ---------------------------------------------------------------- targets

    private List<AutoTarget> BuildTargets()
    {
        var list = new List<AutoTarget>();
        var project = _host.Project;
        var track = _host.SelectedTrack;

        list.Add(new AutoTarget
        {
            Label = "Track",
            Kind = "track",
            Name = track?.Name ?? "(no track selected)",
            Lanes = track?.Auto ?? new List<AutoLane>(),
            Fx = track?.Fx,
            Patch = track?.Patch,
            TrackIndex = track != null ? project.Tracks.IndexOf(track) : 0,
            Track = track
        });

        foreach (var node in project.Graph.Nodes)
        {
            if (node.Kind != "fx" && node.Kind != "group" && node.Kind != "bus") continue;
            if (node.Type == "busfx" || node.Type == "trackfx") continue;
            if (node.Fx == null) continue;
            string name = string.IsNullOrWhiteSpace(node.Label) ? node.Type : node.Label;
            string label = "Node: " + name + " (" + node.Kind + ")";
            if (list.Any(x => x.Label.Equals(label, StringComparison.OrdinalIgnoreCase)))
                label += " #" + (node.Id.Length >= 4 ? node.Id[..4] : node.Id);
            list.Add(new AutoTarget
            {
                Label = label,
                Kind = "node",
                Name = label,
                Lanes = node.Auto,
                Fx = node.Fx,
                TrackIndex = 0
            });
        }

        for (int i = 0; i < project.Buses.Count; i++)
        {
            var bus = project.Buses[i];
            string label = "Bus: " + bus.Name;
            list.Add(new AutoTarget
            {
                Label = label,
                Kind = "bus",
                Name = label,
                Lanes = bus.Auto,
                Fx = bus.Fx,
                TrackIndex = 0
            });
        }

        return list;
    }

    private void RefreshTargets(bool keepSelection)
    {
        string? previous = keepSelection ? _target.SelectedItem as string : null;
        _updating = true;
        try
        {
            _targets = BuildTargets();
            _target.Items.Clear();
            foreach (var target in _targets) _target.Items.Add(target.Label);
            int index = previous != null ? _target.Items.IndexOf(previous) : 0;
            _target.SelectedIndex = index >= 0 ? index : 0;
        }
        finally
        {
            _updating = false;
        }
        UpdateTargetLabel();
    }

    private AutoTarget? GetTarget()
    {
        int index = _target.SelectedIndex;
        if (index < 0 || index >= _targets.Count) return null;
        var target = _targets[index];
        if (target.Kind == "track" && target.Track == null) return null;
        return target;
    }

    private void OnTargetChanged()
    {
        if (_updating) return;
        var target = GetTarget();
        if (target != null && target.Kind != "track" && _module.SelectedItem is string module && module.StartsWith("Synth", StringComparison.OrdinalIgnoreCase))
            _module.SelectedItem = "Track FX Filter";
        UpdateTargetLabel();
        UpdateParams();
        SyncCanvasFromLane();
    }

    private void UpdateTargetLabel()
    {
        var target = GetTarget();
        if (target == null)
        {
            _targetLabel.Text = "Target: (none). Select a track or graph module.";
            return;
        }
        _targetLabel.Text = target.Kind == "track"
            ? $"Target: {target.Name} ({target.Track?.Synth ?? "?"})"
            : "Target: " + target.Name;
    }

    // ---------------------------------------------------------------- parameters

    private void UpdateParams()
    {
        string module = _module.SelectedItem as string ?? "Synth Filter";
        string? previous = _param.SelectedItem as string;
        _updating = true;
        try
        {
            _param.Items.Clear();
            if (Modules.TryGetValue(module, out var parameters))
                foreach (string p in parameters) _param.Items.Add(p);
            int index = previous != null ? _param.Items.IndexOf(previous) : 0;
            _param.SelectedIndex = index >= 0 ? index : 0;
        }
        finally
        {
            _updating = false;
        }
    }

    private (string Param, double Min, double Max) ParamInfo()
    {
        string module = _module.SelectedItem as string ?? "";
        string name = _param.SelectedItem as string ?? "";
        string key = module + "|" + name;

        if (ParamMap.TryGetValue(key, out var info)) return info;

        return name switch
        {
            "Filter Cutoff" => ("p:Cutoff", 30, 12000),
            "Filter Resonance" => ("p:Resonance", 0, 1),
            "Drive" => ("p:Drive", 0, 1),
            "Delay Mix" => ("f:DelayMix", 0, 1),
            "Reverb Wet" => ("f:ReverbMix", 0, 1),
            _ => ("vol", 0, 1)
        };
    }

    private static readonly Dictionary<string, (string Param, double Min, double Max)> ParamMap = new()
    {
        ["Synth Filter|Cutoff"] = ("p:Cutoff", 30, 12000),
        ["Synth Filter|Resonance"] = ("p:Resonance", 0, 1),
        ["Synth Filter|Filter Env"] = ("p:FilterEnv", -1, 1),
        ["Synth Drive/Crush|Drive"] = ("p:Drive", 0, 1),
        ["Synth Drive/Crush|Bits"] = ("p:Bits", 2, 16),
        ["Synth Drive/Crush|Fold"] = ("p:Fold", 0, 1),
        ["Synth FM/Unison|FM Amount"] = ("p:FmAmount", 0, 10),
        ["Synth FM/Unison|Unison"] = ("p:Unison", 1, 7),
        ["Synth FM/Unison|Noise Level"] = ("p:NoiseLevel", 0, 1),
        ["Synth LFO|LFO Rate"] = ("p:LfoRate", 0, 40),
        ["Synth LFO|LFO Filter"] = ("p:LfoFilter", 0, 1),
        ["Synth LFO|LFO Pitch"] = ("p:LfoPitch", 0, 1200),
        ["Track FX Filter|Cutoff"] = ("f:Cutoff", 30, 12000),
        ["Track FX Filter|Resonance"] = ("f:Resonance", 0, 0.95),
        ["Track FX Filter|Filter LFO Depth"] = ("f:FilterLfoDepth", 0, 1),
        ["Track FX Drive/Crush|Drive"] = ("f:Drive", 0, 1),
        ["Track FX Drive/Crush|Bits"] = ("f:Bits", 2, 16),
        ["Track FX Drive/Crush|Fold"] = ("f:Fold", 0, 1),
        ["Track FX Ring|Ring Mix"] = ("f:RingMix", 0, 1),
        ["Track FX Ring|Ring Freq"] = ("f:RingFreq", 20, 5000),
        ["Track FX Chorus|Chorus"] = ("f:Chorus", 0, 1),
        ["Track FX Chorus|Chorus Rate"] = ("f:ChorusRate", 0.05, 10),
        ["Track FX Chorus|Chorus Depth"] = ("f:ChorusDepth", 0, 1),
        ["Track FX Delay|Delay Mix"] = ("f:DelayMix", 0, 1),
        ["Track FX Delay|Delay Beats"] = ("f:DelayBeats", 0.0625, 8),
        ["Track FX Delay|Delay Feedback"] = ("f:DelayFeedback", 0, 0.95),
        ["Track FX Reverb|Reverb Mix"] = ("f:ReverbMix", 0, 1),
        ["Track FX Reverb|Reverb Size"] = ("f:ReverbSize", 0, 1),
        ["Track FX Phaser/Flanger|Phaser Mix"] = ("f:PhaserMix", 0, 1),
        ["Track FX Phaser/Flanger|Phaser Rate"] = ("f:PhaserRate", 0.01, 8),
        ["Track FX Phaser/Flanger|Phaser Depth"] = ("f:PhaserDepth", 0, 1),
        ["Track FX Phaser/Flanger|Flanger Mix"] = ("f:FlangerMix", 0, 1),
        ["Track FX Phaser/Flanger|Flanger Rate"] = ("f:FlangerRate", 0.02, 10),
        ["Track FX Phaser/Flanger|Flanger Depth"] = ("f:FlangerDepth", 0, 1),
        ["Track FX Phaser/Flanger|Flanger Feedback"] = ("f:FlangerFb", 0, 0.95),
        ["Track FX Tape|Tape Stop"] = ("f:TapeStop", 0, 1),
        ["Track FX Tape|Tape Stop Time"] = ("f:TapeStopRate", 0.05, 3),
        ["Track FX Tape|Tape Delay Mix"] = ("f:TapeDelayMix", 0, 1),
        ["Track FX Tape|Tape Delay Ms"] = ("f:TapeDelayTime", 10, 2000),
        ["Track FX Tape|Tape Delay Feedback"] = ("f:TapeDelayFb", 0, 0.95),
        ["Track FX Tape|Tape Delay Wow"] = ("f:TapeDelayWow", 0, 1),
        ["Track FX Plate/Vinyl|Plate Mix"] = ("f:PlateMix", 0, 1),
        ["Track FX Plate/Vinyl|Plate Size"] = ("f:PlateSize", 0, 1),
        ["Track FX Plate/Vinyl|Vinyl Crackle"] = ("f:VinylCrackle", 0, 1),
        ["Track FX Plate/Vinyl|Tape Hiss"] = ("f:TapeHiss", 0, 1),
        ["Track FX Plate/Vinyl|Tape Wow"] = ("f:TapeWow", 0, 1),
        ["Track FX Compressor|Threshold"] = ("f:CompThresh", -40, 0),
        ["Track FX Compressor|Ratio"] = ("f:CompRatio", 1, 12),
        ["Track FX Compressor|Attack"] = ("f:CompAttack", 0.1, 100),
        ["Track FX Compressor|Release"] = ("f:CompRelease", 10, 1000),
        ["Track FX Compressor|Makeup Gain"] = ("f:CompMakeup", -12, 24),
        ["Track FX Compressor|Sidechain Mix"] = ("f:SidechainMix", 0, 1),
        ["Track FX Compressor|Sidechain Attack"] = ("f:SidechainAttack", 0.5, 100),
        ["Track FX Compressor|Sidechain Release"] = ("f:SidechainRelease", 10, 1000),
        ["Track FX EQ/Width|Low Shelf"] = ("f:LowShelf", -12, 12),
        ["Track FX EQ/Width|High Shelf"] = ("f:HighShelf", -12, 12),
        ["Track FX EQ/Width|Mid Peak"] = ("f:MidPeak", -12, 12),
        ["Track FX EQ/Width|Width"] = ("f:Width", 0, 1.6),
        ["Track FX EQ/Width|Pitch Shift"] = ("f:PitchShift", -12, 12),
        ["Track Mixer|Volume"] = ("vol", 0, 2),
        ["Track Mixer|Pan"] = ("pan", -1, 1),
        ["Track Mixer|Low-Pass (post FX)"] = ("lp", 20, 20000),
        ["Track Mixer|High-Pass (post FX)"] = ("hp", 20, 20000)
    };

    private static bool IsWired(string param)
    {
        if (param.StartsWith("p:")) return true;
        return param is "vol" or "pan" or "lp" or "hp"
            or "f:Cutoff" or "f:Resonance" or "f:Drive" or "f:Fold" or "f:Bits"
            or "f:RingMix" or "f:RingFreq" or "f:DelayMix" or "f:ReverbMix"
            or "f:Chorus" or "f:TapeStop" or "f:Width" or "f:CompThresh";
    }

    private double Neutral(AutoTarget target, string param, double min)
    {
        try
        {
            if (param.StartsWith("p:") && target.Patch != null)
            {
                var pi = PropIO.Find(target.Patch, param[2..]);
                if (pi != null && pi.PropertyType == typeof(double)) return (double)pi.GetValue(target.Patch)!;
            }
            if (param.StartsWith("f:") && target.Fx != null)
            {
                var pi = PropIO.Find(target.Fx, param[2..]);
                if (pi != null && pi.PropertyType == typeof(double)) return (double)pi.GetValue(target.Fx)!;
            }
        }
        catch { }
        return param switch { "vol" => 1, "pan" => 0, "lp" => 20000, "hp" => 20, _ => min };
    }

    // ---------------------------------------------------------------- canvas

    private void SyncCanvasFromLane()
    {
        if (_layersData.Count > 0)
        {
            _layersData.Clear();
            _layers.Items.Clear();
            _canvas.Layers = null;
        }
        _canvas.ClearStrokes();

        _grid.Clear();
        var target = GetTarget();
        if (target == null)
        {
            _canvas.Invalidate();
            SetStatus("Select a track or graph module target first.");
            return;
        }

        var (param, min, max) = ParamInfo();
        var lane = target.Lanes.FirstOrDefault(l => l.Param == param);
        double beats = CanvasBeats();
        string wiredNote = IsWired(param) ? "" : " This parameter is not processed by the renderer yet.";

        // A saved painted snapshot is the visual source of truth: restore it at the exact
        // resolution it was painted at instead of reconstructing an approximation from points.
        if (lane != null && lane.Preview != null && lane.PreviewCols > 0 && lane.PreviewRows > 0)
        {
            var preserved = AutomationPreview.Decode(lane.Preview, lane.PreviewCols, lane.PreviewRows);
            if (_grid.Cols != preserved.Cols || _grid.Rows != preserved.Rows)
            {
                _grid = preserved;
                _canvas.Grid = _grid;
                SyncResolutionCombo();
            }
            else
            {
                Array.Copy(preserved.A, _grid.A, Math.Min(preserved.A.Length, _grid.A.Length));
            }
            _canvas.Invalidate();
            SetStatus($"Restored the preserved painted canvas for {param} on '{target.Name}' at {_grid.Cols} × {_grid.Rows} ({lane.Points.Count} lane point(s)).{wiredNote}");
            return;
        }

        if (lane != null && lane.Points.Count > 0)
        {
            bool isLog = param is "p:Cutoff" or "f:Cutoff" or "lp" or "hp";
            double range = max - min;
            double[]? previous = null;
            foreach (var point in lane.Points.OrderBy(p => p[0]))
            {
                double cx = point[0] / beats * _grid.Cols;
                double frac;
                if (isLog && point[1] > 0 && min > 0) frac = Math.Log(point[1] / min) / Math.Log(max / min);
                else frac = range != 0 ? (point[1] - min) / range : 0;
                frac = Math.Max(0, Math.Min(1, frac));
                double cy = frac * (_grid.Rows - 1);
                double strength = point.Length >= 3 ? Math.Clamp(point[2], 0.12, 1) : 0.85;

                // Reconstruct the painted vertical run as well as the centre line, so reopening
                // a lane shows the same stroke the user drew rather than one thin line.
                if (point.Length >= 5)
                {
                    double lowY = Math.Clamp(point[3], 0, 1) * (_grid.Rows - 1);
                    double highY = Math.Clamp(point[4], 0, 1) * (_grid.Rows - 1);
                    if (highY - lowY > 0.4) _grid.Line(cx, lowY, cx, highY, 0.7, strength, false);
                }

                if (previous != null) _grid.Line(previous[0], previous[1], cx, cy, 1.0, strength, false);
                else _grid.Dab(cx, cy, 1.0, strength, false);
                previous = new[] { cx, cy };
            }
            SetStatus($"Showing the saved {param} curve on '{target.Name}' ({lane.Points.Count} point(s)). Paint to replace it.{wiredNote}");
        }
        else
        {
            string saved = target.Lanes.Count > 0 ? " Saved lanes: " + string.Join(", ", target.Lanes.Select(l => l.Param)) + "." : " This target has no automation lanes yet.";
            SetStatus($"No saved {param} curve on '{target.Name}'.{saved}{wiredNote}");
        }
        _canvas.Invalidate();
    }

    private double CanvasBeats()
    {
        double beats = 0;
        try { beats = Arranger.SongBeats(_host.Project); } catch { }
        if (beats <= 0) beats = _host.Project.LoopBeats;
        return beats > 0 ? beats : 1;
    }

    private void ApplyPainting()
    {
        var target = GetTarget();
        if (target == null) { SetStatus("Select a track or graph module target first."); return; }
        var (param, min, max) = ParamInfo();
        if (target.Kind != "track" && param.StartsWith("p:"))
        {
            SetStatus("Synth patch parameters only apply to track targets. Pick a Track FX module for a node or bus target.");
            return;
        }

        double loop = CanvasBeats();
        string mapMode = _map.SelectedItem as string ?? "Smooth curve";
        string zMode = _z.SelectedItem as string ?? "Depth (scale value)";
        double zAmt = (double)_zDepth.Value;
        double neutral = Neutral(target, param, min);

        string detail = _detail.SelectedItem as string ?? "Curve (single value)";
        var points = AutomationPaint.Paint(_grid, param, min, max, loop, mapMode, zMode, zAmt, neutral, detail);

        if (points.Count == 0)
        {
            SetStatus($"Nothing painted into {param}: the automation canvas is empty. Paint on it first, then click Write to Lane. (peak {_grid.Peak():0.000})");
            return;
        }

        _host.BeginEdit("automation paint");
        if (param.StartsWith("f:") && target.Fx != null && _module.SelectedItem is string moduleName)
            EnableModule(target.Fx, moduleName);

        var lane = target.Lanes.FirstOrDefault(l => l.Param == param);
        if (lane == null)
        {
            lane = new AutoLane(target.TrackIndex, param);
            target.Lanes.Add(lane);
        }
        lane.Points.Clear();
        lane.Points.AddRange(points);
        lane.Preview = AutomationPreview.Encode(_grid);
        lane.PreviewCols = _grid.Cols;
        lane.PreviewRows = _grid.Rows;

        _host.MarkDirty();
        double bars = _host.Project.BarBeats > 0 ? loop / _host.Project.BarBeats : 0;
        SetStatus($"Painted {points.Count} point(s) over {bars:0.##} bar(s) into {param} on '{target.Name}'.");
        _canvas.Invalidate();
    }

    // ---------------------------------------------------------------- layers

    private void ExtractLayers()
    {
        bool fromStrokes = _separateStrokes.Checked && _canvas.Strokes.Count > 0;
        var found = fromStrokes
            ? _canvas.Strokes.Select(s => s.Clone()).ToList()
            : AutomationLayers.Extract(_grid, 0.015, 8);

        if (found.Count == 0)
        {
            SetStatus(fromStrokes
                ? "No strokes captured yet. Paint a wave first."
                : "No separate painted regions found. Paint two or more waves that do not touch, then extract.");
            return;
        }

        string param = ParamInfo().Param;
        foreach (var layer in found) layer.Param = param;

        _layersData = found;
        RefreshLayerList();
        _canvas.Layers = _layersData;
        _canvas.ShowLayers = _showLayers.Checked;
        _canvas.Invalidate();
        SetStatus(fromStrokes
            ? $"Found {found.Count} stroke layer(s). Crossing strokes stay separate; assign parameters and write."
            : $"Found {found.Count} painted region(s). Assign a parameter to each and click Write layers.");
    }

    private static (int Cols, int Rows) ResolutionSize(int index) => index switch
    {
        0 => (512, 128),
        1 => (1024, 256),
        2 => (2048, 512),
        _ => (4096, 512)
    };

    private void SyncResolutionCombo()
    {
        for (int i = 0; i < 4; i++)
        {
            var (cols, rows) = ResolutionSize(i);
            if (cols != _grid.Cols || rows != _grid.Rows) continue;
            _updating = true;
            try { _resolution.SelectedIndex = i; }
            finally { _updating = false; }
            return;
        }
    }

    private void ChangeResolution()
    {
        if (_updating) return;
        var (cols, rows) = ResolutionSize(_resolution.SelectedIndex);
        if (cols == _grid.Cols && rows == _grid.Rows) return;

        _grid = _grid.Resample(cols, rows);
        _canvas.Grid = _grid;
        _canvas.ClearStrokes();
        _layersData.Clear();
        _layers.Items.Clear();
        _canvas.Layers = null;
        _canvas.Invalidate();
        SetStatus($"Canvas {cols} × {rows}. Playback lookup stays logarithmic, so lane density does not slow it down.");
    }

    private void RefreshLayerList()
    {
        _updatingLayers = true;
        try
        {
            int selected = _layers.SelectedIndex;
            _layers.Items.Clear();
            foreach (var layer in _layersData)
                _layers.Items.Add($"{layer.Name}  amp {layer.Amplitude:0.00}  {layer.Param}");
            if (_layers.Items.Count > 0)
                _layers.SelectedIndex = selected >= 0 && selected < _layers.Items.Count ? selected : 0;
        }
        finally
        {
            _updatingLayers = false;
        }
        UpdateLayerParamCombo();
    }

    private void UpdateLayerParamCombo()
    {
        int index = _layers.SelectedIndex;
        _updatingLayers = true;
        try
        {
            if (index < 0 || index >= _layersData.Count)
            {
                _layerParam.Enabled = false;
                _layerParam.SelectedIndex = -1;
                return;
            }

            _layerParam.Enabled = true;
            string param = _layersData[index].Param;
            int option = _paramOptions.FindIndex(o => o.Param == param);
            if (option < 0)
            {
                _paramOptions.Add((param, param));
                _layerParam.Items.Add(param);
                option = _paramOptions.Count - 1;
            }
            _layerParam.SelectedIndex = option;
        }
        finally
        {
            _updatingLayers = false;
        }
    }

    private void AssignLayerParam()
    {
        if (_updatingLayers) return;
        int index = _layers.SelectedIndex;
        int option = _layerParam.SelectedIndex;
        if (index < 0 || index >= _layersData.Count || option < 0 || option >= _paramOptions.Count) return;

        _layersData[index].Param = _paramOptions[option].Param;
        _updatingLayers = true;
        try { _layers.Items[index] = $"{_layersData[index].Name}  amp {_layersData[index].Amplitude:0.00}  {_layersData[index].Param}"; }
        finally { _updatingLayers = false; }
        SetStatus($"{_layersData[index].Name} -> {_paramOptions[option].Label}.");
    }

    private void ClearLayers()
    {
        _layersData.Clear();
        _layers.Items.Clear();
        _canvas.Layers = null;
        _canvas.Invalidate();
        SetStatus("Cleared extracted layers (the painting is kept).");
    }

    private void WriteLayers()
    {
        if (_layersData.Count == 0) { SetStatus("Extract layers first."); return; }
        var target = GetTarget();
        if (target == null) { SetStatus("Select a track or graph module target first."); return; }

        double loop = CanvasBeats();
        string mapMode = _map.SelectedItem as string ?? "Exact shape";
        string detail = _detail.SelectedItem as string ?? "Curve (single value)";
        double gain = (double)_layerGain.Value;
        bool normalize = _normalize.Checked;
        string mix = _mix.SelectedItem as string ?? "Add";

        bool begun = false;
        int lanes = 0, points = 0, skipped = 0;

        foreach (var group in _layersData.GroupBy(l => l.Param))
        {
            string param = group.Key;
            if (target.Kind != "track" && param.StartsWith("p:")) { skipped++; continue; }

            var groupLayers = group.ToList();
            var (min, max) = RangeOfParam(param);
            double neutral = Neutral(target, param, min);
            var lanePoints = AutomationLayers.Combine(groupLayers, param, min, max, loop, mapMode,
                neutral, detail, normalize, gain, mix);
            if (lanePoints.Count == 0) continue;

            if (!begun) { _host.BeginEdit("write automation layers"); begun = true; }
            if (param.StartsWith("f:") && target.Fx != null)
            {
                string? module = ModuleForParam(param);
                if (module != null) EnableModule(target.Fx, module);
            }

            var lane = target.Lanes.FirstOrDefault(l => l.Param == param);
            if (lane == null)
            {
                lane = new AutoLane(target.TrackIndex, param);
                target.Lanes.Add(lane);
            }
            lane.Points.Clear();
            lane.Points.AddRange(lanePoints);
            int previewCols = groupLayers[0].Cols;
            int previewRows = groupLayers[0].Rows;
            lane.Preview = AutomationPreview.EncodeLayers(groupLayers, previewCols, previewRows);
            lane.PreviewCols = previewCols;
            lane.PreviewRows = previewRows;
            lanes++;
            points += lanePoints.Count;
        }

        if (lanes == 0)
        {
            SetStatus(skipped > 0
                ? "Nothing written: synth patch layers need a track target."
                : "No layer data to write.");
            return;
        }

        _host.MarkDirty();
        SetStatus($"Wrote {points} point(s) from {_layersData.Count} layer(s) into {lanes} lane(s) on '{target.Name}'."
                  + (skipped > 0 ? " Skipped patch layers on a non-track target." : ""));
        _canvas.Invalidate();
    }

    private static (double Min, double Max) RangeOfParam(string param)
    {
        foreach (var value in ParamMap.Values)
            if (value.Param == param) return (value.Min, value.Max);
        return param switch
        {
            "vol" => (0, 2),
            "pan" => (-1, 1),
            "lp" or "hp" => (20, 20000),
            _ => (0, 1)
        };
    }

    private static string? ModuleForParam(string param)
    {
        foreach (var module in Modules)
            foreach (string name in module.Value)
                if (ParamMap.TryGetValue(module.Key + "|" + name, out var info) && info.Param == param)
                    return module.Key;
        return null;
    }

    private void ClearLane()
    {
        var target = GetTarget();
        if (target == null) { SetStatus("Select a track or graph module target first."); return; }
        var (param, _, _) = ParamInfo();
        int count = target.Lanes.Count(l => l.Param == param);
        if (count == 0) { SetStatus($"No {param} lane on '{target.Name}'."); return; }
        _host.BeginEdit("clear automation lane");
        target.Lanes.RemoveAll(l => l.Param == param);
        _host.MarkDirty();
        SetStatus($"Removed {count} lane(s) for {param} on '{target.Name}'.");
        SyncCanvasFromLane();
    }

    private void ResetAutomation()
    {
        var target = GetTarget();
        if (target == null) { SetStatus("Select a track or graph module target first."); return; }
        int count = target.Lanes.Count;
        if (count > 0)
        {
            _host.BeginEdit("reset automation");
            target.Lanes.Clear();
            _host.MarkDirty();
        }
        _grid.Clear();
        _canvas.ClearStrokes();
        _canvas.Invalidate();
        SetStatus($"Reset automation on '{target.Name}' ({count} lane(s) removed, canvas cleared).");
    }

    private static void EnableModule(Fx fx, string module)
    {
        switch (module)
        {
            case "Track FX Filter":
                fx.Filter = FilterMode.lowpass;
                if (fx.Cutoff > 8000) fx.Cutoff = 1200;
                fx.Resonance = Math.Max(fx.Resonance, 0.4);
                break;
            case "Track FX Drive/Crush":
                fx.Drive = Math.Max(fx.Drive, 0.35);
                fx.Bits = Math.Min(fx.Bits, 6);
                fx.Downsample = Math.Max(fx.Downsample, 2);
                break;
            case "Track FX Ring":
                fx.RingMix = Math.Max(fx.RingMix, 0.5);
                fx.RingFreq = 200;
                break;
            case "Track FX Chorus": fx.Chorus = Math.Max(fx.Chorus, 0.35); break;
            case "Track FX Delay": fx.DelayMix = Math.Max(fx.DelayMix, 0.3); break;
            case "Track FX Reverb": fx.ReverbMix = Math.Max(fx.ReverbMix, 0.3); break;
            case "Track FX Phaser/Flanger":
                fx.PhaserMix = Math.Max(fx.PhaserMix, 0.4);
                fx.FlangerMix = Math.Max(fx.FlangerMix, 0.4);
                break;
            case "Track FX Tape": fx.TapeDelayMix = Math.Max(fx.TapeDelayMix, 0.3); break;
            case "Track FX Plate/Vinyl": fx.PlateMix = Math.Max(fx.PlateMix, 0.35); break;
            case "Track FX Compressor":
                fx.CompThresh = Math.Min(fx.CompThresh, -18);
                fx.CompRatio = Math.Max(fx.CompRatio, 3);
                break;
            case "Track FX EQ/Width":
                fx.LowShelf = 2;
                fx.HighShelf = 2;
                break;
        }
    }

    private void SetStatus(string text)
    {
        _status.Text = text;
        _host.SetStatus(text);
    }

    public void RefreshView()
    {
        RefreshTargets(keepSelection: true);
        UpdateParams();
        SyncCanvasFromLane();
    }
}

/// <summary>The automation canvas. It paints density into a SpectralGrid so the same bright/dim Z axis works.</summary>
internal sealed class AutoCanvas : Control
{
    public SpectralGrid? Grid;
    public int BrushKind;
    public double Brush = 12;                 // screen pixels, independent of canvas resolution
    public List<AutoLayer>? Layers;
    public bool ShowLayers = true;
    public bool CaptureStrokes = true;        // keeps crossing strokes separate
    public List<AutoLayer> Strokes = new();
    public event Action? StrokeCompleted;

    private bool _dragging;
    private double _lastX, _lastY;
    private float[]? _strokeData;
    private SpectralGrid? _strokeGrid;

    public AutoCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(12, 14, 18);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        if (Grid == null) return;
        int w = Math.Max(8, Width - 2), h = Math.Max(8, Height - 2);
        using (var b = RenderGrid(w, h))
        {
            e.Graphics.DrawImageUnscaled(b, 1, 1);
        }

        // Extracted layers (or the live strokes before extraction) are drawn as coloured splines.
        var overlay = Layers != null && Layers.Count > 0 ? Layers : (CaptureStrokes ? Strokes : null);
        if (ShowLayers && overlay != null && overlay.Count > 0)
        {
            Color[] palette =
            {
                Color.FromArgb(230, 90, 205, 255), Color.FromArgb(230, 255, 165, 80),
                Color.FromArgb(230, 120, 240, 140), Color.FromArgb(230, 255, 110, 190),
                Color.FromArgb(230, 245, 225, 90), Color.FromArgb(230, 190, 150, 255),
                Color.FromArgb(230, 255, 110, 110), Color.FromArgb(230, 90, 230, 220)
            };
            for (int i = 0; i < overlay.Count; i++)
            {
                var layer = overlay[i];
                if (layer.Contour.Count < 2) continue;
                var points = new PointF[layer.Contour.Count];
                for (int k = 0; k < points.Length; k++)
                {
                    double px = layer.Contour[k][0], py = layer.Contour[k][1];
                    points[k] = new PointF((float)(1 + px * (w - 1)), (float)(1 + (1 - py) * (h - 1)));
                }
                using var pen = new Pen(palette[i % palette.Length], 1.6f);
                e.Graphics.DrawLines(pen, points);
            }
        }

        using var guide = new Pen(Color.FromArgb(70, 255, 255, 255));
        foreach (double frac in new[] { 0.25, 0.5, 0.75 })
        {
            double y = h - 1 - frac * (h - 1);
            e.Graphics.DrawLine(guide, 0, (float)y, 10, (float)y);
        }

        using var border = new Pen(Color.FromArgb(50, 56, 70));
        e.Graphics.DrawRectangle(border, 0, 0, w + 1, h + 1);
    }

    /// <summary>
    /// Draws the grid at control resolution. Cost is independent of the stored canvas
    /// resolution, so a 4096-column automation lane still paints smoothly.
    /// </summary>
    private Bitmap RenderGrid(int w, int h)
    {
        var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        if (Grid == null) return bmp;
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
        var buf = new byte[data.Stride * h];
        int cols = Grid.Cols, rows = Grid.Rows;

        for (int y = 0; y < h; y++)
        {
            int r = (int)Math.Round((1 - (y + 0.5) / h) * (rows - 1));
            if (r < 0) r = 0; else if (r >= rows) r = rows - 1;
            int rowOffset = y * data.Stride;
            for (int x = 0; x < w; x++)
            {
                int c = (int)Math.Round((x + 0.5) / w * (cols - 1));
                if (c < 0) c = 0; else if (c >= cols) c = cols - 1;

                double v = Math.Max(0, Math.Min(1, Grid.A[c * rows + r]));
                int R = (int)(255 * Math.Min(1, v * 2.2));
                int G = (int)(255 * Math.Max(0, Math.Min(1, v * 2.2 - 0.8)));
                int B2 = (int)(200 * Math.Max(0, 1 - v * 1.5) + 255 * Math.Max(0, v - 0.85));
                int rr = Math.Min(255, R / 2 + (int)(B2 * 0.2));
                int bb = Math.Min(255, B2 + R / 4);
                int i = rowOffset + x * 4;
                buf[i] = (byte)Math.Max(0, Math.Min(255, bb));
                buf[i + 1] = (byte)Math.Max(0, Math.Min(255, G));
                buf[i + 2] = (byte)Math.Max(0, Math.Min(255, rr));
                buf[i + 3] = 255;
            }
        }

        System.Runtime.InteropServices.Marshal.Copy(buf, 0, data.Scan0, buf.Length);
        bmp.UnlockBits(data);
        return bmp;
    }

    private (double X, double Y) Cell(Point p)
    {
        if (Grid == null) return (0, 0);
        double x = (p.X - 1) / (double)Math.Max(1, Width - 2) * Grid.Cols;
        double y = (Grid.Rows - 1) - (p.Y - 1) / (double)Math.Max(1, Height - 2) * (Grid.Rows - 1);
        return (Math.Max(0, Math.Min(Grid.Cols, x)), Math.Max(0, Math.Min(Grid.Rows - 1, y)));
    }

    private (double Rx, double Ry) Radii()
    {
        if (Grid == null) return (1, 1);
        double rx = Brush * Grid.Cols / Math.Max(1, Width);
        double ry = Brush * Grid.Rows / Math.Max(1, Height);
        return (Math.Max(0.6, rx), Math.Max(0.6, ry));
    }

    private void DabAt(SpectralGrid g, double x, double y, double rx, double ry)
    {
        switch (BrushKind)
        {
            case 0: g.DabEllipse(x, y, rx, ry, 0.9, false); break;
            case 1: g.DabEllipse(x, y, rx * 1.6, ry * 1.6, 0.5, false); break;
            case 2: g.DabEllipse(x, y, rx, ry, 0.7, false); break;
            case 3: g.LineEllipse(x, y, x, y, rx, ry, 0.9, false); break;
            case 4: g.DabEllipse(x, y, rx * 2, ry * 2, 0.35, false); break;
            case 5: g.Smear(0.9); g.DabEllipse(x, y, rx, ry, 0.6, false); break;
            default: g.Smear(0.93); g.DabEllipse(x, y, rx, ry, 0.8, false); break;
        }
    }

    private void StrokeAt(SpectralGrid g, double x0, double y0, double x1, double y1, double rx, double ry)
    {
        switch (BrushKind)
        {
            case 0: g.LineEllipse(x0, y0, x1, y1, rx, ry, 0.9, false); break;
            case 1: g.LineEllipse(x0, y0, x1, y1, rx * 1.6, ry * 1.6, 0.5, false); break;
            case 2: g.LineEllipse(x0, y0, x1, y1, rx, ry, 0.7, false); break;
            case 3: g.LineEllipse(x0, y0, x1, y1, rx, ry, 0.9, false); break;
            case 4: g.LineEllipse(x0, y0, x1, y1, rx * 2, ry * 2, 0.35, false); break;
            case 5: g.Smear(0.9); g.LineEllipse(x0, y0, x1, y1, rx, ry, 0.6, false); break;
            default: g.Smear(0.93); g.LineEllipse(x0, y0, x1, y1, rx, ry, 0.8, false); break;
        }
    }

    private void Dab(double x, double y)
    {
        if (Grid == null) return;
        var (rx, ry) = Radii();
        DabAt(Grid, x, y, rx, ry);
        if (_strokeGrid != null) DabAt(_strokeGrid, x, y, rx, ry);
    }

    private void Stroke(double x0, double y0, double x1, double y1)
    {
        if (Grid == null) return;
        var (rx, ry) = Radii();
        StrokeAt(Grid, x0, y0, x1, y1, rx, ry);
        if (_strokeGrid != null) StrokeAt(_strokeGrid, x0, y0, x1, y1, rx, ry);
    }

    public void ClearStrokes()
    {
        Strokes.Clear();
        _strokeGrid = null;
        _strokeData = null;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Grid == null || e.Button != MouseButtons.Left) return;
        _dragging = true;
        if (CaptureStrokes)
        {
            _strokeData = new float[Grid.A.Length];
            _strokeGrid = new SpectralGrid(Grid.Cols, Grid.Rows);
            _strokeGrid.A = _strokeData;
        }
        var (x, y) = Cell(e.Location);
        _lastX = x; _lastY = y;
        Dab(x, y);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging || Grid == null) return;
        var (x, y) = Cell(e.Location);
        Stroke(_lastX, _lastY, x, y);
        _lastX = x; _lastY = y;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_strokeGrid != null && Grid != null)
        {
            var layer = AutomationLayers.FromData(_strokeGrid.A, Grid.Cols, Grid.Rows);
            if (layer != null)
            {
                layer.Name = "Stroke " + (Strokes.Count + 1);
                Strokes.Add(layer);
            }
            _strokeGrid = null;
            _strokeData = null;
            StrokeCompleted?.Invoke();
        }
        _dragging = false;
        Invalidate();
    }
}
