using System.Drawing.Drawing2D;
using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

/// <summary>Spectral painting and the four-corner meta synth, both feeding the sample engine.</summary>
internal sealed class SpectralTab : UserControl
{
    private readonly MainForm _host;
    private readonly TabControl _inner = new() { Dock = DockStyle.Fill };

    // spectral
    private readonly GridCanvas _canvas = new() { Dock = DockStyle.Fill };
    private readonly SpectralSettings _settings = new();
    private readonly FlowLayoutPanel _spectralKnobs = new() { Dock = DockStyle.Right, Width = 310, AutoScroll = true, BackColor = Theme.Panel, Padding = new Padding(8) };
    private readonly SpectralGrid _grid = new(128, 64);
    private readonly Label _spectralState = new() { AutoSize = true, ForeColor = Theme.Muted };

    // meta
    private readonly Patch[] _slots = { new(), new(), new(), new() };
    private readonly MetaSettings _meta = new();
    private readonly ComboBox _slotChoice = new() { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly FlowLayoutPanel _slotKnobs = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Back, Padding = new Padding(8) };
    private readonly FlowLayoutPanel _metaKnobs = new() { Dock = DockStyle.Right, Width = 300, AutoScroll = true, BackColor = Theme.Panel, Padding = new Padding(8) };
    private readonly Label _metaState = new() { AutoSize = true, ForeColor = Theme.Muted };

    public SpectralTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        _inner.TabPages.Add(SpectralPage());
        _inner.TabPages.Add(MetaPage());
        Theme.StyleTabs(_inner);
        Controls.Add(_inner);

