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

        var rows = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(18) };
        rows.Controls.Add(new Label { Text = "Settings", AutoSize = true, Font = Theme.Title, ForeColor = Theme.Text, Margin = new Padding(0, 0, 0, 12) });

        rows.Controls.Add(Section("FOLDERS"));
        rows.Controls.Add(Field("Renders and exports", _output, Theme.Button("Browse…", (_, _) => Browse(_output, true)), Theme.Button("Open", (_, _) => OpenFolder(_output.Text))));
        rows.Controls.Add(Field("Sample libraries", _roots, Theme.Button("Add…", (_, _) => AddRoot()), Theme.Button("Remove", (_, _) => RemoveRoot())));
        rows.Controls.Add(_rootInfo);
        rows.Controls.Add(Theme.Label("Sample roots contain DrumKits, OrchestralHits, GuitarHits and FoleyPercussion folders.", Theme.Muted, Theme.Small));

        rows.Controls.Add(Section("LOCAL AI (OLLAMA)"));
        rows.Controls.Add(Field("Server URL", _ollama, Theme.Button("Test", (_, _) => TestOllama())));
        rows.Controls.Add(Theme.Label("Default: http://127.0.0.1:11434. The model is chosen on the AI tab.", Theme.Muted, Theme.Small));

        rows.Controls.Add(Section("TOOLS"));
        rows.Controls.Add(Field("FFmpeg", _ffmpeg, Theme.Button("Browse…", (_, _) => Browse(_ffmpeg, false)), Theme.Button("Detect", (_, _) => DetectFfmpeg())));
        rows.Controls.Add(Field("SuperCollider (sclang)", _sc, Theme.Button("Browse…", (_, _) => Browse(_sc, false))));
        rows.Controls.Add(Theme.Label("FFmpeg is only needed for MP3/OGG/FLAC export. SuperCollider is only needed for live SC sessions.", Theme.Muted, Theme.Small));

        rows.Controls.Add(Section("STARTUP"));
        rows.Controls.Add(_openLast);

        var save = Theme.Button("Save settings", (_, _) => Save(), true);
        rows.Controls.Add(save);
        rows.Controls.Add(Theme.Button("Open data folder", (_, _) => OpenFolder(AppSettings.DataDirectory)));
        rows.Controls.Add(Theme.Label("App data: " + AppSettings.DataDirectory, Theme.Muted, Theme.Small));

        Controls.Add(rows);
        Load += (_, _) => LoadValues();
    }

    private static Label Section(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.Accent,
        Font = Theme.UiBold,
        Margin = new Padding(0, 16, 0, 6)
    };

    private static Control Field(string label, Control editor, params Control[] buttons)
    {
        var p = new Panel { Width = 900, Height = Math.Max(30, editor.Height + 8), Margin = new Padding(0, 1, 0, 1), BackColor = Theme.Back };
        p.Controls.Add(new Label { Text = label, Width = 170, Height = 26, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Muted });
        editor.Left = 174;
        editor.Top = 2;
        p.Controls.Add(editor);
        int x = 182 + editor.Width;
        foreach (var b in buttons)
        {
            b.Left = x;
            b.Top = 1;
            p.Controls.Add(b);
            x += b.Width + 4;
        }
        return p;
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
