using HumbleBakerStudio.Services;
using HumbleBakerStudio.Tabs;
using HumbleBakerStudio.Audio;
using System.Runtime.InteropServices;

namespace HumbleBakerStudio;

internal sealed class MainForm : Form
{
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private readonly AppSettings _settings;
    private readonly AudioPlayer _player = new();
    private readonly OllamaClient _ollama;
    private readonly AiTools _ai;
    private readonly System.Windows.Forms.Timer _playTimer = new() { Interval = 33 };
    private readonly UndoHistory _undo = new();

    private Project _project;
    private Track? _selected;
    private string? _projectPath;
    private bool _dirty;
    private bool _refreshing;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly SequencerTab _sequencer;
    private readonly ToolsTab _toolsTab;
    private readonly ArrangementTab _arrangementTab;
    private readonly TracksTab _tracksTab;
    private readonly EffectsTab _effectsTab;
    private readonly SynthTab _synthTab;
    private readonly GraphTab _graphTab;
    private readonly MasterTab _masterTab;
    private readonly MixerTab _mixerTab;
    private readonly AiTab _aiTab;
    private readonly SettingsTab _settingsTab;

    private readonly NumericUpDown _bpm = new() { Minimum = 30, Maximum = 300, DecimalPlaces = 2, Increment = 1, Width = 68 };
    private readonly NumericUpDown _bars = new() { Minimum = 1, Maximum = 64, Width = 52 };
    private readonly CheckBox _loop = new() { Text = "Loop", AutoSize = true, Checked = true };
    private readonly CheckBox _song = new() { Text = "Song", AutoSize = true };
    private readonly CheckBox _master = new() { Text = "Master", AutoSize = true };
    private readonly Button _playButton = Theme.Button("▶  Play", null, true);
    private readonly Label _status = new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(10, 9, 3, 3) };
    private readonly Label _position = new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(10, 9, 3, 3) };
    private readonly Label _title = new() { AutoSize = true, ForeColor = Theme.Text, Font = Theme.UiBold, Margin = new Padding(10, 8, 3, 3) };
    private readonly TrackListPanel _trackList;
    private LiveWindow? _liveWindow;

    public MainForm()
    {
        Text = "Humble Baker Studio";
        MinimumSize = new Size(1180, 700);
        Size = new Size(1440, 900);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        KeyPreview = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        _settings = AppSettings.Load();
        _ollama = new OllamaClient(_settings.OllamaUrl);
        _ai = new AiTools(_ollama) { Model = _settings.OllamaModel };
        SampleLibrary.Discover(_settings);

        _project = LoadInitialProject();

        _trackList = new TrackListPanel(this);
        _sequencer = new SequencerTab(this);
        _toolsTab = new ToolsTab(this);
        _arrangementTab = new ArrangementTab(this);
        _tracksTab = new TracksTab(this);
        _effectsTab = new EffectsTab(this);
        _synthTab = new SynthTab(this);
        _graphTab = new GraphTab(this);
        _masterTab = new MasterTab(this);
        _mixerTab = new MixerTab(this);
        _aiTab = new AiTab(this);
        _settingsTab = new SettingsTab(this);

        BuildLayout();

        _playTimer.Tick += (_, _) => UpdatePlayhead();
        _player.Finished += () => BeginInvoke(() =>
        {
            _playTimer.Stop();
            _sequencer.SetPlayhead(-1);
            _playButton.Text = "▶  Play";
            SetStatus("Ready");
        });

        FormClosing += OnClosing;
        KeyDown += OnKeyDown;
        Shown += (_, _) =>
        {
            _sequencer.ScrollToContent();
            if (Environment.GetEnvironmentVariable("HBS_OPEN_LIVE") == "1") OpenLiveWindow();
        };

        RefreshFromProject(keepSelection: true);
        _trackList.Select(0);
        _undo.Reset(_project);
        SetStatus("Ready");

        // Small hook for screenshots and smoke tests; harmless in normal use.
        var startTab = Environment.GetEnvironmentVariable("HBS_START_TAB");
        if (!string.IsNullOrWhiteSpace(startTab)) SelectTabByContent(startTab);
        var subTab = Environment.GetEnvironmentVariable("HBS_START_SUBTAB");
        if (!string.IsNullOrWhiteSpace(subTab)) _toolsTab.Select(subTab);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int on = 1;
            // DWMWA_USE_IMMERSIVE_DARK_MODE (20 on current Windows, 19 on older builds).
            if (DwmSetWindowAttribute(Handle, 20, ref on, sizeof(int)) != 0)
                DwmSetWindowAttribute(Handle, 19, ref on, sizeof(int));
        }
        catch
        {
            // Dark title bar is cosmetic; ignore unsupported Windows builds.
        }
    }

    // ---- shared services for tabs ---------------------------------------

    public Project Project => _project;
    public Track? SelectedTrack => _selected;
    public AppSettings Settings => _settings;
    public AudioPlayer Player => _player;
    public AiTools Ai => _ai;
    public OllamaClient Ollama => _ollama;
    public bool SongMode => _song.Checked;
    public bool MasterOnPlayback => _master.Checked;

    private Project LoadInitialProject()
    {
        try
        {
            if (_settings.OpenLastProject && !string.IsNullOrWhiteSpace(_settings.LastProject) && File.Exists(_settings.LastProject))
            {
                _projectPath = _settings.LastProject;
                return ProjectStore.Load(_settings.LastProject);
            }
        }
        catch
        {
            // Fall back to a fresh project.
        }
        return ProjectStore.CreateDefault();
    }

    // ---- layout ----------------------------------------------------------

    private void BuildLayout()
    {
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Theme.Panel,
            Padding = new Padding(6, 6, 6, 4),
            WrapContents = false,
            AutoScroll = true
        };

        top.Controls.Add(Theme.Button("New", (_, _) => NewProject()));
        top.Controls.Add(Theme.Button("Open", (_, _) => OpenProject()));
        top.Controls.Add(Theme.Button("Save", (_, _) => SaveProject(false)));
        top.Controls.Add(Theme.Button("Undo", (_, _) => Undo()));
        top.Controls.Add(Theme.Button("Redo", (_, _) => Redo()));
        top.Controls.Add(Separator());
        top.Controls.Add(Theme.Label("BPM"));
        top.Controls.Add(_bpm);
        top.Controls.Add(Theme.Label("Bars"));
        top.Controls.Add(_bars);
        top.Controls.Add(_loop);
        top.Controls.Add(_song);
        top.Controls.Add(Separator());
        top.Controls.Add(_playButton);
        top.Controls.Add(Theme.Button("■  Stop", (_, _) => StopPlayback()));
        top.Controls.Add(Separator());
        top.Controls.Add(_master);
        top.Controls.Add(Theme.Button("Export…", (_, _) => ExportProject()));
        top.Controls.Add(Separator());
        top.Controls.Add(Theme.Button("Live", (_, _) => OpenLiveWindow(), true));
        top.Controls.Add(Theme.Button("AI", (_, _) => SelectTab(_aiTab)));
        top.Controls.Add(Theme.Button("Settings", (_, _) => SelectTab(_settingsTab)));

        _bpm.ValueChanged += (_, _) =>
        {
            if (_refreshing || _bpm.Value <= 0) return;
            BeginEdit("tempo");
            _project.Bpm = (double)_bpm.Value;
            MarkDirty();
        };
        _bars.ValueChanged += (_, _) =>
        {
            if (_refreshing) return;
            BeginEdit("bars");
            _project.Bars = (int)_bars.Value;
            _sequencer.RefreshRoll();
            MarkDirty();
        };
        _loop.CheckedChanged += (_, _) => _sequencer.RefreshRoll();
        _master.CheckedChanged += (_, _) => _settings.MasterOnPlayback = _master.Checked;
        _playButton.Click += (_, _) => TogglePlayback();

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = Theme.Panel, Padding = new Padding(8, 4, 8, 0) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = Theme.Panel };
        flow.Controls.Add(_status);
        flow.Controls.Add(_position);
        flow.Controls.Add(new Label { AutoSize = true, Width = 12 });
        flow.Controls.Add(_title);
        bottom.Controls.Add(flow);

        var sidebar = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.Panel, Padding = new Padding(6) };
        _trackList.Dock = DockStyle.Fill;
        sidebar.Controls.Add(_trackList);

        _tabs.Dock = DockStyle.Fill;
        _tabs.TabPages.Add(Page("Sequencer", _sequencer));
        _tabs.TabPages.Add(Page("Tools", _toolsTab));
        _tabs.TabPages.Add(Page("Arrangement", _arrangementTab));
        _tabs.TabPages.Add(Page("Tracks", _tracksTab));
        _tabs.TabPages.Add(Page("Effects", _effectsTab));
        _tabs.TabPages.Add(Page("Synth", _synthTab));
        _tabs.TabPages.Add(Page("Graph", _graphTab));
        _tabs.TabPages.Add(Page("Mixer", _mixerTab));
        _tabs.TabPages.Add(Page("Master", _masterTab));
        _tabs.TabPages.Add(Page("AI", _aiTab));
        _tabs.TabPages.Add(Page("Settings", _settingsTab));
        Theme.StyleTabs(_tabs);

        Controls.Add(_tabs);
        Controls.Add(sidebar);
        Controls.Add(bottom);
        Controls.Add(top);
        Theme.Apply(this);
    }

    private static TabPage Page(string text, Control content)
    {
        var page = new TabPage(text) { BackColor = Theme.Back, Padding = new Padding(0) };
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        return page;
    }

    private static Control Separator() => new Panel
    {
        Width = 1,
        Height = 26,
        BackColor = Theme.Border,
        Margin = new Padding(8, 4, 8, 3)
    };

    public void SelectTabByContent(string tabText)
    {
        foreach (TabPage p in _tabs.TabPages)
            if (string.Equals(p.Text, tabText, StringComparison.OrdinalIgnoreCase))
            {
                _tabs.SelectedTab = p;
                return;
            }
    }

    private void SelectTab(Control tab)
    {
        foreach (TabPage p in _tabs.TabPages)
            if (p.Controls.Contains(tab)) { _tabs.SelectedTab = p; break; }
    }

    // ---- state broadcast -------------------------------------------------

    public void RefreshFromProject(bool keepSelection = false)
    {
        _refreshing = true;
        try
        {
            _bpm.Value = (decimal)Math.Clamp(_project.Bpm, 30, 300);
            _bars.Value = Math.Clamp(_project.Bars, 1, 64);
            _master.Checked = _settings.MasterOnPlayback;

            if (!keepSelection || _selected == null || !_project.Tracks.Contains(_selected))
                _selected = _project.Tracks.FirstOrDefault();

            _trackList.Rebuild();
            _sequencer.SetProject(_project, _selected);
            _toolsTab.RefreshView();
            _arrangementTab.RefreshView();
            _tracksTab.RefreshView();
            _effectsTab.RefreshView();
            _synthTab.RefreshView();
            _graphTab.RefreshView();
            _masterTab.RefreshView();
            _mixerTab.Rebuild();
        }
        finally
        {
            _refreshing = false;
        }
        _title.Text = (_projectPath != null ? Path.GetFileNameWithoutExtension(_projectPath) : "Untitled") + (_dirty ? " •" : "");
        Text = "Humble Baker Studio" + (_projectPath != null ? " — " + Path.GetFileName(_projectPath) : "");
    }

    public void SelectTrack(Track? track, bool fromList = false)
    {
        _selected = track;
        if (!fromList) _trackList.Select(track);
        _sequencer.SetTrack(track);
        _tracksTab.RefreshView();
        _effectsTab.RefreshView();
        _synthTab.RefreshView();
        _mixerTab.RefreshTrack();
        _toolsTab.RefreshView();
        _graphTab.RefreshView();
    }

    public void NotifyTracksChanged(Track? select = null)
    {
        _trackList.Rebuild();
        if (select != null) { _selected = select; _trackList.Select(select); }
        _sequencer.SetProject(_project, _selected);
        _mixerTab.Rebuild();
        _graphTab.RefreshView();
        MarkDirty();
    }

    public void MarkDirty()
    {
        _dirty = true;
        _title.Text = (_projectPath != null ? Path.GetFileNameWithoutExtension(_projectPath) : "Untitled") + " •";
        Text = "Humble Baker Studio" + (_projectPath != null ? " — " + Path.GetFileName(_projectPath) : "");
        _sequencer.RefreshRoll();
    }

    /// <summary>Records the state before a user edit. Safe to call repeatedly during a drag.</summary>
    public void BeginEdit(string label) => _undo.Record(_project, label);

    public void ResetHistory() => _undo.Reset(_project);

    public void Undo()
    {
        int index = _selected != null ? Math.Max(0, _project.Tracks.IndexOf(_selected)) : 0;
        var restored = _undo.Undo(_project);
        if (restored == null) { SetStatus("Nothing to undo"); return; }
        _project = restored;
        _selected = null;
        RefreshFromProject();
        _trackList.Select(Math.Clamp(index, 0, Math.Max(0, _project.Tracks.Count - 1)));
        _dirty = true;
        RefreshTitle();
        SetStatus("Undo");
    }

    public void Redo()
    {
        int index = _selected != null ? Math.Max(0, _project.Tracks.IndexOf(_selected)) : 0;
        var restored = _undo.Redo(_project);
        if (restored == null) { SetStatus("Nothing to redo"); return; }
        _project = restored;
        _selected = null;
        RefreshFromProject();
        _trackList.Select(Math.Clamp(index, 0, Math.Max(0, _project.Tracks.Count - 1)));
        _dirty = true;
        RefreshTitle();
        SetStatus("Redo");
    }

    public void RefreshTitle()
    {
        _title.Text = (_projectPath != null ? Path.GetFileNameWithoutExtension(_projectPath) : "Untitled") + (_dirty ? " •" : "");
        Text = "Humble Baker Studio" + (_projectPath != null ? " — " + Path.GetFileName(_projectPath) : "");
    }

    public void OpenLiveWindow()
    {
        if (_liveWindow == null || _liveWindow.IsDisposed)
        {
            _liveWindow = new LiveWindow(this);
            _liveWindow.FormClosed += (_, _) => _liveWindow = null;
            _liveWindow.Show(this);
        }
        else
        {
            _liveWindow.Activate();
            _liveWindow.BringToFront();
        }
    }

    public void SetStatus(string text) => _status.Text = text;
    public double PlayheadBeat => _sequencer.PlayheadBeat;
    public void SetPosition(string text) => _position.Text = text;
    public void SetProject(Project project, string? path)
    {
        _project = project;
        _projectPath = path;
        _dirty = false;
        RefreshFromProject();
        _trackList.Select(0);
        ResetHistory();
    }

    // ---- project files ---------------------------------------------------

    private void NewProject()
    {
        if (!ConfirmDiscard()) return;
        StopPlayback();
        _project = ProjectStore.CreateDefault();
        _projectPath = null;
        _dirty = false;
        RefreshFromProject();
        _trackList.Select(0);
        ResetHistory();
        SetStatus("New project");
    }

    private void OpenProject()
    {
        if (!ConfirmDiscard()) return;
        using var dlg = new OpenFileDialog
        {
            Filter = "Humble Baker project (*.json;*.hbstudio.json)|*.json;*.hbstudio.json|All files (*.*)|*.*",
            Title = "Open project"
        };
        if (!string.IsNullOrWhiteSpace(_settings.LastProject))
            dlg.InitialDirectory = Path.GetDirectoryName(_settings.LastProject);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            StopPlayback();
            _project = ProjectStore.Load(dlg.FileName);
            _projectPath = dlg.FileName;
            _dirty = false;
            _settings.LastProject = dlg.FileName;
            _settings.Save();
            RefreshFromProject();
            _trackList.Select(0);
            ResetHistory();
            SetStatus("Opened " + Path.GetFileName(dlg.FileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open that project:\n\n" + ex.Message, "Open project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool SaveProject(bool saveAs)
    {
        var path = _projectPath;
        if (saveAs || string.IsNullOrWhiteSpace(path))
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "Humble Baker project (*.hbstudio.json)|*.hbstudio.json|JSON (*.json)|*.json",
                Title = "Save project",
                FileName = string.IsNullOrWhiteSpace(path) ? "Untitled.hbstudio.json" : Path.GetFileName(path),
                InitialDirectory = string.IsNullOrWhiteSpace(path) ? _settings.ResolvedOutputDirectory : Path.GetDirectoryName(path)
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return false;
            path = dlg.FileName;
        }

        try
        {
            ProjectStore.Save(_project, path!);
            _projectPath = path;
            _dirty = false;
            _settings.LastProject = path!;
            _settings.Save();
            RefreshFromProject(keepSelection: true);
            SetStatus("Saved " + Path.GetFileName(path!));
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not save the project:\n\n" + ex.Message, "Save project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private bool ConfirmDiscard()
    {
        if (!_dirty) return true;
        return MessageBox.Show(this, "Save changes to this project first?", "Humble Baker Studio",
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) switch
        {
            DialogResult.Yes => SaveProject(false),
            DialogResult.No => true,
            _ => false
        };
    }

    // ---- transport -------------------------------------------------------

    public void TogglePlayback()
    {
        if (_player.IsPlaying)
        {
            StopPlayback();
            return;
        }
        Play();
    }

    private async void Play()
    {
        try
        {
            _playButton.Enabled = false;
            SetStatus("Rendering…");
            bool song = _song.Checked && _project.Song.Count > 0;
            bool loop = _loop.Checked;
            int repeats = loop && !song ? 8 : 1;
            var project = _project;
            bool master = _master.Checked;

            var pcm = await Task.Run(() => ExportService.RenderPcm(project, song, loop, master, repeats));
            if (pcm.Length < 4) { SetStatus("Nothing to play"); return; }

            _sequencer.SetPlayhead(0);
            _player.Play(pcm, Dsp.SR);
            _playButton.Text = "■  Playing";
            _playTimer.Start();
            SetStatus(song ? "Playing song" : "Playing pattern");
        }
        catch (Exception ex)
        {
            SetStatus("Playback failed");
            MessageBox.Show(this, ex.Message, "Playback", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _playButton.Enabled = true;
        }
    }

    public void StopPlayback()
    {
        _player.Stop();
        _playTimer.Stop();
        _sequencer.SetPlayhead(-1);
        _playButton.Text = "▶  Play";
        _playButton.Enabled = true;
        SetPosition("");
    }

    private void UpdatePlayhead()
    {
        double seconds = _player.Position.TotalSeconds;
        double spb = 60.0 / _project.Bpm;
        double beat = seconds / spb;
        if (_loop.Checked && _project.LoopBeats > 0)
            beat %= _project.LoopBeats;
        _sequencer.SetPlayhead(beat);
        SetPosition(DurationText.Format(_player.Position) + " / " + DurationText.Format(_player.Duration));
    }

    // ---- export ----------------------------------------------------------

    private void ExportProject()
    {
        using var dlg = new ExportDialog(this, _project, _settings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            SetStatus("Exported " + Path.GetFileName(dlg.ExportedPath));
        }
    }

    public void ApplySettings()
    {
        _ollama.BaseUrl = _settings.OllamaUrl.TrimEnd('/');
        _ai.Model = _settings.OllamaModel;
        SampleLibrary.Discover(_settings);
        _settings.Save();
    }

    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        try { _liveWindow?.Close(); } catch { }
        StopPlayback();
        if (!ConfirmDiscard())
        {
            e.Cancel = true;
            return;
        }
        try { _settings.LastProject = _projectPath ?? _settings.LastProject; _settings.Save(); } catch { }
        _ollama.Dispose();
        _player.Dispose();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && !(ActiveControl is TextBoxBase))
        {
            TogglePlayback();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.S)
        {
            SaveProject(e.Shift);
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.Z)
        {
            Undo();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.Y)
        {
            Redo();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.O)
        {
            OpenProject();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.N)
        {
            NewProject();
            e.Handled = true;
        }
    }
}
