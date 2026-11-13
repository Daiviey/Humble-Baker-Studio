using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

internal sealed class TracksTab : UserControl
{
    private readonly MainForm _host;
    private readonly FlowLayoutPanel _rows = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(14) };
    private readonly Label _heading = new() { AutoSize = true, Font = Theme.Title, ForeColor = Theme.Text, Margin = new Padding(0, 0, 0, 10) };

    private readonly TextBox _name = new() { Width = 240 };
    private readonly ComboBox _synth = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _kit = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _bus = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _vol = Num(0, 2, 2, 0.01m);
    private readonly NumericUpDown _pan = Num(-1, 1, 2, 0.05m);
    private readonly NumericUpDown _cutoff = Num(30, 16000, 0, 10);
    private readonly NumericUpDown _res = Num(0, 0.95m, 2, 0.01m);
    private readonly NumericUpDown _env = Num(-1, 1, 2, 0.01m);
    private readonly NumericUpDown _atk = Num(0.001m, 5, 3, 0.01m);
    private readonly NumericUpDown _rel = Num(0.005m, 8, 3, 0.01m);
    private readonly NumericUpDown _crush = Num(2, 16, 0, 1);
    private readonly ComboBox _sampleMode = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _sampleGate = new() { Text = "Gate by note length (apply release)", AutoSize = true, ForeColor = Theme.Text };
    private readonly Label _stats = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private List<InstrumentOption> _options = new();
    private bool _syncing;

    public TracksTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;
        _rows.Controls.Add(_heading);

        _kit.Items.AddRange(SampleLibrary.BuiltInKits);
        ReloadInstruments();

        _rows.Controls.Add(Section("IDENTITY"));
        _rows.Controls.Add(Field("Name", _name));
        _rows.Controls.Add(Field("Engine", _synth));
        _rows.Controls.Add(Field("Drum kit", _kit));
        _rows.Controls.Add(Field("Bus", _bus));
        _rows.Controls.Add(Section("LEVEL AND FILTER"));
        _rows.Controls.Add(Field("Volume (0-2)", _vol));
        _rows.Controls.Add(Field("Pan (-1 left .. 1 right)", _pan));
        _rows.Controls.Add(Field("Cutoff Hz", _cutoff));
        _rows.Controls.Add(Field("Resonance", _res));
        _rows.Controls.Add(Field("Envelope amount", _env));
        _rows.Controls.Add(Section("SHAPE"));
        _rows.Controls.Add(Field("Attack (s)", _atk));
        _rows.Controls.Add(Field("Release (s)", _rel));
        _rows.Controls.Add(Field("Bit crush (16 = clean)", _crush));
        _rows.Controls.Add(Section("SAMPLE ENVELOPE"));
        _sampleMode.Items.AddRange(new[] { "Pitch (key)", "One-shot", "Loop" });
        _sampleMode.SelectedIndex = 0;
        _rows.Controls.Add(Field("Sample mode", _sampleMode));
        _rows.Controls.Add(Field("Gate by note length", _sampleGate));
        _rows.Controls.Add(Section("STATE"));
        _rows.Controls.Add(_stats);
        _rows.Controls.Add(Theme.Label("Synth patches and the full effects chain live on the Synth and Effects tabs.", Theme.Muted, Theme.Small));

        Controls.Add(_rows);

        _name.TextChanged += (_, _) => Apply(t => t.Name = _name.Text.Trim());
        _synth.SelectedIndexChanged += (_, _) =>
        {
            if (_syncing || _host.SelectedTrack == null) return;
            if (_synth.SelectedItem is not InstrumentOption option) return;
            var t = _host.SelectedTrack;
            _host.BeginEdit("instrument");
            InstrumentCatalog.Apply(t, option, _host.Vst);
            if (t.Synth == "patch" && t.Patch == null) t.Patch = new Patch();
            _kit.Enabled = option.Vst == null && t.Synth == "drums";
            _host.MarkDirty();
            _host.NotifyTracksChanged(t);
        };
        _kit.SelectedIndexChanged += (_, _) => Apply(t => t.DrumKit = _kit.SelectedItem as string ?? t.DrumKit);
        _bus.SelectedIndexChanged += (_, _) => Apply(t => t.Bus = Math.Max(0, _bus.SelectedIndex));
        _vol.ValueChanged += (_, _) => Apply(t => t.Vol = (double)_vol.Value);
        _pan.ValueChanged += (_, _) => Apply(t => t.Pan = (double)_pan.Value);
        _cutoff.ValueChanged += (_, _) => Apply(t => t.Cutoff = (double)_cutoff.Value);
        _res.ValueChanged += (_, _) => Apply(t => t.Res = (double)_res.Value);
        _env.ValueChanged += (_, _) => Apply(t => t.EnvAmt = (double)_env.Value);
        _atk.ValueChanged += (_, _) => Apply(t => t.Atk = (double)_atk.Value);
        _rel.ValueChanged += (_, _) => Apply(t => t.Rel = (double)_rel.Value);
        _crush.ValueChanged += (_, _) => Apply(t => t.Crush = (double)_crush.Value);
        _sampleMode.SelectedIndexChanged += (_, _) => { if (!_syncing) Apply(t => t.SampleMode = _sampleMode.SelectedIndex); };
        _sampleGate.CheckedChanged += (_, _) => { if (!_syncing) Apply(t => t.SampleGate = _sampleGate.Checked); };
    }

    private static NumericUpDown Num(decimal min, decimal max, int decimals, decimal step) => new()
    {
        Minimum = min,
        Maximum = max,
        DecimalPlaces = decimals,
        Increment = step,
        Width = 240
    };

    private static Label Section(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.Accent,
        Font = Theme.UiBold,
        Margin = new Padding(0, 14, 0, 4)
    };

    private static Control Field(string label, Control editor)
    {
        var p = new Panel { Width = 480, Height = 30, Margin = new Padding(0, 1, 0, 1), BackColor = Theme.Back };
        var l = new Label { Text = label, Width = 210, Height = 26, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Muted };
        editor.Left = 214;
        editor.Top = 2;
        p.Controls.Add(l);
        p.Controls.Add(editor);
        return p;
    }

    public void RefreshView()
    {
        var t = _host.SelectedTrack;
        _syncing = true;
        try
        {
            _heading.Text = t == null ? "No track selected" : t.Name + " — track settings";
            _name.Enabled = _synth.Enabled = _kit.Enabled = _bus.Enabled = t != null;
            foreach (var c in new Control[] { _vol, _pan, _cutoff, _res, _env, _atk, _rel, _crush, _sampleMode, _sampleGate }) c.Enabled = t != null;
            if (t == null) { _stats.Text = ""; return; }

            _name.Text = t.Name;
            _synth.SelectedItem = InstrumentCatalog.Find(t, _options);
            _kit.SelectedItem = t.DrumKit;
            _kit.Enabled = string.IsNullOrEmpty(t.PluginPath) && t.Synth == "drums";
            _bus.Items.Clear();
            _bus.Items.Add("Direct to master");
            for (int i = 0; i < _host.Project.Buses.Count; i++) _bus.Items.Add(_host.Project.Buses[i].Name);
            _bus.SelectedIndex = Math.Clamp(t.Bus, 0, _bus.Items.Count - 1);
            _vol.Value = Clamp((decimal)t.Vol, _vol);
            _pan.Value = Clamp((decimal)t.Pan, _pan);
            _cutoff.Value = Clamp((decimal)t.Cutoff, _cutoff);
            _res.Value = Clamp((decimal)t.Res, _res);
            _env.Value = Clamp((decimal)t.EnvAmt, _env);
            _atk.Value = Clamp((decimal)t.Atk, _atk);
            _rel.Value = Clamp((decimal)t.Rel, _rel);
            _crush.Value = Clamp((decimal)t.Crush, _crush);
            _sampleMode.SelectedIndex = Math.Clamp(t.SampleMode, 0, _sampleMode.Items.Count - 1);
            _sampleGate.Checked = t.SampleGate;

            _stats.Text =
                $"Notes: {t.Notes.Count}\n" +
                $"Automation lanes: {t.Auto.Count}\n" +
                $"Effects active: {(t.Fx.Active ? "yes" : "no")}\n" +
                $"Sample loaded: {(t.Sample != null ? t.Sample.Length + " frames" : "no")}\n" +
                $"VST3: {(string.IsNullOrEmpty(t.PluginPath) ? "none" : t.PluginName + (t.PluginInstrument ? " (instrument)" : " (effect)"))}\n" +
                $"Uid: {t.Uid}";
        }
        finally
        {
            _syncing = false;
        }
    }

    public void ReloadInstruments()
    {
        _syncing = true;
        try
        {
            _options = InstrumentCatalog.Build(_host.Vst);
            _synth.Items.Clear();
            _synth.Items.AddRange(_options.ToArray());
            var t = _host.SelectedTrack;
            if (t != null) _synth.SelectedItem = InstrumentCatalog.Find(t, _options);
        }
        finally
        {
            _syncing = false;
        }
    }

    private static decimal Clamp(decimal v, NumericUpDown n) => Math.Max(n.Minimum, Math.Min(n.Maximum, v));

    private void Apply(Action<Track> change)
    {
        if (_syncing || _host.SelectedTrack == null) return;
        _host.BeginEdit("track");
        change(_host.SelectedTrack);
        _host.MarkDirty();
        _host.NotifyTracksChanged();
    }
}
