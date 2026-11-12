using HumbleBakerStudio.Services;
using HumbleBakerStudio.Tabs;
using HumbleBakerStudio.Audio;
using System.Diagnostics;
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
    private bool _loopRenderBusy;
    private bool _loopDirty;
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
    private readonly LibraryTab _libraryTab;
    private readonly SampleTab _sampleTab;
    private readonly EffectsTab _effectsTab;
    private readonly SynthTab _synthTab;
    private readonly GraphTab _graphTab;
    private readonly MasterTab _masterTab;
    private readonly MixerTab _mixerTab;
    private readonly AiTab _aiTab;
    private readonly SettingsTab _settingsTab;
    private readonly PluginTab _pluginTab;
    private readonly Vst3Host _vst = new();
    private Task? _vstScanTask;

    private readonly NumericUpDown _bpm = new() { Minimum = 30, Maximum = 300, DecimalPlaces = 2, Increment = 1, Width = 68 };
    private readonly NumericUpDown _bars = new() { Minimum = 1, Maximum = 64, Width = 52 };
    private readonly CheckBox _loop = new() { Text = "Loop", AutoSize = true, Checked = true };
    private readonly CheckBox _song = new() { Text = "Song", AutoSize = true };
    private readonly CheckBox _master = new() { Text = "Master", AutoSize = true };
    private readonly Button _playButton = Theme.Button("▶  Play", null, true);
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Muted };
    private readonly ToolStripStatusLabel _position = new() { ForeColor = Theme.Muted };
    private readonly ToolStripStatusLabel _title = new() { Alignment = ToolStripItemAlignment.Right, ForeColor = Theme.Muted, Font = Theme.Small };
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
        AudioPlugins.Host = _vst;
        SampleLibrary.Discover(_settings);

        _project = LoadInitialProject();

        _trackList = new TrackListPanel(this);
        _sequencer = new SequencerTab(this);
        _toolsTab = new ToolsTab(this);
        _arrangementTab = new ArrangementTab(this);
        _tracksTab = new TracksTab(this);
        _libraryTab = new LibraryTab(this);
        _sampleTab = new SampleTab(this);
        _effectsTab = new EffectsTab(this);
        _pluginTab = new PluginTab(this);
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
            Theme.ApplyDarkScrollbars(this);
            _vst.CatalogChanged += OnVstCatalogChanged;
            _ = EnsureVstScan();
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
    public Vst3Host Vst => _vst;

    /// <summary>Starts one shared VST3 scan; safe to call from any tab.</summary>
    public Task EnsureVstScan()
    {
        if (_vst.Catalog.Count > 0) return Task.CompletedTask;
        _vstScanTask ??= Task.Run(() =>
        {
            try { _vst.Scan(); }
            catch { }
        });
        return _vstScanTask;
    }

    private void OnVstCatalogChanged()
    {
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke(new Action(() =>
            {
                _tracksTab.ReloadInstruments();
                _sequencer.ReloadInstruments();
                _graphTab.RefreshVstChoices();
                _pluginTab.RefreshView();
            }));
        }
        catch { }
    }
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
        var menu = BuildMenu();
        MainMenuStrip = menu;

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = Theme.Panel,
            Padding = new Padding(10, 9, 8, 6),
            WrapContents = false,
            AutoScroll = false
        };

        top.Controls.Add(Separator());
        top.Controls.Add(Theme.Label("BPM"));
        top.Controls.Add(_bpm);
        top.Controls.Add(Theme.Label("Bars"));
        top.Controls.Add(_bars);
        top.Controls.Add(_loop);
        top.Controls.Add(_song);
        top.Controls.Add(Separator());
        top.Controls.Add(_playButton);
        var stop = Theme.Button("Stop", (_, _) => StopPlayback(), false, "Stop playback");
        stop.Width = 70;
        top.Controls.Add(stop);
        top.Controls.Add(Separator());
        top.Controls.Add(_master);
        top.Controls.Add(Theme.Button("Export…", (_, _) => ExportProject(), false, "Render and export audio (Ctrl+E)"));
        top.Controls.Add(Separator());
        var live = Theme.Button("LIVE", (_, _) => OpenLiveWindow(), true, "Two-deck live performance window");
        live.Font = Theme.UiBold;
        live.BackColor = Theme.AccentWarm;
        live.ForeColor = Theme.Back;
        live.FlatAppearance.BorderColor = Theme.AccentWarm;
        live.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 192, 124);
        top.Controls.Add(live);
        top.Controls.Add(Theme.Button("AI", (_, _) => SelectTab(_aiTab), false, "Local AI assistant"));
        top.Controls.Add(Theme.Button("Settings", (_, _) => SelectTab(_settingsTab), false, "Studio settings"));

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

        var status = new StatusStrip { Dock = DockStyle.Bottom, Height = 26 };
        _status.Text = "Ready";
        status.Items.Add(_status);
        status.Items.Add(new ToolStripStatusLabel("   "));
        status.Items.Add(_position);
        status.Items.Add(new ToolStripStatusLabel("   "));
        status.Items.Add(_title);
        Theme.StyleStatus(status);

        var sidebar = new Panel { Dock = DockStyle.Left, Width = 236, BackColor = Theme.Panel, Padding = new Padding(8) };
        _trackList.Dock = DockStyle.Fill;
        sidebar.Controls.Add(_trackList);

        _tabs.Dock = DockStyle.Fill;
        _tabs.TabPages.Add(Page("Sequencer", _sequencer));
        _tabs.TabPages.Add(Page("Tools", _toolsTab));
        _tabs.TabPages.Add(Page("Arrangement", _arrangementTab));
        _tabs.TabPages.Add(Page("Tracks", _tracksTab));
        _tabs.TabPages.Add(Page("Library", _libraryTab));
        _tabs.TabPages.Add(Page("Sample lab", _sampleTab));
        _tabs.TabPages.Add(Page("Effects", _effectsTab));
        _tabs.TabPages.Add(Page("Plugins", _pluginTab));
        _tabs.TabPages.Add(Page("Synth", _synthTab));
        _tabs.TabPages.Add(Page("Graph", _graphTab));
        _tabs.TabPages.Add(Page("Mixer", _mixerTab));
        _tabs.TabPages.Add(Page("Master", _masterTab));
        _tabs.TabPages.Add(Page("AI", _aiTab));
        _tabs.TabPages.Add(Page("Settings", _settingsTab));
        Theme.StyleTabs(_tabs);

        Controls.Add(_tabs);
        Controls.Add(sidebar);
        Controls.Add(status);
        Controls.Add(top);
        Controls.Add(menu);
        Theme.Apply(this);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");
        AddMenuItem(file, "New project", NewProject, Keys.Control | Keys.N);
        AddMenuItem(file, "Open project…", OpenProject, Keys.Control | Keys.O);
        AddMenuItem(file, "Save", () => SaveProject(false), Keys.Control | Keys.S);
        AddMenuItem(file, "Save as…", () => SaveProject(true), Keys.Control | Keys.Shift | Keys.S);
        AddSeparator(file);
        AddMenuItem(file, "Export audio…", ExportProject, Keys.Control | Keys.E);
        AddSeparator(file);
        AddMenuItem(file, "Exit", Close);

        var edit = new ToolStripMenuItem("Edit");
        var undo = AddMenuItem(edit, "Undo", Undo);
        undo.ShortcutKeyDisplayString = "Ctrl+Z";
        var redo = AddMenuItem(edit, "Redo", Redo);
        redo.ShortcutKeyDisplayString = "Ctrl+Y";
        AddSeparator(edit);
        var hint = new ToolStripMenuItem("Ctrl+Z / Ctrl+Y also work inside the piano roll") { Enabled = false };
        edit.DropDownItems.Add(hint);

        var view = new ToolStripMenuItem("View");
        AddMenuItem(view, "Sequencer", () => SelectTab(_sequencer));
        AddMenuItem(view, "Tools (sample, MIDI, spectral)", () => SelectTab(_toolsTab));
        AddMenuItem(view, "Arrangement", () => SelectTab(_arrangementTab));
        AddMenuItem(view, "Tracks", () => SelectTab(_tracksTab));
        AddMenuItem(view, "Effects", () => SelectTab(_effectsTab));
        AddMenuItem(view, "Synth", () => SelectTab(_synthTab));
        AddMenuItem(view, "Patch graph", () => SelectTab(_graphTab));
        AddMenuItem(view, "Mixer", () => SelectTab(_mixerTab));
        AddMenuItem(view, "Master", () => SelectTab(_masterTab));
        AddMenuItem(view, "AI", () => SelectTab(_aiTab));
        AddMenuItem(view, "Settings", () => SelectTab(_settingsTab));

        var tools = new ToolStripMenuItem("Tools");
        AddMenuItem(tools, "Live performance…", OpenLiveWindow);
        AddMenuItem(tools, "AI assistant", () => SelectTab(_aiTab));
        AddMenuItem(tools, "Settings", () => SelectTab(_settingsTab));

        var help = new ToolStripMenuItem("Help");
        AddMenuItem(help, "About Humble Baker Studio", ShowAbout);
        AddMenuItem(help, "Open repository", () => OpenUrl("https://github.com/Daiviey/Humble-Baker-Studio"));
        AddMenuItem(help, "C# port release", () => OpenUrl("https://github.com/Daiviey/Humble-Baker-Studio/releases/tag/csharp-port-v0.1.1"));

        menu.Items.Add(file);
        menu.Items.Add(edit);
        menu.Items.Add(view);
        menu.Items.Add(tools);
        menu.Items.Add(help);
        Theme.StyleMenu(menu);
        return menu;
    }

    private static ToolStripMenuItem AddMenuItem(ToolStripDropDownItem parent, string text, Action action, Keys shortcut = Keys.None)
    {
        var item = new ToolStripMenuItem(text);
        if (shortcut != Keys.None) item.ShortcutKeys = shortcut;
        item.Click += (_, _) => action();
        parent.DropDownItems.Add(item);
        return item;
    }

    private static void AddSeparator(ToolStripDropDownItem parent) => parent.DropDownItems.Add(new ToolStripSeparator());

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    private void ShowAbout()
    {
        using var about = new Form
        {
            Text = "About Humble Baker Studio",
            Size = new Size(480, 360),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.Back,
            ForeColor = Theme.Text,
            Font = Theme.Ui
        };
        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(24),
            AutoScroll = true
        };
        body.Controls.Add(new Label { Text = "Humble Baker Studio", AutoSize = true, Font = new Font("Segoe UI Semibold", 17f), ForeColor = Theme.Text });
        body.Controls.Add(new Label { Text = "C# port 0.1.0 — native local studio", AutoSize = true, ForeColor = Theme.Accent, Font = Theme.UiBold, Margin = new Padding(0, 2, 0, 12) });
        body.Controls.Add(new Label
        {
            Text = "Offline audio production: piano roll, synthesis, effects, mixing, mastering, sampling, MIDI, spectral synthesis, local AI and two-deck live performance.",
            AutoSize = false,
            Width = 410,
            Height = 54,
            ForeColor = Theme.Muted
        });
        body.Controls.Add(new Label { Text = "Project files use the same *.hbstudio.json format as the PowerShell studio.", AutoSize = false, Width = 410, Height = 34, ForeColor = Theme.Muted });
        body.Controls.Add(new Label { Text = "MIT licence. Built with .NET 8 and WinForms; no runtime packages beyond Windows.", AutoSize = false, Width = 410, Height = 34, ForeColor = Theme.Muted });
        var link = new LinkLabel
        {
            Text = "github.com/Daiviey/Humble-Baker-Studio",
            AutoSize = true,
            LinkColor = Theme.Accent,
            ActiveLinkColor = Theme.AccentWarm,
            Margin = new Padding(0, 12, 0, 0)
        };
        link.LinkClicked += (_, _) => OpenUrl("https://github.com/Daiviey/Humble-Baker-Studio");
        body.Controls.Add(link);
        about.Controls.Add(body);
        about.ShowDialog(this);
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
        Height = 30,
        BackColor = Theme.Border,
        Margin = new Padding(8, 3, 8, 3)
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
            _libraryTab.RefreshView();
            _sampleTab.RefreshView();
            _effectsTab.RefreshView();
            _pluginTab.RefreshView();
            _synthTab.RefreshView();
            _graphTab.RefreshView();
            _masterTab.RefreshView();
            _mixerTab.Rebuild();
            _vst.Trim(_project);
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
        _pluginTab.RefreshView();
    }

    public void NotifyTracksChanged(Track? select = null)
    {
        // The patch graph drives the mix when initialized; keep it in step with the track list.
        if (_project.Graph.Initialized)
        {
            foreach (var t in _project.Tracks) PatchGraphControl.AddTrackNode(_project, t);
            PatchGraphControl.PruneTrackNodes(_project);
        }
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
        ScheduleLoopRefresh();
    }

    /// <summary>Re-renders the loop in the background so newly added notes play on the next pass.</summary>
    private void ScheduleLoopRefresh()
    {
        if (!_player.IsPlaying || !_loop.Checked || _song.Checked) return;
        _loopDirty = true;
        if (_loopRenderBusy) return;
        _ = RefreshLoopAsync();
    }

    private async Task RefreshLoopAsync()
    {
        _loopRenderBusy = true;
        try
        {
            while (_loopDirty && _player.IsPlaying && _loop.Checked && !_song.Checked)
            {
                _loopDirty = false;
                _vst.CaptureAll(_project);
                var snapshot = ProjectStore.LoadFromJson(ProjectStore.SaveToJson(_project));
                bool master = _master.Checked;
                var pcm = await Task.Run(() => ExportService.RenderPcm(snapshot, false, true, master));
                if (_player.IsPlaying && _loop.Checked && !_song.Checked)
                {
                    _player.QueueBuffer(pcm);
                }
            }
        }
        catch
        {
            // Keep playback running if a refresh fails.
        }
        finally
        {
            _loopRenderBusy = false;
        }
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
            _vst.CaptureAll(_project);
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
            bool loopPattern = _loop.Checked && !song;
            _vst.CaptureAll(_project);
            var project = _project;
            bool master = _master.Checked;

            var pcm = await Task.Run(() => ExportService.RenderPcm(project, song, loopPattern, master));
            if (pcm.Length < 4) { SetStatus("Nothing to play"); return; }

            _loopDirty = false;
            _sequencer.SetPlayhead(0);
            _player.Play(pcm, Dsp.SR, loopPattern);
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
        _loopDirty = false;
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
        _vst.CaptureAll(_project);
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
        _vst.Dispose();
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
    }
}