        _canvas.Grid = _grid;
        _slotChoice.Items.AddRange(new object[] { "A", "B", "C", "D" });
        _slotChoice.SelectedIndex = 0;
        _slotChoice.SelectedIndexChanged += (_, _) => BuildSlotKnobs();
        RandomizeSlots();
        BuildSettings();
        BuildSlotKnobs();
    }

    private TabPage SpectralPage()
    {
        var page = new TabPage("Spectral paint") { BackColor = Theme.Back, Padding = new Padding(0) };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Panel, Padding = new Padding(8, 6, 4, 4), WrapContents = false, AutoScroll = true };
        bar.Controls.Add(Theme.Button("Clear", (_, _) => { _grid.Clear(); _canvas.Invalidate(); }, true));
        bar.Controls.Add(Theme.Button("Randomize", (_, _) => RandomizeGrid()));
        bar.Controls.Add(Theme.Button("Smear", (_, _) => { _grid.Smear(0.86); _canvas.Invalidate(); }));
        bar.Controls.Add(Theme.Button("Blur", (_, _) => { _grid.Blur(1, 1); _canvas.Invalidate(); }));
        bar.Controls.Add(Theme.Button("Invert", (_, _) => { _grid.Invert(); _canvas.Invalidate(); }));
        bar.Controls.Add(Theme.Button("Blur freq", (_, _) => { _grid.Blur(0, 2); _canvas.Invalidate(); }));
        bar.Controls.Add(Theme.Button("Shift up", (_, _) => { _grid.ShiftRows(2); _canvas.Invalidate(); }));
        bar.Controls.Add(Theme.Button("Shift down", (_, _) => { _grid.ShiftRows(-2); _canvas.Invalidate(); }));
        bar.Controls.Add(Theme.Button("Harmonics", (_, _) => { _grid.Harmonics(220, 0, _grid.Cols - 1, 6, 0.7, 0.7, _settings.MinHz, _settings.MaxHz); _canvas.Invalidate(); }));
        bar.Controls.Add(Theme.Button("Render to WAV…", (_, _) => RenderSpectral(saveToFile: true), true));
        bar.Controls.Add(Theme.Button("Use as sample track", (_, _) => RenderSpectral(saveToFile: false)));
        bar.Controls.Add(_spectralState);

        page.Controls.Add(_canvas);
        page.Controls.Add(_spectralKnobs);
        page.Controls.Add(bar);
        return page;
    }

    private TabPage MetaPage()
    {
        var page = new TabPage("Meta synth") { BackColor = Theme.Back, Padding = new Padding(0) };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Panel, Padding = new Padding(8, 6, 4, 4), WrapContents = false };
        bar.Controls.Add(Theme.Label("Corner patch", Theme.Muted));
        bar.Controls.Add(_slotChoice);
        bar.Controls.Add(Theme.Button("Randomize corner", (_, _) => RandomizeSlot()));
        bar.Controls.Add(Theme.Button("Randomize all", (_, _) => { RandomizeSlots(); BuildSlotKnobs(); }));
        bar.Controls.Add(Theme.Button("Render to WAV…", (_, _) => RenderMeta(saveToFile: true), true));
        bar.Controls.Add(Theme.Button("Use as sample track", (_, _) => RenderMeta(saveToFile: false)));
        bar.Controls.Add(_metaState);

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(_slotKnobs);
        body.Controls.Add(_metaKnobs);
        page.Controls.Add(body);
        page.Controls.Add(bar);
        return page;
    }

    public void RefreshView() { }

    private void BuildSettings()
    {
        KnobPanel.Build(_settings, _spectralKnobs, (_, _) => { }, Theme.Panel, Theme.Text, Theme.Accent);
        _spectralKnobs.AutoScrollPosition = new Point(0, 0);
        KnobPanel.Build(_meta, _metaKnobs, (_, _) => { }, Theme.Panel, Theme.Text, Theme.Accent);
        _metaKnobs.AutoScrollPosition = new Point(0, 0);
    }

    private void RandomizeGrid()
    {
        var rnd = new Random(Environment.TickCount);
        for (int c = 0; c < _grid.Cols; c++)
        {
            double baseAmp = 0.15 + rnd.NextDouble() * 0.6;
            for (int r = 0; r < _grid.Rows; r++)
            {
                double roll = Math.Pow(0.92, r / 6.0) * baseAmp;
                _grid.A[c * _grid.Rows + r] = (float)(rnd.NextDouble() < 0.12 ? roll : roll * 0.15);
            }
        }
        _grid.Smear(0.6);
        _grid.Normalize();
        _canvas.Invalidate();
    }

    private void RenderSpectral(bool saveToFile)
    {
        if (_grid.Peak() < 1e-4) { _spectralState.Text = "Paint something first."; return; }
        try
        {
            _spectralState.Text = "Rendering…";
            var settings = CloneSettings();
            var grid = _grid;
            Task.Run(() => Spectral.Render(grid, settings)).ContinueWith(t =>
            {
                if (IsDisposed) return;
                BeginInvoke(() =>
                {
                    if (t.IsFaulted) { _spectralState.Text = "Render failed"; MessageBox.Show(this, t.Exception?.GetBaseException().Message, "Spectral", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    Deliver(t.Result, "spectral", saveToFile);
                });
            });
        }
        catch (Exception ex)
        {
            _spectralState.Text = "Render failed";
            MessageBox.Show(this, ex.Message, "Spectral", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RenderMeta(bool saveToFile)
    {
        try
        {
            _metaState.Text = "Rendering…";
            var slots = _slots.Select(p => p.Clone()).ToArray();
            var settings = CloneMeta();
            Task.Run(() => MetaSynth.Evolve(slots, settings)).ContinueWith(t =>
            {
                if (IsDisposed) return;
                BeginInvoke(() =>
                {
                    if (t.IsFaulted) { _metaState.Text = "Render failed"; MessageBox.Show(this, t.Exception?.GetBaseException().Message, "Meta synth", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    Deliver(t.Result, "meta", saveToFile);
                });
            });
        }
        catch (Exception ex)
        {
            _metaState.Text = "Render failed";
            MessageBox.Show(this, ex.Message, "Meta synth", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Deliver(float[] audio, string label, bool saveToFile)
    {
        if (audio == null || audio.Length < 64) { _spectralState.Text = "Nothing rendered."; return; }

        if (label == "spectral") _spectralState.Text = $"Rendered {audio.Length / (double)Dsp.SR:0.0} s";
        else _metaState.Text = $"Rendered {audio.Length / (double)Dsp.SR:0.0} s";

        if (saveToFile)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "WAV (*.wav)|*.wav",
                FileName = (label == "spectral" ? "spectral" : "meta") + ".wav",
                InitialDirectory = _host.Settings.ResolvedOutputDirectory
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            Dsp.WriteWavMono(dlg.FileName, audio);
            _host.SetStatus("Saved " + Path.GetFileName(dlg.FileName));
            return;
        }

        _host.BeginEdit("synthesis");
        var t = ProjectStore.NewTrack(label == "spectral" ? "Spectral" : "Meta", "sample");
        t.Sample = audio;
        t.SampleGate = true;
        t.SampleRoot = 60;
        t.Notes.Add(new Note(60, 0, Math.Max(0.25, audio.Length / (double)Dsp.SR / (60.0 / _host.Project.Bpm)), 110));
        _host.Project.Tracks.Add(t);
        _host.NotifyTracksChanged(t);
        _host.SetStatus("Added " + t.Name + " sample track");
    }

    private SpectralSettings CloneSettings()
    {
        var c = new SpectralSettings();
        foreach (var pi in typeof(SpectralSettings).GetProperties())
            if (pi.CanWrite) pi.SetValue(c, pi.GetValue(_settings));
        return c;
    }

    private MetaSettings CloneMeta()
    {
        var c = new MetaSettings();
        foreach (var pi in typeof(MetaSettings).GetProperties())
            if (pi.CanWrite) pi.SetValue(c, pi.GetValue(_meta));
        return c;
    }

    private void RandomizeSlots()
    {
        for (int i = 0; i < _slots.Length; i++) PropIO.Randomize(_slots[i], 0.45, Environment.TickCount + i * 977);
    }

    private void RandomizeSlot()
    {
        PropIO.Randomize(_slots[_slotChoice.SelectedIndex], 0.45, Environment.TickCount);
        BuildSlotKnobs();
    }

    private void BuildSlotKnobs()
    {
        KnobPanel.Build(_slots[_slotChoice.SelectedIndex], _slotKnobs, (_, _) => { }, Theme.Back, Theme.Text, Theme.Accent);
        _slotKnobs.AutoScrollPosition = new Point(0, 0);
    }
}

/// <summary>Paintable spectrogram grid. Left drag paints, right drag erases.</summary>
internal sealed class GridCanvas : Control
{
    public SpectralGrid? Grid;
    public double Brush = 2.6;
    public double Amount = 0.85;

    private bool _dragging;
    private bool _erase;
    private double _lastX, _lastY;
    private Bitmap? _cache;
    private int _cacheW, _cacheH;

    public GridCanvas()
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
        if (_cache == null || _cacheW != w || _cacheH != h)
        {
            _cache?.Dispose();
            _cacheW = w;
            _cacheH = h;
            _cache = new Bitmap(w, h);
        }
        using (var b = Grid.ToBitmap())
        using (var g = Graphics.FromImage(_cache))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(b, new Rectangle(0, 0, w, h));
        }
        e.Graphics.DrawImageUnscaled(_cache, 1, 1);
        using var border = new Pen(Color.FromArgb(50, 56, 70));
        e.Graphics.DrawRectangle(border, 0, 0, w + 1, h + 1);
    }

    private (double X, double Y) Cell(Point p)
    {
        if (Grid == null) return (0, 0);
        double x = (p.X - 1) / (double)Math.Max(1, Width - 2) * (Grid.Cols - 1);
        double y = (1 - (p.Y - 1) / (double)Math.Max(1, Height - 2)) * (Grid.Rows - 1);
        return (x, y);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Grid == null) return;
        _dragging = true;
        _erase = e.Button == MouseButtons.Right;
        var (x, y) = Cell(e.Location);
        _lastX = x; _lastY = y;
        Grid.Dab(x, y, Brush, Amount, _erase);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging || Grid == null) return;
        var (x, y) = Cell(e.Location);
        Grid.Line(_lastX, _lastY, x, y, Brush, Amount, _erase);
        _lastX = x; _lastY = y;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _cache?.Dispose();
        base.Dispose(disposing);
    }
}
