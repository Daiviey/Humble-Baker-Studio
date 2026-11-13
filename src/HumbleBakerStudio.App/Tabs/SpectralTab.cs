using System.Drawing.Drawing2D;
using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

/// <summary>
/// Spectral painting and the four-corner meta synth, both feeding the sample engine.
/// Painting, image import, spectrogram analysis, additive resynthesis, AI drawing, live
/// morphing, instrument saving and the shared instrument library all live here.
/// </summary>
internal sealed class SpectralTab : UserControl
{
    private readonly MainForm _host;
    private readonly TabControl _inner = new() { Dock = DockStyle.Fill };

    // ---- spectral paint ----
    private readonly GridCanvas _canvas = new() { Dock = DockStyle.Fill };
    private readonly SpectralSettings _settings = new();
    private readonly SpectralGrid _grid = new(160, 64);
    private readonly FlowLayoutPanel _spectralKnobs = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        BackColor = Theme.Panel,
        Padding = new Padding(4)
    };
    private readonly FlowLayoutPanel _spectralTools = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = Theme.Panel,
        Padding = new Padding(10)
    };
    private readonly Label _spectralState = new() { AutoSize = false, Width = 330, Height = 44, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly NumericUpDown _brushSize = new() { Minimum = 1, Maximum = 20, DecimalPlaces = 1, Increment = 0.5m, Value = 3, Width = 64 };
    private readonly NumericUpDown _brushAmount = new() { Minimum = 0.05m, Maximum = 1, DecimalPlaces = 2, Increment = 0.05m, Value = 0.9m, Width = 64 };
    private readonly NumericUpDown _harmonicHz = new() { Minimum = 20, Maximum = 4000, Increment = 5, Value = 110, Width = 64 };
    private readonly NumericUpDown _harmonicParts = new() { Minimum = 1, Maximum = 40, Value = 10, Width = 52 };
    private readonly ComboBox _brushKind = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _erase = new() { Text = "Erase", AutoSize = true, ForeColor = Theme.Text };
    private readonly CheckBox _lineMode = new() { Text = "Straight line", AutoSize = true, ForeColor = Theme.Text };
    private readonly TextBox _specPrompt = new()
    {
        Width = 330,
        Height = 48,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        PlaceholderText = "Describe a shape, call, riser or texture"
    };
    private readonly Button _specAiButton;
    private readonly AudioPlayer _specPlayer = new();
    private SpectralGrid? _backup;
    private CancellationTokenSource? _cts;

    // ---- meta synth ----
    private readonly Patch[] _slots = { new(), new(), new(), new() };
    private readonly MetaSettings _meta = new();
    private readonly ComboBox _slotChoice = new() { Width = 64, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly FlowLayoutPanel _slotKnobs = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        BackColor = Theme.Back,
        Padding = new Padding(8)
    };
    private readonly FlowLayoutPanel _metaKnobs = new()
    {
        Width = 344,
        Height = 300,
        AutoScroll = true,
        BackColor = Theme.Panel,
        Padding = new Padding(4)
    };
    private readonly MorphPad _pad = new() { Width = 300, Height = 300 };
    private readonly Label _metaState = new() { AutoSize = false, Width = 330, Height = 44, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly TextBox _metaPrompt = new()
    {
        Width = 330,
        Height = 48,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        PlaceholderText = "From a soft glassy pad to a screaming acid bass"
    };
    private readonly AudioPlayer _metaPlayer = new();
    private readonly Button _metaAiButton;

    public SpectralTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        // The buttons are used while the pages are built, so create them first.
        _specAiButton = Theme.Button("AI: draw this as a spectrogram", (_, _) => RunSpectralAi(), true);
        _metaAiButton = Theme.Button("AI: design the 4 patches", (_, _) => RunMetaAi(), true);

        _inner.TabPages.Add(SpectralPage());
        _inner.TabPages.Add(MetaPage());
        Theme.StyleTabs(_inner);
        Controls.Add(_inner);

        _brushKind.Items.AddRange(new object[] { "Hard", "Soft", "Smooth", "Streak", "Gradient", "Smudge", "Harmonic" });
        _brushKind.SelectedIndex = 0;
        _brushKind.SelectedIndexChanged += (_, _) => ApplyBrushSettings();
        _erase.CheckedChanged += (_, _) => ApplyBrushSettings();
        _lineMode.CheckedChanged += (_, _) => ApplyBrushSettings();
        _brushSize.ValueChanged += (_, _) => ApplyBrushSettings();
        _brushAmount.ValueChanged += (_, _) => ApplyBrushSettings();

        _slotChoice.Items.AddRange(new object[] { "A", "B", "C", "D" });
        _slotChoice.SelectedIndex = 0;
        _slotChoice.SelectedIndexChanged += (_, _) => BuildSlotKnobs();

        _pad.Settings = _meta;
        _pad.Selected = 0;
        _pad.PointChanged += () => { };
        _pad.PointReleased += PlayMetaPoint;

        _canvas.Grid = _grid;
        _canvas.Settings = _settings;
        RandomizeSlots();
        BuildSettings();
        BuildSlotKnobs();
        ApplyBrushSettings();
    }

    // ================================================================= spectral page

    private TabPage SpectralPage()
    {
        var page = new TabPage("Spectral paint") { BackColor = Theme.Back, Padding = new Padding(0) };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
            BackColor = Theme.Back
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var canvasCard = new CardPanel { Dock = DockStyle.Fill, BackColor = Theme.Back, Margin = new Padding(0, 0, 8, 0) };
        canvasCard.Controls.Add(_canvas);
        body.Controls.Add(canvasCard, 0, 0);

        var side = new TabControl { Dock = DockStyle.Fill, Margin = new Padding(0) };
        var toolsPage = new TabPage("Paint tools") { BackColor = Theme.Panel, Padding = new Padding(0) };
        var soundPage = new TabPage("Sound settings") { BackColor = Theme.Panel, Padding = new Padding(0) };
        BuildSpectralTools();
        toolsPage.Controls.Add(_spectralTools);
        soundPage.Controls.Add(_spectralKnobs);
        side.TabPages.Add(toolsPage);
        side.TabPages.Add(soundPage);
        Theme.StyleTabs(side);
        body.Controls.Add(side, 1, 0);

        page.Controls.Add(body);
        return page;
    }

    private void BuildSpectralTools()
    {
        _spectralTools.Controls.Add(Theme.SectionLabel("Brush"));
        _spectralTools.Controls.Add(Row(Theme.Label("Size", Theme.Muted), _brushSize, Theme.Label("Amount", Theme.Muted), _brushAmount));
        _spectralTools.Controls.Add(Row(_erase, _lineMode));
        _spectralTools.Controls.Add(_brushKind);
        _spectralTools.Controls.Add(Row(
            Theme.Button("Clear", (_, _) => { _grid.Clear(); _canvas.Invalidate(); }),
            Theme.Button("Blur", (_, _) => { _grid.Blur(1, 1); _canvas.Invalidate(); }),
            Theme.Button("Smear", (_, _) => { _grid.Smear(0.93); _canvas.Invalidate(); }),
            Theme.Button("Invert", (_, _) => { _grid.Invert(); _canvas.Invalidate(); })));
        _spectralTools.Controls.Add(Row(
            Theme.Button("Flip time", (_, _) => { _grid.FlipTime(); _canvas.Invalidate(); }),
            Theme.Button("Flip pitch", (_, _) => { _grid.FlipFreq(); _canvas.Invalidate(); }),
            Theme.Button("Grain", (_, _) => { _grid.Grain(0.7, Environment.TickCount); _canvas.Invalidate(); }),
            Theme.Button("Threshold", (_, _) => { _grid.Threshold(0.4); _canvas.Invalidate(); })));
        _spectralTools.Controls.Add(Row(
            Theme.Button("Pitch +", (_, _) => { _grid.ShiftRows(2); _canvas.Invalidate(); }),
            Theme.Button("Pitch -", (_, _) => { _grid.ShiftRows(-2); _canvas.Invalidate(); }),
            Theme.Button("Harmonics", (_, _) => AddHarmonicStack())));

        _spectralTools.Controls.Add(Theme.SectionLabel("Harmonic stack"));
        _spectralTools.Controls.Add(Row(
            Theme.Label("Base Hz", Theme.Muted), _harmonicHz,
            Theme.Label("partials", Theme.Muted), _harmonicParts));
        _spectralTools.Controls.Add(Theme.Button("Add stack", (_, _) => AddHarmonicStack()));

        _spectralTools.Controls.Add(Theme.SectionLabel("Import"));
        _spectralTools.Controls.Add(Row(
            Theme.Button("Load image...", (_, _) => LoadImage()),
            Theme.Button("From selected track sound", (_, _) => SpectrogramFromTrack())));
        _spectralTools.Controls.Add(Row(Theme.Button("Undo AI / import", (_, _) => UndoSpectral())));

        _spectralTools.Controls.Add(Theme.SectionLabel("Resynthesise"));
        _spectralTools.Controls.Add(Row(
            Theme.Button("Render and play", (_, _) => AuditionSpectral(), true),
            Theme.Button("Stop", (_, _) => _specPlayer.Stop())));
        _spectralTools.Controls.Add(Row(
            Theme.Button("Render to new sample track", (_, _) => RenderSpectralToTrack()),
            Theme.Button("Save WAV...", (_, _) => SaveSpectralWav())));
        _spectralTools.Controls.Add(Theme.Button("Save as instrument", (_, _) => SaveSpectralInstrument()));

        _spectralTools.Controls.Add(Theme.SectionLabel("AI painting"));
        _spectralTools.Controls.Add(_specPrompt);
        _spectralTools.Controls.Add(_specAiButton);
        _spectralTools.Controls.Add(_spectralState);
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

    private void ApplyBrushSettings()
    {
        _canvas.BrushKind = _brushKind.SelectedIndex;
        _canvas.Amount = (double)_brushAmount.Value;
        _canvas.Brush = (double)_brushSize.Value;
        _canvas.Erase = _erase.Checked;
        _canvas.LineMode = _lineMode.Checked;
        _canvas.Invalidate();
    }

    private void AddHarmonicStack()
    {
        _grid.Harmonics((double)_harmonicHz.Value, 4, _grid.Cols - 5, (int)_harmonicParts.Value, 0.8, (double)_brushAmount.Value, _settings.MinHz, _settings.MaxHz);
        _canvas.Invalidate();
    }

    private void LoadImage()
    {
        using var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _backup = CloneGrid(_grid);
            using var img = Image.FromFile(dlg.FileName);
            using var bmp = new Bitmap(img);
            _grid.FromBitmap(bmp, false);
            _canvas.Invalidate();
            SetSpectralStatus($"Loaded image '{Path.GetFileName(dlg.FileName)}' (brightness becomes loudness).");
        }
        catch (Exception ex)
        {
            SetSpectralStatus("Could not load image: " + ex.Message);
        }
    }

    private void SpectrogramFromTrack()
    {
        var t = _host.SelectedTrack;
        if (t == null) { SetSpectralStatus("Select a track first."); return; }
        try
        {
            float[] sample = t.Sample != null && t.Sample.Length > 64 ? t.Sample : Dsp.RenderSample(t, 48, 2.5, true);
            _backup = CloneGrid(_grid);
            Spectral.FromSample(_grid, sample, _settings.MinHz, _settings.MaxHz);
            _canvas.Invalidate();
            SetSpectralStatus($"Spectrogram of '{t.Name}' loaded. Paint over it, then render.");
        }
        catch (Exception ex)
        {
            SetSpectralStatus("Failed: " + ex.Message);
        }
    }

    private void UndoSpectral()
    {
        if (_backup == null) { SetSpectralStatus("Nothing to undo."); return; }
        _grid.A = _backup.A;
        _grid.Cols = _backup.Cols;
        _grid.Rows = _backup.Rows;
        _backup = null;
        _canvas.Invalidate();
        SetSpectralStatus("Restored the previous drawing.");
    }

    private void AuditionSpectral()
    {
        if (_grid.Peak() < 0.01f) { SetSpectralStatus("The canvas is empty. Paint something first."); return; }
        var settings = CloneSettings();
        var grid = CloneGrid(_grid);
        SetSpectralStatus("Rendering...");
        Task.Run(() => Spectral.Render(grid, settings)).ContinueWith(t =>
        {
            if (IsDisposed) return;
            BeginInvoke(() =>
            {
                if (t.IsFaulted) { SetSpectralStatus("Render failed: " + t.Exception?.GetBaseException().Message); return; }
                Play(_specPlayer, t.Result);
                SetSpectralStatus($"Playing {t.Result.Length / (double)Dsp.SR:0.0} s.");
            });
        });
    }

    private void RenderSpectralToTrack()
    {
        if (_grid.Peak() < 0.01f) { SetSpectralStatus("The canvas is empty. Paint something first."); return; }
        var settings = CloneSettings();
        var grid = CloneGrid(_grid);
        SetSpectralStatus("Rendering...");
        Task.Run(() => Spectral.Render(grid, settings)).ContinueWith(t =>
        {
            if (IsDisposed) return;
            BeginInvoke(() =>
            {
                if (t.IsFaulted) { SetSpectralStatus("Render failed: " + t.Exception?.GetBaseException().Message); return; }
                AddSampleTrack("Spectral", t.Result, 60, gate: true, player: _specPlayer);
                SetSpectralStatus($"New sampler track 'Spectral' ({t.Result.Length / (double)Dsp.SR:0.0} s).");
            });
        });
    }

    private void SaveSpectralWav()
    {
        if (_grid.Peak() < 0.01f) { SetSpectralStatus("The canvas is empty. Paint something first."); return; }
        var settings = CloneSettings();
        var grid = CloneGrid(_grid);
        Task.Run(() => Spectral.Render(grid, settings)).ContinueWith(t =>
        {
            if (IsDisposed) return;
            BeginInvoke(() =>
            {
                if (t.IsFaulted) { SetSpectralStatus("Render failed: " + t.Exception?.GetBaseException().Message); return; }
                using var dlg = new SaveFileDialog
                {
                    Filter = "WAV (*.wav)|*.wav",
                    FileName = "spectral.wav",
                    InitialDirectory = _host.Settings.ResolvedOutputDirectory
                };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                Dsp.WriteWavMono(dlg.FileName, t.Result);
                _host.SetStatus("Saved " + Path.GetFileName(dlg.FileName));
                SetSpectralStatus("Saved " + Path.GetFileName(dlg.FileName));
            });
        });
    }

    private void SaveSpectralInstrument()
    {
        if (_grid.Peak() < 0.01f) { SetSpectralStatus("The canvas is empty. Paint something first."); return; }
        using var dlg = new InstrumentSaveDialog("Save spectral instrument", "My spectral sound", showRoot: true, showGate: true);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (ConfirmReplace(dlg.InstrumentName)) return;
            string path = InstrumentLibrary.SaveSpectral(dlg.InstrumentName, _grid, _settings, dlg.Root, dlg.Gate, dlg.Role, dlg.Octave, dlg.Notes);
            SetSpectralStatus($"Saved '{dlg.InstrumentName}'.");
            _host.SetStatus("Saved instrument " + Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            SetSpectralStatus("Save failed: " + ex.Message);
        }
    }

    private async void RunSpectralAi()
    {
        if (string.IsNullOrWhiteSpace(_specPrompt.Text)) { SetSpectralStatus("Describe the picture/sound first."); return; }
        _specAiButton.Enabled = false;
        _cts = new CancellationTokenSource();
        SetSpectralStatus("Thinking (a local model may need to load first)...");
        try
        {
            if (!await EnsureModelAsync()) return;
            var settings = CloneSettings();
            var grid = _grid;
            _backup = CloneGrid(_grid);
            int shapes = await _host.Ai.DrawSpectralAsync(grid, settings, _specPrompt.Text.Trim(), _cts.Token);
            CopySettings(settings, _settings);
            KnobPanel.Build(_settings, _spectralKnobs, (_, _) => { }, Theme.Panel, Theme.Text, Theme.Accent);
            _canvas.Invalidate();
            SetSpectralStatus($"Drew {shapes} shapes. Render and play to hear it.");
        }
        catch (Exception ex)
        {
            SetSpectralStatus("AI failed: " + ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _specAiButton.Enabled = true;
        }
    }

    // ================================================================= meta page

    private TabPage MetaPage()
    {
        var page = new TabPage("Meta synth") { BackColor = Theme.Back, Padding = new Padding(0) };

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

        var left = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Back, Margin = new Padding(0, 0, 8, 0) };
        var slotBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 78, BackColor = Theme.Panel, Padding = new Padding(6, 6, 4, 4), WrapContents = true };
        slotBar.Controls.Add(Theme.Label("Corner patch", Theme.Muted));
        slotBar.Controls.Add(_slotChoice);
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            var b = Theme.Button(("ABCD")[index].ToString(), (_, _) => { _slotChoice.SelectedIndex = index; });
            slotBar.Controls.Add(b);
        }
        slotBar.Controls.Add(Theme.Button("Randomize", (_, _) => RandomizeSlot(1.0)));
        slotBar.Controls.Add(Theme.Button("Mutate", (_, _) => RandomizeSlot(0.25)));
        slotBar.Controls.Add(Theme.Button("Randomize all 4", (_, _) => { RandomizeSlots(); BuildSlotKnobs(); }));
        slotBar.Controls.Add(Theme.Button("From track synth", (_, _) => CopyTrackPatch()));
        left.Controls.Add(_slotKnobs);
        left.Controls.Add(slotBar);
        body.Controls.Add(left, 0, 0);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Panel,
            Padding = new Padding(10)
        };
        var padRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Panel };
        padRow.Controls.Add(_pad);
        right.Controls.Add(padRow);
        right.Controls.Add(Theme.Label("Morph pad: click or drag, release to hear the blend.", Theme.Muted, Theme.Small));
        right.Controls.Add(Row(
            Theme.Button("Play morph point", (_, _) => PlayMetaPoint(), true),
            Theme.Button("Stop", (_, _) => _metaPlayer.Stop())));
        right.Controls.Add(Row(
            Theme.Button("Apply morph point to track synth", (_, _) => ApplyMetaToTrack()),
            Theme.Button("From track", (_, _) => CopyTrackPatch())));
        right.Controls.Add(Row(
            Theme.Button("Render evolving sound to new track", (_, _) => RenderMetaToTrack(), true),
            Theme.Button("Save WAV...", (_, _) => SaveMetaWav())));
        right.Controls.Add(Theme.Button("Save as instrument (4 patches)", (_, _) => SaveMetaInstrument()));
        right.Controls.Add(Theme.SectionLabel("AI morph design"));
        right.Controls.Add(_metaPrompt);
        right.Controls.Add(_metaAiButton);
        right.Controls.Add(_metaState);
        right.Controls.Add(Theme.SectionLabel("Morph settings"));
        right.Controls.Add(_metaKnobs);

        body.Controls.Add(right, 1, 0);
        page.Controls.Add(body);
        return page;
    }

    private void BuildSettings()
    {
        KnobPanel.Build(_settings, _spectralKnobs, (_, _) => { }, Theme.Panel, Theme.Text, Theme.Accent);
        _spectralKnobs.AutoScrollPosition = new Point(0, 0);
        KnobPanel.Build(_meta, _metaKnobs, (_, _) => _pad.Invalidate(), Theme.Panel, Theme.Text, Theme.Accent);
        _metaKnobs.AutoScrollPosition = new Point(0, 0);
    }

    private void RandomizeSlots()
    {
        for (int i = 0; i < _slots.Length; i++)
            PropIO.Randomize(_slots[i], 1.0, i * 17 + 1);
    }

    private void RandomizeSlot(double amount)
    {
        int i = Math.Clamp(_slotChoice.SelectedIndex, 0, 3);
        PropIO.Randomize(_slots[i], amount, Environment.TickCount + i * 977);
        BuildSlotKnobs();
        _pad.Invalidate();
    }

    private void BuildSlotKnobs()
    {
        int i = Math.Clamp(_slotChoice.SelectedIndex, 0, 3);
        KnobPanel.Build(_slots[i], _slotKnobs, (_, _) => _pad.Invalidate(), Theme.Back, Theme.Text, Theme.Accent);
        _slotKnobs.AutoScrollPosition = new Point(0, 0);
        _pad.Selected = i;
        _pad.Invalidate();
    }

    private void CopyTrackPatch()
    {
        var t = _host.SelectedTrack;
        if (t == null || t.Synth != "patch" || t.Patch == null)
        {
            SetMetaStatus("Select a track that uses the patch synth first.");
            return;
        }
        int i = Math.Clamp(_slotChoice.SelectedIndex, 0, 3);
        _slots[i] = t.Patch.Clone();
        BuildSlotKnobs();
        SetMetaStatus($"Copied '{t.Name}' into slot {("ABCD")[i]}.");
    }

    private void PlayMetaPoint()
    {
        try
        {
            var patch = MetaSynth.Bilinear(_slots, _meta.X, _meta.Y);
            var smp = PatchSynth.Render(patch, (int)_meta.Pitch, 1.4);
            Dsp.Normalize(smp, 0.89f);
            Play(_metaPlayer, smp);
            SetMetaStatus("Playing the morph point.");
        }
        catch (Exception ex)
        {
            SetMetaStatus("Play failed: " + ex.Message);
        }
    }

    private void ApplyMetaToTrack()
    {
        var t = _host.SelectedTrack;
        if (t == null) { SetMetaStatus("Select a track first."); return; }
        _host.BeginEdit("meta synth");
        t.Patch = MetaSynth.Bilinear(_slots, _meta.X, _meta.Y);
        t.Synth = "patch";
        t.Sample = null!;
        _host.MarkDirty();
        _host.NotifyTracksChanged(t);
        SetMetaStatus($"Track '{t.Name}' now uses the patch at the pad position.");
    }

    private void RenderMetaToTrack()
    {
        var slots = _slots.Select(p => p.Clone()).ToArray();
        var settings = CloneMeta();
        SetMetaStatus("Rendering...");
        Task.Run(() => MetaSynth.Evolve(slots, settings)).ContinueWith(t =>
        {
            if (IsDisposed) return;
            BeginInvoke(() =>
            {
                if (t.IsFaulted) { SetMetaStatus("Render failed: " + t.Exception?.GetBaseException().Message); return; }
                AddSampleTrack("Morph", t.Result, (int)settings.Pitch, gate: true, player: _metaPlayer);
                SetMetaStatus($"New sampler track 'Morph' ({t.Result.Length / (double)Dsp.SR:0.0} s).");
            });
        });
    }

    private void SaveMetaWav()
    {
        var slots = _slots.Select(p => p.Clone()).ToArray();
        var settings = CloneMeta();
        Task.Run(() => MetaSynth.Evolve(slots, settings)).ContinueWith(t =>
        {
            if (IsDisposed) return;
            BeginInvoke(() =>
            {
                if (t.IsFaulted) { SetMetaStatus("Render failed: " + t.Exception?.GetBaseException().Message); return; }
                using var dlg = new SaveFileDialog
                {
                    Filter = "WAV (*.wav)|*.wav",
                    FileName = "meta.wav",
                    InitialDirectory = _host.Settings.ResolvedOutputDirectory
                };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                Dsp.WriteWavMono(dlg.FileName, t.Result);
                _host.SetStatus("Saved " + Path.GetFileName(dlg.FileName));
                SetMetaStatus("Saved " + Path.GetFileName(dlg.FileName));
            });
        });
    }

    private void SaveMetaInstrument()
    {
        using var dlg = new InstrumentSaveDialog("Save morph instrument", "My morph", showRoot: false, showGate: false);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (ConfirmReplace(dlg.InstrumentName)) return;
            string path = InstrumentLibrary.SaveMorph(dlg.InstrumentName, _slots, _meta, dlg.Role, dlg.Octave, dlg.Notes);
            SetMetaStatus($"Saved '{dlg.InstrumentName}'.");
            _host.SetStatus("Saved instrument " + Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            SetMetaStatus("Save failed: " + ex.Message);
        }
    }

    private async void RunMetaAi()
    {
        if (string.IsNullOrWhiteSpace(_metaPrompt.Text)) { SetMetaStatus("Describe the journey first."); return; }
        _metaAiButton.Enabled = false;
        _cts = new CancellationTokenSource();
        SetMetaStatus("Thinking (a local model may need to load first)...");
        try
        {
            if (!await EnsureModelAsync()) return;
            int applied = await _host.Ai.DesignMorphAsync(_slots, _metaPrompt.Text.Trim(), _cts.Token);
            BuildSlotKnobs();
            _pad.Invalidate();
            SetMetaStatus($"Set 4 patches ({applied} parameters). Click the pad to hear the morph or render it.");
        }
        catch (Exception ex)
        {
            SetMetaStatus("AI failed: " + ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _metaAiButton.Enabled = true;
        }
    }

    // ================================================================= helpers

    private bool ConfirmReplace(string name)
    {
        var existing = InstrumentLibrary.Find(name, InstrumentLibrary.FindRoots(SampleLibrary.RootPaths));
        if (existing == null) return false;
        return MessageBox.Show(this, $"An instrument called '{name}' exists. Replace it?", "Instrument library",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes;
    }

    private async Task<bool> EnsureModelAsync()
    {
        if (!string.IsNullOrWhiteSpace(_host.Ai.Model)) return true;
        var models = await _host.Ollama.ListModelsAsync();
        if (models.Length == 0)
        {
            SetSpectralStatus("No Ollama model found. Start Ollama or choose a model in the AI tab.");
            SetMetaStatus("No Ollama model found. Start Ollama or choose a model in the AI tab.");
            return false;
        }
        string preferred = models.FirstOrDefault(m => string.Equals(m, _host.Settings.OllamaModel, StringComparison.OrdinalIgnoreCase))
                           ?? models.FirstOrDefault(m => m.Contains("qwen", StringComparison.OrdinalIgnoreCase))
                           ?? models.FirstOrDefault(m => m.Contains("llama", StringComparison.OrdinalIgnoreCase))
                           ?? models[0];
        _host.Ai.Model = preferred;
        _host.Settings.OllamaModel = preferred;
        return true;
    }

    private void AddSampleTrack(string name, float[] audio, int root, bool gate, AudioPlayer? player = null)
    {
        _host.BeginEdit("synthesis");
        var t = ProjectStore.NewTrack(name, "sample");
        t.Sample = audio;
        t.SampleRoot = Math.Clamp(root, 0, 127);
        t.SampleGate = gate;
        t.SampleMode = 0;
        double beats = Math.Max(0.25, audio.Length / (double)Dsp.SR / (60.0 / _host.Project.Bpm));
        t.Notes.Add(new Note(t.SampleRoot, 0, beats, 110));
        _host.Project.Tracks.Add(t);
        _host.NotifyTracksChanged(t);
        if (player != null) Play(player, audio);
    }

    private static void Play(AudioPlayer player, float[] mono)
    {
        var pcm = new short[mono.Length * 2];
        for (int i = 0; i < mono.Length; i++)
        {
            int v = (int)Math.Round(Math.Clamp(mono[i], -1f, 1f) * 32767f);
            pcm[2 * i] = (short)v;
            pcm[2 * i + 1] = (short)v;
        }
        player.Play(pcm, Dsp.SR);
    }

    private static SpectralGrid CloneGrid(SpectralGrid g)
    {
        var c = new SpectralGrid(g.Cols, g.Rows);
        Array.Copy(g.A, c.A, Math.Min(g.A.Length, c.A.Length));
        return c;
    }

    private static void CopySettings(SpectralSettings from, SpectralSettings to)
    {
        foreach (var pi in typeof(SpectralSettings).GetProperties())
            if (pi.CanWrite) pi.SetValue(to, pi.GetValue(from));
    }

    private SpectralSettings CloneSettings()
    {
        var c = new SpectralSettings();
        CopySettings(_settings, c);
        return c;
    }

    private MetaSettings CloneMeta()
    {
        var c = new MetaSettings();
        foreach (var pi in typeof(MetaSettings).GetProperties())
            if (pi.CanWrite) pi.SetValue(c, pi.GetValue(_meta));
        return c;
    }

    private void SetSpectralStatus(string text)
    {
        _spectralState.Text = text;
        _host.SetStatus(text);
    }

    private void SetMetaStatus(string text)
    {
        _metaState.Text = text;
        _host.SetStatus(text);
    }

    public void RefreshView() { }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Dispose();
            _specPlayer.Dispose();
            _metaPlayer.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>Paintable spectrogram grid. Left drag paints, right drag erases, and the seven brush types mirror the PowerShell studio.</summary>
internal sealed class GridCanvas : Control
{
    public SpectralGrid? Grid;
    public SpectralSettings? Settings;
    public double Brush = 3;
    public double Amount = 0.9;
    public int BrushKind;
    public bool Erase;
    public bool LineMode;

    private bool _dragging;
    private bool _eraseStroke;
    private double _lastX, _lastY, _startX, _startY;

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
        using (var b = Grid.ToBitmap())
        {
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.DrawImage(b, new Rectangle(1, 1, w, h));
        }

        // Octave/frequency guides so the vertical axis means something.
        if (Settings != null && Grid.Rows > 1)
        {
            using var guide = new Pen(Color.FromArgb(70, 255, 255, 255));
            for (int oct = 1; oct <= 6; oct++)
            {
                double hz = 55 * Math.Pow(2, oct - 1);
                if (hz < Settings.MinHz || hz > Settings.MaxHz) continue;
                double row = Spectral.HzToRow(hz, Settings.MinHz, Settings.MaxHz, Grid.Rows);
                double y = h - 1 - row / (Grid.Rows - 1) * (h - 1);
                e.Graphics.DrawLine(guide, 0, (float)y, 12, (float)y);
                e.Graphics.DrawString(((int)hz).ToString(), Theme.Small, Brushes.Gray, 13, (float)(y - 7));
            }
        }

        using var border = new Pen(Color.FromArgb(50, 56, 70));
        e.Graphics.DrawRectangle(border, 0, 0, w + 1, h + 1);
    }

    private (double X, double Y) Cell(Point p)
    {
        if (Grid == null) return (0, 0);
        double x = (p.X - 1) / (double)Math.Max(1, Width - 2) * (Grid.Cols - 1);
        double y = (1 - (p.Y - 1) / (double)Math.Max(1, Height - 2)) * (Grid.Rows - 1);
        return (Math.Max(0, Math.Min(Grid.Cols - 1, x)), Math.Max(0, Math.Min(Grid.Rows - 1, y)));
    }

    private void Apply(double x0, double y0, double x1, double y1)
    {
        if (Grid == null) return;
        bool erase = Erase || _eraseStroke;
        switch (BrushKind)
        {
            case 0: Grid.Line(x0, y0, x1, y1, Brush, Amount, erase); break;
            case 1: Grid.Line(x0, y0, x1, y1, Brush * 1.8, Amount * 0.55, erase); break;
            case 2: Grid.Line(x0, y0, x1, y1, Brush, Amount * 0.7, erase); Grid.Blur(1, 0); break;
            case 3: Grid.Line(x0, y0, x1, y1, Brush * 0.55, Amount, erase); break;
            case 4: Grid.Line(x0, y0, x1, y1, Brush * 2.2, Amount * 0.35, erase); break;
            case 5: Grid.Smear(0.9); Grid.Line(x0, y0, x1, y1, Brush, Amount * 0.6, erase); break;
            default:
                if (Settings != null) Grid.Harmonics(110, (int)x0, (int)x1, 10, 0.8, Amount, Settings.MinHz, Settings.MaxHz);
                break;
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Grid == null) return;
        _dragging = true;
        _eraseStroke = e.Button == MouseButtons.Right;
        var (x, y) = Cell(e.Location);
        _lastX = x; _lastY = y; _startX = x; _startY = y;
        if (!LineMode) { Apply(x, y, x, y); Invalidate(); }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging || Grid == null || LineMode) return;
        var (x, y) = Cell(e.Location);
        Apply(_lastX, _lastY, x, y);
        _lastX = x; _lastY = y;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (Grid == null || !_dragging) { _dragging = false; return; }
        if (LineMode)
        {
            var (x, y) = Cell(e.Location);
            Apply(_startX, _startY, x, y);
            Invalidate();
        }
        _dragging = false;
    }
}

