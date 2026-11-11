using HumbleBakerStudio.Services;
using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio;

internal sealed class ExportDialog : Form
{
    private readonly Project _project;
    private readonly AppSettings _settings;

    private readonly RadioButton _pattern = new() { Text = "Pattern (loop)", AutoSize = true, Checked = true };
    private readonly RadioButton _song = new() { Text = "Song arrangement", AutoSize = true };
    private readonly NumericUpDown _repeats = new() { Minimum = 1, Maximum = 64, Value = 1, Width = 64 };
    private readonly ComboBox _format = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _normalize = new() { Text = "Normalize peak", AutoSize = true, Checked = true };
    private readonly NumericUpDown _targetDb = new() { Minimum = -12, Maximum = 0, DecimalPlaces = 1, Increment = 0.5m, Value = -1m, Width = 64 };
    private readonly NumericUpDown _fadeIn = new() { Minimum = 0, Maximum = 10000, Increment = 100, Width = 70 };
    private readonly NumericUpDown _fadeOut = new() { Minimum = 0, Maximum = 10000, Increment = 100, Width = 70 };
    private readonly TextBox _file = new() { Width = 500 };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Theme.Muted };
    private readonly Label _summary = new() { AutoSize = true, ForeColor = Theme.Accent, Font = Theme.Small, Margin = new Padding(0, 6, 0, 0) };
    private readonly Button _export = Theme.Button("Export", null, true);

    public string ExportedPath { get; private set; } = "";

    public ExportDialog(MainForm host, Project project, AppSettings settings)
    {
        _project = project;
        _settings = settings;

        Text = "Export audio";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = MaximizeBox = false;
        ClientSize = new Size(700, 470);
        BackColor = Theme.Back;
        ForeColor = Theme.Text;
        Font = Theme.Ui;

        var rows = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(16), AutoScroll = true };
        rows.Controls.Add(new Label { Text = "Export", AutoSize = true, Font = Theme.Title, Margin = new Padding(0, 0, 0, 10) });

        rows.Controls.Add(Section("WHAT TO RENDER"));
        var scope = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        scope.Controls.Add(_pattern);
        scope.Controls.Add(_song);
        rows.Controls.Add(scope);
        rows.Controls.Add(_summary);
        rows.Controls.Add(Inline("Repeats (pattern only)", _repeats));

        rows.Controls.Add(Section("FORMAT"));
        _format.Items.AddRange(ExportService.Formats);
        _format.SelectedIndex = 0;
        rows.Controls.Add(Inline("Format", _format));
        rows.Controls.Add(Inline("File", _file, Theme.Button("Browse…", (_, _) => Browse())));
        rows.Controls.Add(new Label
        {
            Text = "FFmpeg is required for MP3/OGG/FLAC; WAV always works.",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.Small
        });

        rows.Controls.Add(Section("MASTERING"));
        rows.Controls.Add(Inline("", _normalize));
        rows.Controls.Add(Inline("Target peak (dBFS)", _targetDb));
        rows.Controls.Add(Inline("Fade in / out (ms)", _fadeIn, _fadeOut));

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 8, 12, 8), BackColor = Theme.Panel };
        buttons.Controls.Add(Theme.Button("Cancel", (_, _) => { DialogResult = DialogResult.Cancel; Close(); }));
        buttons.Controls.Add(_export);
        _export.Click += (_, _) => DoExport();

        var bottomStatus = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Theme.Panel, Padding = new Padding(14, 4, 8, 0) };
        bottomStatus.Controls.Add(_status);

        Controls.Add(rows);
        Controls.Add(bottomStatus);
        Controls.Add(buttons);

        _song.Enabled = project.Song.Count > 0;
        if (!_song.Enabled) _song.Text = "Song arrangement (add patterns first)";
        _format.SelectedIndexChanged += (_, _) =>
        {
            if (_format.SelectedItem is ExportFormat f && !string.IsNullOrWhiteSpace(_file.Text))
                _file.Text = Path.ChangeExtension(_file.Text, f.Extension);
            UpdateSummary();
        };
        _song.CheckedChanged += (_, _) => UpdateSummary();
        _repeats.ValueChanged += (_, _) => UpdateSummary();
        Load += (_, _) => { BuildDefaultFileName(); UpdateSummary(); };
        UpdateSummary();
        Theme.Apply(this);
    }

    private static Label Section(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.Accent,
        Font = Theme.UiBold,
        Margin = new Padding(0, 14, 0, 4)
    };

    private static Control Inline(string label, params Control[] controls)
    {
        var p = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 2, 0, 2) };
        if (!string.IsNullOrEmpty(label))
            p.Controls.Add(new Label { Text = label, Width = 150, Height = 24, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Muted });
        foreach (var c in controls) p.Controls.Add(c);
        return p;
    }

    private void BuildDefaultFileName()
    {
        var name = "Untitled";
        try
        {
            if (!string.IsNullOrWhiteSpace(_settings.LastProject)) name = Path.GetFileNameWithoutExtension(_settings.LastProject);
        }
        catch { }
        _file.Text = Path.Combine(_settings.ResolvedOutputDirectory, ExportService.SanitizeFileName(name) + ".wav");
    }

    private void UpdateSummary()
    {
        double spb = 60.0 / Math.Max(1, _project.Bpm);
        double seconds = _song.Checked
            ? Arranger.SongBeats(_project) * spb
            : _project.LoopBeats * spb * Math.Max(1, (int)_repeats.Value);
        string scope = _song.Checked ? "Song arrangement" : "Pattern loop";
        _summary.Text = scope + "  ·  " + DurationText.Format(TimeSpan.FromSeconds(seconds)) + "  ·  " + (_format.SelectedItem as ExportFormat)?.Name;
    }

    private void Browse()
    {
        if (_format.SelectedItem is not ExportFormat f) return;
        using var dlg = new SaveFileDialog
        {
            Filter = $"{f.Name}|*{f.Extension}",
            FileName = Path.GetFileName(_file.Text),
            InitialDirectory = Path.GetDirectoryName(_file.Text)
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) _file.Text = dlg.FileName;
    }

    private async void DoExport()
    {
        if (_format.SelectedItem is not ExportFormat format) return;
        if (string.IsNullOrWhiteSpace(_file.Text)) { _status.Text = "Choose a file name."; return; }

        try
        {
            _export.Enabled = false;
            _status.Text = "Rendering…";
            var path = _file.Text.Trim();
            bool song = _song.Checked;
            int repeats = (int)_repeats.Value;
            bool normalize = _normalize.Checked;
            double target = (double)_targetDb.Value;
            int fadeIn = (int)_fadeIn.Value;
            int fadeOut = (int)_fadeOut.Value;
            var project = _project;
            string? ffmpeg = ExportService.FindFfmpeg(_settings.FfmpegPath, AppSettings.DataDirectory);

            await Task.Run(() => ExportService.Export(project, path, format, song, loop: !song, repeats,
                normalize, target, fadeIn, fadeOut, ffmpeg));

            ExportedPath = path;
            _status.Text = "Done: " + Path.GetFileName(path);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _status.Text = "Export failed";
            MessageBox.Show(this, ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _export.Enabled = true;
        }
    }
}
