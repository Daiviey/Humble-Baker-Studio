using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio;

/// <summary>One deck of the live performance window.</summary>
internal sealed class DeckView : UserControl
{
    private readonly LiveWindow _host;
    private readonly bool _isA;
    private readonly Label _title = new() { AutoSize = false, Dock = DockStyle.Fill, Font = Theme.Title, ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly Label _source = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly DeckWave _wave = new() { Dock = DockStyle.Fill };
    private readonly Label _info = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small, Text = "No track loaded" };
    private readonly Button _play = Theme.Button("Play", null, true);
    private readonly Button _cue = Theme.Button("Cue", null, true);
    private readonly Button _sync = Theme.Button("Sync", null, true);
    private readonly CheckBox _loop = new() { Text = "Loop", AutoSize = true, Checked = true };
    private readonly TrackBar _gain = new() { Minimum = 0, Maximum = 150, Value = 100, TickFrequency = 25, Width = 210, BackColor = Theme.Panel };
    private readonly TrackBar _filter = new() { Minimum = -100, Maximum = 100, Value = 0, TickFrequency = 25, Width = 210, BackColor = Theme.Panel };
    private readonly Label _gainText = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly Label _filterText = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly SimpleList _queue = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly Label _cueBadge = new() { Dock = DockStyle.Right, Width = 56, TextAlign = ContentAlignment.MiddleCenter, Font = Theme.Small, ForeColor = Theme.Muted, Text = "CUE" };
    private readonly List<string> _queuePaths = new();
    private int _queueIndex = -1;
    private Project? _project;
    private string _path = "";
    private bool _building;