/// <summary>The four-corner morph pad: shows the journey path and the A/B/C/D corners.</summary>
internal sealed class MorphPad : Control
{
    public MetaSettings? Settings;
    public int Selected;
    public event Action? PointChanged;
    public event Action? PointReleased;

    private bool _dragging;

    public MorphPad()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(18, 20, 28);
        Cursor = Cursors.Cross;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int w = Width - 2, h = Height - 2, m = 18;
        using var pen = new Pen(Color.FromArgb(90, 150, 160, 190));
        var pts = new PointF[61];
        for (int i = 0; i <= 60; i++)
        {
            MetaSynth.PathPoint(Settings?.Path ?? MetaPath.tour, i / 60.0, (int)(Settings?.Seed ?? 1), out double x, out double y);
            pts[i] = new PointF((float)(m + x * (w - 2 * m)), (float)(m + y * (h - 2 * m)));
        }
        e.Graphics.DrawLines(pen, pts);

        string names = "ABCD";
        int[] cx = { 0, 1, 0, 1 };
        int[] cy = { 0, 0, 1, 1 };
        for (int i = 0; i < 4; i++)
        {
            float px = m + cx[i] * (w - 2 * m);
            float py = m + cy[i] * (h - 2 * m);
            using var fill = new SolidBrush(i == Selected ? Color.Gold : Color.SlateGray);
            e.Graphics.FillEllipse(fill, px - 9, py - 9, 18, 18);
            e.Graphics.DrawString(names[i].ToString(), Theme.UiBold, Brushes.Black, px - 5, py - 7);
        }

        if (Settings != null)
        {
            float mx = m + (float)Settings.X * (w - 2 * m);
            float my = m + (float)Settings.Y * (h - 2 * m);
            e.Graphics.FillEllipse(Brushes.Cyan, mx - 6, my - 6, 12, 12);
        }

        using var border = new Pen(Color.FromArgb(54, 61, 78));
        e.Graphics.DrawRectangle(border, 0, 0, w + 1, h + 1);
    }

    private void MoveTo(Point p)
    {
        if (Settings == null) return;
        double w = Width - 2.0, h = Height - 2.0, m = 18.0;
        Settings.X = Math.Max(0, Math.Min(1, (p.X - m) / Math.Max(1, w - 2 * m)));
        Settings.Y = Math.Max(0, Math.Min(1, (p.Y - m) / Math.Max(1, h - 2 * m)));
        Invalidate();
        PointChanged?.Invoke();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _dragging = true;
        MoveTo(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) MoveTo(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging) return;
        _dragging = false;
        MoveTo(e.Location);
        PointReleased?.Invoke();
    }
}
