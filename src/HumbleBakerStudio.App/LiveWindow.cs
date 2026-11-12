using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio;

/// <summary>
/// Two independent projects, two decks, one crossfader. Master and cue can go to separate
/// Windows outputs so the next track can be prepared in headphones.
/// </summary>
internal sealed class LiveWindow : Form
{
    private readonly MainForm _main;
    private readonly LiveAudioEngine _engine;
    private readonly DeckView _deckA;
    private readonly DeckView _deckB;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 60 };

    private readonly ComboBox _masterDevice = new() { Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cueDevice = new() { Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _masterGain = new() { Minimum = 0, Maximum = 150, Value = 85, Width = 58 };
    private readonly NumericUpDown _cueLevel = new() { Minimum = 0, Maximum = 150, Value = 80, Width = 58 };
    private readonly CheckBox _cueToMaster = new() { Text = "Cue on master", AutoSize = true };
    private readonly TrackBar _crossfader = new() { Minimum = -100, Maximum = 100, TickFrequency = 25, Value = 0, Width = 230, Height = 42, BackColor = Theme.Panel };
    private readonly LevelMeter _masterMeter = new() { Width = 210 };
    private readonly LevelMeter _cueMeter = new() { Width = 210 };
    private readonly Label _state = new() { AutoSize = true, ForeColor = Theme.Muted };
    private readonly Label _engineState = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly Button _audioButton = Theme.Button("Audio off", null, true);

    private bool _advancingA, _advancingB;
    private bool _starting;

    public LiveMixer Mixer { get; } = new();
    public AppSettings Settings => _main.Settings;

    public LiveWindow(MainForm main)
    {
        _main = main;
        _engine = new LiveAudioEngine(Mixer);

        Text = "Live Performance — Humble Baker Studio";
        MinimumSize = new Size(1100, 640);
        Size = new Size(1420, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        _deckA = new DeckView(this, isA: true) { Dock = DockStyle.Fill };
        _deckB = new DeckView(this, isA: false) { Dock = DockStyle.Fill };

        BuildLayout();
        Theme.Apply(this);

        _timer.Tick += (_, _) => Tick();
        _crossfader.ValueChanged += (_, _) => Mixer.Crossfade = (_crossfader.Value + 100) / 200.0;
        _masterGain.ValueChanged += (_, _) => Mixer.MasterGain = (double)_masterGain.Value / 100.0;
        _cueLevel.ValueChanged += (_, _) => Mixer.CueLevel = (double)_cueLevel.Value / 100.0;
        _cueToMaster.CheckedChanged += (_, _) => Mixer.CueToMaster = _cueToMaster.Checked;
        _audioButton.Click += (_, _) => ToggleAudio();

        Shown += (_, _) =>
        {
            LoadDevices();
            StartAudio();
            _timer.Start();
        };
        FormClosing += (_, _) =>
        {
            _timer.Stop();
            _engine.Stop();
            Mixer.A.Stop();
            Mixer.B.Stop();
        };
    }

    private void BuildLayout()
    {
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 86,
            BackColor = Theme.Panel,
            Padding = new Padding(8, 6, 4, 4),
            WrapContents = true
        };
        top.Controls.Add(Theme.Label("Master out", Theme.Muted));
        top.Controls.Add(_masterDevice);
        top.Controls.Add(Theme.Label("Cue out", Theme.Muted));
        top.Controls.Add(_cueDevice);
        top.Controls.Add(Theme.Button("Apply outputs", (_, _) => StartAudio()));
        top.Controls.Add(_audioButton);
        top.Controls.Add(Theme.Label("Master", Theme.Muted));
        top.Controls.Add(_masterGain);
        top.Controls.Add(Theme.Label("Cue", Theme.Muted));
        top.Controls.Add(_cueLevel);
        top.Controls.Add(_cueToMaster);
        top.Controls.Add(_engineState);
        top.Controls.Add(_state);

        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.Back };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        columns.Controls.Add(_deckA, 0, 0);
        columns.Controls.Add(BuildCenter(), 1, 0);
        columns.Controls.Add(_deckB, 2, 0);

        Controls.Add(columns);
        Controls.Add(top);
    }

    private Control BuildCenter()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel, Padding = new Padding(10) };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Panel };
        panel.Controls.Add(stack);

        stack.Controls.Add(new Label { Text = "CROSSFADER", ForeColor = Theme.Accent, Font = Theme.UiBold, AutoSize = true, Margin = new Padding(0, 8, 0, 0) });
        stack.Controls.Add(_crossfader);
        var ab = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Panel };
        ab.Controls.Add(new Label { Text = "A", Width = 20, ForeColor = Theme.Text, Font = Theme.UiBold });
        ab.Controls.Add(new Label { Text = "←  blend  →", AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small, Margin = new Padding(40, 4, 40, 0) });
        ab.Controls.Add(new Label { Text = "B", Width = 20, ForeColor = Theme.Text, Font = Theme.UiBold });
        stack.Controls.Add(ab);
        stack.Controls.Add(Theme.Button("Centre", (_, _) => _crossfader.Value = 0));
        stack.Controls.Add(Spacer());
        stack.Controls.Add(new Label { Text = "MASTER", ForeColor = Theme.Muted, Font = Theme.Small, AutoSize = true });
        stack.Controls.Add(_masterMeter);
        stack.Controls.Add(new Label { Text = "CUE / HEADPHONES", ForeColor = Theme.Muted, Font = Theme.Small, AutoSize = true });
        stack.Controls.Add(_cueMeter);
        stack.Controls.Add(Spacer());
        stack.Controls.Add(Theme.Button("Pause both", (_, _) => PauseBoth()));
        stack.Controls.Add(Theme.Button("Stop both", (_, _) => StopBoth()));
        stack.Controls.Add(Spacer());
        stack.Controls.Add(new Label
        {
            Text = "How to use\r\n\r\n" +
                   "Load a project into each deck. Press Play on A, then cue B in headphones, match its tempo with Sync, and move the crossfader to transition.\r\n\r\n" +
                   "Cue out needs a second Windows output (for example a USB interface). Choose “(none)” to keep cue silent.\r\n\r\n" +
                   "Loop plays the current pattern; turn it off to render the whole song arrangement.",
            AutoSize = false,
            Width = 230,
            Height = 260,
            ForeColor = Theme.Muted,
            Font = Theme.Small,
            Margin = new Padding(0, 10, 0, 0)
        });

        return panel;
    }

    private static Control Spacer() => new Panel { Height = 10, Width = 10, BackColor = Theme.Panel };

    private void LoadDevices()
    {
        var devices = WaveOutStream.GetDevices();
        _masterDevice.Items.Clear();
        _cueDevice.Items.Clear();
        _cueDevice.Items.Add("(none)");
        foreach (var d in devices)
        {
            _masterDevice.Items.Add(d);
            _cueDevice.Items.Add(d);
        }
        if (devices.Length == 0)
        {
            _masterDevice.Items.Add("(no output devices)");
            _masterDevice.SelectedIndex = 0;
            _masterDevice.Enabled = false;
        }
        else
        {
            _masterDevice.SelectedIndex = 0;
        }
        _cueDevice.SelectedIndex = 0;
    }

    private void ToggleAudio()
    {
        if (_engine.IsRunning) StopAudio();
        else StartAudio();
    }

    private void StartAudio()
    {
        if (_starting) return;
        _starting = true;
        try
        {
            int master = _masterDevice.SelectedIndex;
            int cue = _cueDevice.SelectedIndex <= 0 ? -1 : _cueDevice.SelectedIndex - 1;
            _engine.Start(master, cue);
            _audioButton.Text = "Audio on";
            _audioButton.BackColor = Color.FromArgb(54, 120, 84);
            _engineState.Text = cue >= 0 ? "Audio: master + cue" : "Audio: master only";
        }
        finally
        {
            _starting = false;
        }
    }

    private void StopAudio()
    {
        _engine.Stop();
        _audioButton.Text = "Audio off";
        _audioButton.BackColor = Theme.PanelAlt;
        _engineState.Text = "Audio: stopped";
    }

    private void Tick()
    {
        _deckA.UpdateUi();
        _deckB.UpdateUi();
        _masterMeter.SetLevel(Mixer.LastMasterPeak);
        _cueMeter.SetLevel(Mixer.LastCuePeak);

        if (!_engine.IsRunning && !string.IsNullOrEmpty(_engine.LastError))
            _engineState.Text = _engine.LastError;

        if (Mixer.A.Ended && !Mixer.A.Playing && !_advancingA && _deckA.AutoAdvance()) _advancingA = true;
        if (Mixer.B.Ended && !Mixer.B.Playing && !_advancingB && _deckB.AutoAdvance()) _advancingB = true;

        _state.Text = $"{Mixer.A.Bpm:0.#} BPM  ↔  {Mixer.B.Bpm:0.#} BPM";
    }

    private void PauseBoth()
    {
        Mixer.A.Playing = false;
        Mixer.B.Playing = false;
        _deckA.UpdateUi();
        _deckB.UpdateUi();
    }

    private void StopBoth()
    {
        Mixer.A.Stop();
        Mixer.B.Stop();
        _deckA.UpdateUi();
        _deckB.UpdateUi();
    }

    public Project MainProjectSnapshot() => ProjectStore.LoadFromJson(ProjectStore.SaveToJson(_main.Project));

    public async Task LoadPathIntoDeck(bool isA, string path, DeckView view)
    {
        try
        {
            view.SetStatus("Loading…");
            var project = ProjectStore.Load(path);
            await LoadProjectIntoDeck(isA, project, path);
        }
        catch (Exception ex)
        {
            view.SetStatus("Load failed");
            MessageBox.Show(this, ex.Message, "Live deck", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public async Task LoadNewProjectIntoDeck(bool isA)
    {
        await LoadProjectIntoDeck(isA, ProjectStore.CreateDefault(), "New project");
    }

    public async Task LoadProjectIntoDeck(bool isA, Project project, string path)
    {
        var deck = isA ? Mixer.A : Mixer.B;
        var view = isA ? _deckA : _deckB;
        bool song = !view.LoopEnabled && project.Song.Count > 0;
        deck.Playing = false;
        view.SetStatus(song ? "Rendering song…" : "Rendering loop…");
        try
        {
            var (l, r) = await Task.Run(() => ExportService.Render(project, song, loop: !song, master: false));
            string name = string.IsNullOrEmpty(path) ? "Main project" : Path.GetFileNameWithoutExtension(path);
            string key = AiTools.NoteName(project.Key) + " " + project.Scale;
            deck.SetBuffer(name, path, l, r, project.Bpm, key, song);
            view.SetLoaded(project, path, song ? "Song ready" : "Loop ready");
            if (isA) _advancingA = false; else _advancingB = false;
            _state.Text = $"Loaded deck {(isA ? "A" : "B")}: {name}";
        }
        catch (Exception ex)
        {
            view.SetStatus("Render failed");
            MessageBox.Show(this, ex.Message, "Live deck", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