    public DeckView(LiveWindow host, bool isA)
    {
        _host = host;
        _isA = isA;
        BackColor = Theme.Card;
        Padding = new Padding(10);

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = isA ? Color.FromArgb(40, 62, 100) : Color.FromArgb(94, 58, 40) };
        var tag = new Label
        {
            Text = isA ? "A" : "B",
            Width = 34,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 16f),
            ForeColor = Theme.Text
        };
        _title.Text = isA ? "Deck A" : "Deck B";
        _cueBadge.Text = "CUE";
        titleBar.Controls.Add(_title);
        titleBar.Controls.Add(_cueBadge);
        titleBar.Controls.Add(tag);

        var wavePanel = new Panel { Dock = DockStyle.Top, Height = 116, Padding = new Padding(0, 4, 0, 4) };
        wavePanel.Controls.Add(_wave);

        var infoPanel = new Panel { Dock = DockStyle.Top, Height = 50 };
        _info.Location = new Point(2, 4);
        _source.Location = new Point(2, 26);
        infoPanel.Controls.Add(_info);
        infoPanel.Controls.Add(_source);

        var transport = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            ColumnCount = 7,
            RowCount = 1,
            BackColor = Theme.Card,
            Padding = new Padding(0, 2, 0, 2)
        };
        for (int i = 0; i < 7; i++)
            transport.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, i == 6 ? 16f : 14f));

        _play.AutoSize = false;
        _play.Dock = DockStyle.Fill;
        _play.Margin = new Padding(1);
        _play.Padding = new Padding(4, 2, 4, 2);
        _play.Font = Theme.Small;
        _cue.AutoSize = false;
        _cue.Dock = DockStyle.Fill;
        _cue.Margin = new Padding(1);
        _cue.Padding = new Padding(4, 2, 4, 2);
        _cue.Font = Theme.Small;
        _sync.AutoSize = false;
        _sync.Dock = DockStyle.Fill;
        _sync.Margin = new Padding(1);
        _sync.Padding = new Padding(4, 2, 4, 2);
        _sync.Font = Theme.Small;
        _loop.AutoSize = false;
        _loop.Dock = DockStyle.Fill;
        _loop.CheckAlign = ContentAlignment.MiddleLeft;
        _loop.Margin = new Padding(2, 1, 1, 1);
        _loop.Font = Theme.Small;

        Button MakeDeckButton(string text, EventHandler click, string? tip = null)
        {
            var b = Theme.Button(text, click, false, tip);
            b.AutoSize = false;
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(1);
            b.Padding = new Padding(4, 2, 4, 2);
            b.Font = Theme.Small;
            return b;
        }

        transport.Controls.Add(_play, 0, 0);
        transport.Controls.Add(_cue, 1, 0);
        transport.Controls.Add(_sync, 2, 0);
        transport.Controls.Add(_loop, 3, 0);
        transport.Controls.Add(MakeDeckButton("Load…", (_, _) => ChooseFile(), "Load a project into this deck"), 4, 0);
        transport.Controls.Add(MakeDeckButton("Main", (_, _) => LoadMain(), "Load the project from the main studio window"), 5, 0);
        transport.Controls.Add(MakeDeckButton("New", (_, _) => NewProject(), "Create a new project in this deck"), 6, 0);

        var controls = new TableLayoutPanel { Dock = DockStyle.Top, Height = 100, ColumnCount = 2, RowCount = 2, BackColor = Theme.Panel };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var gainCell = new Panel { Dock = DockStyle.Fill };
        _gain.Location = new Point(0, 20);
        _gain.Height = 40;
        gainCell.Controls.Add(_gain);
        gainCell.Controls.Add(new Label { Text = "GAIN", ForeColor = Theme.Muted, Font = Theme.Small, Location = new Point(0, 0), AutoSize = true });
        gainCell.Controls.Add(_gainText);
        _gainText.Location = new Point(150, 0);
        var filterCell = new Panel { Dock = DockStyle.Fill };
        _filter.Location = new Point(0, 20);
        _filter.Height = 40;
        filterCell.Controls.Add(_filter);
        filterCell.Controls.Add(new Label { Text = "FILTER  LP / HP", ForeColor = Theme.Muted, Font = Theme.Small, Location = new Point(0, 0), AutoSize = true });
        filterCell.Controls.Add(_filterText);
        _filterText.Location = new Point(150, 0);
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        controls.Controls.Add(gainCell, 0, 0);
        controls.Controls.Add(filterCell, 1, 0);
        controls.Controls.Add(_status, 0, 1);
        controls.SetColumnSpan(_status, 2);

        var queueTitle = new Label { Text = "QUEUE", Dock = DockStyle.Top, Height = 20, ForeColor = Theme.Accent, Font = Theme.UiBold };
        var queueButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 62, WrapContents = true, BackColor = Theme.Panel };
        queueButtons.Controls.Add(Theme.Button("Add…", (_, _) => AddToQueue()));
        queueButtons.Controls.Add(Theme.Button("Next", (_, _) => Next()));
        queueButtons.Controls.Add(Theme.Button("Clear", (_, _) => ClearQueue()));
        queueButtons.Controls.Add(Theme.Button("Load selected", (_, _) => LoadSelectedQueue()));

        var queuePanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel };
        queuePanel.Controls.Add(_queue);
        queuePanel.Controls.Add(queueButtons);
        queuePanel.Controls.Add(queueTitle);

        Controls.Add(queuePanel);
        Controls.Add(controls);
        Controls.Add(transport);
        Controls.Add(infoPanel);
        Controls.Add(wavePanel);
        Controls.Add(titleBar);

        _play.Click += (_, _) => TogglePlay();
        _cue.Click += (_, _) => ToggleCue();
        _sync.Click += (_, _) => Sync();
        _loop.CheckedChanged += (_, _) => { if (!_building) Reload(); };
        _gain.ValueChanged += (_, _) =>
        {
            if (_building) return;
            Deck.Gain = _gain.Value / 100.0;
            UpdateControlText();
        };
        _filter.ValueChanged += (_, _) =>
        {
            if (_building) return;
            Deck.Filter = _filter.Value / 100.0;
            UpdateControlText();
        };
        _queue.DoubleClick += (_, _) => LoadSelectedQueue();

        UpdateControlText();
        UpdateUi();
    }

    public LiveDeck Deck => _isA ? _host.Mixer.A : _host.Mixer.B;

    private Button FixedButton(string text, int width, EventHandler click)
    {
        var b = Theme.Button(text, click);
        b.AutoSize = false;
        b.Width = width;
        b.Height = 28;
        return b;
    }
    public bool LoopEnabled => _loop.Checked;

    public void SetLoaded(Project project, string path, string message)
    {
        _project = project;
        _path = path;
        _title.Text = Deck.Name;
        _source.Text = string.IsNullOrEmpty(path) ? message : Path.GetFileName(path);
        _status.Text = message;
        _wave.SetData(Deck.GetLeft(), Deck.GetRight());
        _wave.Playhead = 0;
        UpdateUi();
    }

    public void SetStatus(string text) => _status.Text = text;

    public void UpdateUi()
    {
        _building = true;
        try
        {
            _play.Text = Deck.Playing ? "Pause" : "Play";
            _play.BackColor = Deck.Playing ? Color.FromArgb(54, 120, 84) : Color.FromArgb(54, 92, 156);
            _cue.BackColor = Deck.Cue ? Theme.AccentWarm : Theme.PanelAlt;
            _sync.BackColor = Math.Abs(Deck.Rate - 1) > 0.001 ? Color.FromArgb(54, 92, 156) : Theme.PanelAlt;
            _cueBadge.ForeColor = Deck.Cue ? Theme.AccentWarm : Theme.Muted;
            _cueBadge.Text = Deck.Cue ? "CUE ON" : "CUE";
            _gain.Value = Math.Clamp((int)Math.Round(Deck.Gain * 100), _gain.Minimum, _gain.Maximum);
            _filter.Value = Math.Clamp((int)Math.Round(Deck.Filter * 100), _filter.Minimum, _filter.Maximum);
            _info.Text = Deck.HasAudio
                ? $"{Deck.Bpm:0.##} BPM · {Deck.KeyText} · {Deck.Seconds:0.0} s · rate {Deck.Rate:0.000}× · {(Deck.Loop ? "loop" : "song")}"
                : "No track loaded";
            if (Deck.Seconds > 0)
                _wave.Playhead = Math.Clamp(Deck.PositionSeconds / Deck.Seconds, 0, 1);
            else
                _wave.Playhead = -1;
            _status.Text = Deck.HasAudio
                ? (Deck.Playing ? "Playing" : "Ready") + (Deck.Rate != 1 ? $" · sync {Deck.Rate:0.000}×" : "")
                : "Load a project to begin";
        }
        finally
        {
            _building = false;
        }
        UpdateControlText();
    }

    private void UpdateControlText()
    {
        _gainText.Text = $"{Deck.Gain * 100:0}%";
        _filterText.Text = Math.Abs(Deck.Filter) < 0.01 ? "bypass" : Deck.Filter < 0 ? $"LP {Math.Abs(Deck.Filter) * 100:0}%" : $"HP {Deck.Filter * 100:0}%";
    }

    private void TogglePlay()
    {
        if (!Deck.HasAudio) { ChooseFile(); return; }
        Deck.Playing = !Deck.Playing;
        if (Deck.Playing && Deck.PositionSeconds >= Deck.Seconds - 0.01) Deck.Restart();
        UpdateUi();
    }

    private void ToggleCue()
    {
        Deck.Cue = !Deck.Cue;
        UpdateUi();
    }

    private void Sync()
    {
        _host.Mixer.Sync(Deck);
        UpdateUi();
    }

    private async void ChooseFile()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Humble Baker project (*.json;*.hbstudio.json)|*.json;*.hbstudio.json|All files (*.*)|*.*",
            Title = "Load a project into deck " + (_isA ? "A" : "B")
        };
        if (!string.IsNullOrWhiteSpace(_host.Settings.LastProject))
            dlg.InitialDirectory = Path.GetDirectoryName(_host.Settings.LastProject);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        await _host.LoadPathIntoDeck(_isA, dlg.FileName, this);
    }

    private async void LoadMain()
    {
        await _host.LoadProjectIntoDeck(_isA, _host.MainProjectSnapshot(), "Main project");
    }

    private async void NewProject()
    {
        if (Deck.HasAudio)
        {
            var answer = MessageBox.Show(this,
                "Replace deck " + (_isA ? "A" : "B") + " with a new project?",
                "Live performance", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }
        await _host.LoadNewProjectIntoDeck(_isA);
    }

    private async void Reload()
    {
        if (_project == null) return;
        await _host.LoadProjectIntoDeck(_isA, _project, string.IsNullOrEmpty(_path) ? "Re-render" : _path);
    }

    private void AddToQueue()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Humble Baker project (*.json;*.hbstudio.json)|*.json;*.hbstudio.json|All files (*.*)|*.*",
            Multiselect = true,
            Title = "Add projects to the deck queue"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        foreach (var f in dlg.FileNames)
        {
            _queuePaths.Add(f);
            _queue.Add(Path.GetFileNameWithoutExtension(f));
        }
    }

    private void ClearQueue()
    {
        _queuePaths.Clear();
        _queue.Clear();
        _queueIndex = -1;
    }

    private async void LoadSelectedQueue()
    {
        int i = _queue.SelectedIndex;
        if (i < 0 || i >= _queuePaths.Count) return;
        _queueIndex = i;
        await _host.LoadPathIntoDeck(_isA, _queuePaths[i], this);
    }

    public async void Next()
    {
        if (_queuePaths.Count == 0) return;
        _queueIndex = _queueIndex < 0 ? 0 : (_queueIndex + 1) % _queuePaths.Count;
        await _host.LoadPathIntoDeck(_isA, _queuePaths[_queueIndex], this);
    }

    public bool AutoAdvance()
    {
        if (_queuePaths.Count == 0) return false;
        Next();
        return true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
