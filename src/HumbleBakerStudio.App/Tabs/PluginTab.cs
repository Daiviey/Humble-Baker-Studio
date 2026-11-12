using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

/// <summary>VST3 plug-in browser and track insert. Effects and instruments share one slot per track.</summary>
internal sealed class PluginTab : UserControl
{
    private readonly MainForm _host;
    private readonly TextBox _search = new() { Width = 210, PlaceholderText = "Search plug-ins" };
    private readonly ComboBox _filter = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ListView _list = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = false,
        HideSelection = false,
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Back,
        ForeColor = Theme.Text
    };
    private readonly Label _status = new()
    {
        Dock = DockStyle.Bottom,
        Height = 26,
        Padding = new Padding(10, 5, 4, 0),
        ForeColor = Theme.Muted,
        Font = Theme.Small,
        BackColor = Theme.Panel
    };
    private readonly Label _current = new()
    {
        AutoSize = true,
        Padding = new Padding(0, 0, 0, 0),
        ForeColor = Theme.Text,
        Font = Theme.UiBold,
        BackColor = Theme.Panel,
        Margin = new Padding(0, 8, 22, 0)
    };
    private readonly CheckBox _bypass = new() { Text = "Bypass plug-in", AutoSize = true, ForeColor = Theme.Text };
    private bool _scanning;
    private bool _scanned;

    public PluginTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Theme.Panel,
            Padding = new Padding(10, 8, 6, 4),
            WrapContents = false
        };
        bar.Controls.Add(_search);
        _filter.Items.AddRange(new object[] { "All plug-ins", "Instruments", "Effects" });
        _filter.SelectedIndex = 0;
        bar.Controls.Add(_filter);
        bar.Controls.Add(Theme.Button("Scan VST3", (_, _) => Scan(), true, "Scan the standard VST3 folders"));
        bar.Controls.Add(Theme.Button("Set as instrument", (_, _) => Assign(instrument: true), false, "Use the selected plug-in as this track's instrument"));
        bar.Controls.Add(Theme.Button("Set as effect", (_, _) => Assign(instrument: false), false, "Insert the selected effect after this track's FX chain"));
        bar.Controls.Add(Theme.Button("Open editor", (_, _) => OpenEditor()));
        bar.Controls.Add(Theme.Button("Remove", (_, _) => RemovePlugin()));

        _list.Columns.Add("Plug-in", 320);
        _list.Columns.Add("Vendor", 150);
        _list.Columns.Add("Format", 70);
        _list.Columns.Add("Type", 90);

        var section = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(12, 7, 0, 0),
            Text = "VST3 PLUG-INS",
            ForeColor = Theme.Accent,
            Font = Theme.Section,
            BackColor = Theme.Panel
        };

        Controls.Add(_list);
        Controls.Add(_status);
        var currentRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(12, 2, 0, 0),
            WrapContents = false,
            BackColor = Theme.Panel
        };
        _bypass.Margin = new Padding(0, 8, 0, 0);
        currentRow.Controls.Add(_current);
        currentRow.Controls.Add(_bypass);
        Controls.Add(currentRow);
        Controls.Add(bar);
        Controls.Add(section);

        _search.TextChanged += (_, _) => ApplyFilter();
        _filter.SelectedIndexChanged += (_, _) => ApplyFilter();
        _list.DoubleClick += (_, _) => OpenEditor();
        _bypass.CheckedChanged += (_, _) =>
        {
            var track = _host.SelectedTrack;
            if (track == null || _syncingBypass) return;
            _host.BeginEdit("vst bypass");
            track.PluginBypass = _bypass.Checked;
            _host.MarkDirty();
            RefreshCurrent();
        };
    }

    private bool _syncingBypass;

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) return;
        RefreshCurrent();
        if (!_scanned && !_scanning) Scan();
        else ApplyFilter();
    }

    public void RefreshView()
    {
        RefreshCurrent();
        if (Visible) ApplyFilter();
    }

    private void Scan()
    {
        if (_scanning) return;
        _scanning = true;
        _status.Text = "Scanning VST3 plug-ins...";
        _host.EnsureVstScan().ContinueWith(t =>
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                _scanning = false;
                _scanned = true;
                ApplyFilter();
                _status.Text = t.Exception == null
                    ? $"Found {_host.Vst.Catalog.Count} VST3 plug-ins."
                    : "Scan failed: " + t.Exception.GetBaseException().Message;
            }));
        });
    }

    private void ApplyFilter()
    {
        var all = _host.Vst.Catalog;
        string text = _search.Text.Trim();
        int mode = _filter.SelectedIndex;
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var p in all)
            {
                if (mode == 1 && !p.IsInstrument) continue;
                if (mode == 2 && p.IsInstrument) continue;
                if (text.Length > 0 &&
                    p.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0 &&
                    p.Vendor.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var item = new ListViewItem(p.Name) { Tag = p };
                item.SubItems.Add(p.Vendor);
                item.SubItems.Add(p.Format);
                item.SubItems.Add(p.IsInstrument ? "instrument" : "effect");
                _list.Items.Add(item);
            }
        }
        finally
        {
            _list.EndUpdate();
        }
        if (!_scanning)
            _status.Text = $"{_list.Items.Count} of {all.Count} VST3 plug-ins · 64-bit only";
    }

    private void RefreshCurrent()
    {
        var track = _host.SelectedTrack;
        if (track == null)
        {
            _current.Text = "No track selected";
            _bypass.Enabled = false;
            return;
        }

        bool has = !string.IsNullOrEmpty(track.PluginPath);
        _current.Text = has
            ? $"{track.Name}  ·  {(track.PluginInstrument ? "instrument" : "effect")}: {track.PluginName}"
            : $"{track.Name}  ·  no VST3 plug-in";
        _syncingBypass = true;
        _bypass.Checked = track.PluginBypass;
        _bypass.Enabled = has;
        _syncingBypass = false;
    }

    private void Assign(bool instrument)
    {
        var track = _host.SelectedTrack;
        if (track == null) { _status.Text = "Select a track first."; return; }
        if (_list.SelectedItems.Count == 0 || _list.SelectedItems[0].Tag is not Vst3PluginInfo info)
        {
            _status.Text = "Select a plug-in first.";
            return;
        }
        if (instrument && !info.IsInstrument) { _status.Text = info.Name + " is not an instrument."; return; }
        if (!instrument && info.IsInstrument) { _status.Text = info.Name + " is an instrument; use Set as instrument."; return; }

        _host.BeginEdit("vst assign");
        _host.Vst.Remove(track);
        track.PluginFormat = "vst3";
        track.PluginPath = info.Path;
        track.PluginClass = info.ClassId;
        track.PluginName = info.Name;
        track.PluginInstrument = instrument;
        track.PluginState = "";
        track.PluginBypass = false;
        _host.MarkDirty();
        _host.NotifyTracksChanged(track);
        RefreshCurrent();
        _status.Text = $"{info.Name} set as {(instrument ? "instrument" : "effect")} on {track.Name}.";
    }

    private void OpenEditor()
    {
        var track = _host.SelectedTrack;
        if (track == null || string.IsNullOrEmpty(track.PluginPath))
        {
            _status.Text = "This track has no VST3 plug-in.";
            return;
        }
        var form = _host.Vst.CreateEditor(track);
        if (form == null) { _status.Text = "The plug-in did not provide an editor."; return; }
        form.Show(this);
    }

    private void RemovePlugin()
    {
        var track = _host.SelectedTrack;
        if (track == null || string.IsNullOrEmpty(track.PluginPath)) return;
        _host.BeginEdit("vst remove");
        _host.Vst.Remove(track);
        track.PluginFormat = "";
        track.PluginPath = "";
        track.PluginClass = "";
        track.PluginName = "";
        track.PluginInstrument = false;
        track.PluginState = "";
        track.PluginBypass = false;
        _host.MarkDirty();
        _host.NotifyTracksChanged(track);
        RefreshCurrent();
        _status.Text = "Plug-in removed.";
    }
}
