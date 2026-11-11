using System.Diagnostics;
using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

internal sealed class SettingsTab : UserControl
{
    private readonly MainForm _host;
    private readonly TextBox _output = new() { Width = 360 };
    private readonly TextBox _ollama = new() { Width = 360 };
    private readonly TextBox _ffmpeg = new() { Width = 360 };
    private readonly TextBox _sc = new() { Width = 360 };
    private readonly CheckBox _openLast = new() { Text = "Reopen the last project on launch", AutoSize = true };
    private readonly ListBox _roots = new() { Width = 560, Height = 110, BackColor = Theme.PanelAlt, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle };
    private readonly Label _rootInfo = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };

    public SettingsTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        _output.Width = 560;
        _ollama.Width = 420;
        _ffmpeg.Width = 560;
        _sc.Width = 560;
        _roots.Width = 560;
        _roots.Height = 96;

        var page = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(18),
            BackColor = Theme.Back
        };

        var titleRow = new Panel { Width = 1000, Height = 44, BackColor = Theme.Back, Margin = new Padding(0, 0, 0, 8) };
        titleRow.Controls.Add(new Label { Text = "Settings", AutoSize = true, Font = Theme.Title, ForeColor = Theme.Text, Location = new Point(0, 4) });
        var saveTop = Theme.Button("Save settings", (_, _) => Save(), true);
        saveTop.Location = new Point(868, 2);
        titleRow.Controls.Add(saveTop);
        page.Controls.Add(titleRow);

        var folders = Theme.CardPanel("Folders and sample libraries");
        folders.Width = 1000;
        folders.Height = 268;
        var folderRows = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Card };
        folderRows.Controls.Add(Row("Renders and exports", _output, Theme.Button("Browse…", (_, _) => Browse(_output, true)), Theme.Button("Open", (_, _) => OpenFolder(_output.Text))));
        folderRows.Controls.Add(Row("Sample libraries", _roots, Theme.Button("Add…", (_, _) => AddRoot()), Theme.Button("Remove", (_, _) => RemoveRoot())));
        folderRows.Controls.Add(_rootInfo);
        folderRows.Controls.Add(Theme.Label("Sample roots contain DrumKits, OrchestralHits, GuitarHits and FoleyPercussion folders.", Theme.Muted, Theme.Small));
        folders.Controls.Add(folderRows);
        page.Controls.Add(folders);

        var ai = Theme.CardPanel("Local AI (Ollama)");
        ai.Width = 1000;
        ai.Height = 108;
        var aiRows = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Card };
        aiRows.Controls.Add(Row("Server URL", _ollama, Theme.Button("Test", (_, _) => TestOllama())));
        aiRows.Controls.Add(Theme.Label("Default: http://127.0.0.1:11434. The model is chosen on the AI tab.", Theme.Muted, Theme.Small));
        ai.Controls.Add(aiRows);
        page.Controls.Add(ai);

        var tools = Theme.CardPanel("Tools");
        tools.Width = 1000;
        tools.Height = 152;
        var toolRows = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Card };
        toolRows.Controls.Add(Row("FFmpeg", _ffmpeg, Theme.Button("Browse…", (_, _) => Browse(_ffmpeg, false)), Theme.Button("Detect", (_, _) => DetectFfmpeg())));
        toolRows.Controls.Add(Row("SuperCollider (sclang)", _sc, Theme.Button("Browse…", (_, _) => Browse(_sc, false))));
        toolRows.Controls.Add(Theme.Label("FFmpeg is only needed for MP3/OGG/FLAC export. SuperCollider is only needed for live SC sessions.", Theme.Muted, Theme.Small));
        tools.Controls.Add(toolRows);
        page.Controls.Add(tools);

        var startup = Theme.CardPanel("Startup and data");
        startup.Width = 1000;
        startup.Height = 116;
        var startRows = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Card };
        startRows.Controls.Add(_openLast);
        var dataRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Card, Margin = new Padding(0, 6, 0, 0) };
        dataRow.Controls.Add(Theme.Button("Open data folder", (_, _) => OpenFolder(AppSettings.DataDirectory)));
        dataRow.Controls.Add(Theme.Label("App data: " + AppSettings.DataDirectory, Theme.Muted, Theme.Small));
        startRows.Controls.Add(dataRow);
        startup.Controls.Add(startRows);
        page.Controls.Add(startup);

        Controls.Add(page);
        Load += (_, _) => LoadValues();
    }

    private static Control Row(string label, Control editor, params Control[] buttons)
    {
        var row = new Panel
        {
            Width = 950,
            Height = Math.Max(34, editor.Height + 8),
            BackColor = Theme.Card,
            Margin = new Padding(0, 2, 0, 2)
        };
        row.Controls.Add(new Label
        {
            Text = label,
            Width = 160,
            Height = 26,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            Location = new Point(0, 4)
        });
        editor.Left = 164;
        editor.Top = 2;
        row.Controls.Add(editor);
        int x = 172 + editor.Width;
        foreach (var b in buttons)
        {
            b.Left = x;
            b.Top = 2;
            row.Controls.Add(b);
            x += b.Width + 4;
        }
        return row;
    }
    private void LoadValues()
    {
        var s = _host.Settings;
        _output.Text = s.ResolvedOutputDirectory;
        _ollama.Text = s.OllamaUrl;
        _ffmpeg.Text = s.FfmpegPath;
        _sc.Text = s.SuperColliderPath;
        _openLast.Checked = s.OpenLastProject;
        _roots.Items.Clear();
        foreach (var r in s.SampleRoots) _roots.Items.Add(r);
        UpdateRootInfo();
    }

    private void Save()
    {
        var s = _host.Settings;
        s.OutputDirectory = _output.Text.Trim();
        s.OllamaUrl = _ollama.Text.Trim();
        s.FfmpegPath = _ffmpeg.Text.Trim();
        s.SuperColliderPath = _sc.Text.Trim();
        s.OpenLastProject = _openLast.Checked;
        s.SampleRoots = _roots.Items.Cast<string>().ToList();
        TryCreateDir(s.ResolvedOutputDirectory);
        _host.ApplySettings();
        UpdateRootInfo();
        MessageBox.Show(this, "Settings saved.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AddRoot()
    {
        using var dlg = new FolderBrowserDialog { Description = "Choose a folder containing DrumKits / orchestral / foley samples" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!_roots.Items.Contains(dlg.SelectedPath)) _roots.Items.Add(dlg.SelectedPath);
    }

    private void RemoveRoot()
    {
        if (_roots.SelectedIndex >= 0) _roots.Items.RemoveAt(_roots.SelectedIndex);
    }

    private void UpdateRootInfo()
    {
        var discovered = SampleLibrary.Discover(_host.Settings);
        var kits = SampleLibrary.KitDirectories(discovered);
        _rootInfo.Text = kits.Length == 0
            ? "No drum kit folders found; the procedural drum synths are used."
            : "Kits found: " + string.Join(", ", kits);
    }

    private async void TestOllama()
    {
        _host.Settings.OllamaUrl = _ollama.Text.Trim();
        _host.ApplySettings();
        var models = await _host.Ollama.ListModelsAsync();
        MessageBox.Show(this,
            models.Length == 0 ? "Could not reach Ollama at " + _host.Settings.OllamaUrl : "Connected. Models:\n\n" + string.Join("\n", models),
            "Ollama", MessageBoxButtons.OK, models.Length == 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void DetectFfmpeg()
    {
        var found = ExportService.FindFfmpeg(_ffmpeg.Text, AppSettings.DataDirectory);
        if (found == null)
        {
            MessageBox.Show(this, "FFmpeg was not found. Put ffmpeg.exe in the app folder, a Tools folder, or on PATH.", "FFmpeg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _ffmpeg.Text = found;
    }

    private void Browse(TextBox target, bool folder)
    {
        if (folder)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK) target.Text = dlg.SelectedPath;
        }
        else
        {
            using var dlg = new OpenFileDialog { Filter = "Programs (*.exe)|*.exe|All files (*.*)|*.*" };
            if (dlg.ShowDialog(this) == DialogResult.OK) target.Text = dlg.FileName;
        }
    }

    private void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void TryCreateDir(string dir)
    {
        try { Directory.CreateDirectory(dir); } catch { }
    }
}
