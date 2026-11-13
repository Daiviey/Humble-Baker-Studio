using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

/// <summary>
/// Deterministic MIDI pattern browser: choose an artist-facing role/style/mood and energy
/// parameters, then write a pre-programmed example (transposed, fitted and humanised). This is
/// the replacement for generative melody tools in the No-AI edition.
/// </summary>
internal sealed class PatternsTab : UserControl
{
    private readonly MainForm _host;
    private readonly ComboBox _role = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _style = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _mood = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _bars = new() { Minimum = 1, Maximum = 64, Value = 4, Width = 56 };
    private readonly NumericUpDown _energy = new() { Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.05m, Value = 0.6m, Width = 60 };
    private readonly NumericUpDown _complexity = new() { Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.05m, Value = 0.5m, Width = 60 };
    private readonly NumericUpDown _density = new() { Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.05m, Value = 0.6m, Width = 60 };
    private readonly NumericUpDown _seed = new() { Minimum = 0, Maximum = 999999, Value = 1, Width = 72 };
    private readonly CheckBox _replace = new() { Text = "Replace existing notes", AutoSize = true, Checked = true, ForeColor = Theme.Text };
    private readonly CheckBox _humanize = new() { Text = "Humanize", AutoSize = true, Checked = true, ForeColor = Theme.Text };
    private readonly CheckBox _snap = new() { Text = "Snap to project scale", AutoSize = true, Checked = true, ForeColor = Theme.Text };
    private readonly Label _target = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly Label _result = new() { Dock = DockStyle.Fill, ForeColor = Theme.Text, Font = Theme.Ui, Padding = new Padding(12) };
    private readonly ListBox _notes = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Theme.Back, ForeColor = Theme.Text };
    private PatternResult? _preview;

    public PatternsTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        _role.Items.AddRange(new object[] { "Drums", "Bassline", "Chords", "Lead", "Arpeggio", "Counter" });
        _role.SelectedIndex = 3;
        _style.Items.AddRange(new object[] { "Any", "Techno", "House", "Acid", "Breakbeat", "Hip-hop", "Bass", "Ambient", "Cinematic", "Funk" });
        _style.SelectedIndex = 0;
        _mood.Items.AddRange(new object[] { "Any", "Dark", "Uplifting", "Dreamy", "Aggressive", "Hypnotic", "Melancholic" });
        _mood.SelectedIndex = 0;

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 126,
            WrapContents = true,
            BackColor = Theme.Panel,
            Padding = new Padding(12, 8, 8, 4)
        };

        top.Controls.Add(Row(Theme.Label("Role", Theme.Muted), _role, Theme.Label("Style", Theme.Muted), _style, Theme.Label("Mood", Theme.Muted), _mood));
        top.Controls.Add(Row(Theme.Label("Bars", Theme.Muted), _bars, Theme.Label("Energy", Theme.Muted), _energy,
            Theme.Label("Complexity", Theme.Muted), _complexity, Theme.Label("Density", Theme.Muted), _density,
            Theme.Label("Seed", Theme.Muted), _seed));
        top.Controls.Add(Row(
            Theme.Button("Preview", (_, _) => PreviewPattern()),
            Theme.Button("Write to selected track", (_, _) => WritePattern(), true),
            Theme.Button("Random seed", (_, _) => _seed.Value = Random.Shared.Next(1, 999999)),
            _replace, _humanize, _snap));
        top.Controls.Add(_target);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(12),
            BackColor = Theme.Back
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        var infoCard = Theme.CardPanel("Selected example");
        infoCard.Dock = DockStyle.Fill;
        infoCard.Margin = new Padding(0, 0, 8, 0);
        infoCard.Controls.Add(_result);

        var noteCard = Theme.CardPanel("Preview notes");
        noteCard.Dock = DockStyle.Fill;
        noteCard.Margin = new Padding(8, 0, 0, 0);
        noteCard.Controls.Add(_notes);

        body.Controls.Add(infoCard, 0, 0);
        body.Controls.Add(noteCard, 1, 0);

        Controls.Add(body);
        Controls.Add(top);

        _result.Text = "Choose a role, style and mood, then Preview or Write. " +
                       "Every result comes from a fixed MIDI example library; no model or network is used.\r\n\r\n" +
                       "Energy changes velocity, density thins notes, complexity selects busier examples, " +
                       "and the seed makes the selection reproducible.";
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

    public void RefreshView()
    {
        var t = _host.SelectedTrack;
        _target.Text = t == null
            ? "Target: (no track selected)"
            : $"Target: {t.Name} ({t.Synth})  -  project key {NoteName(_host.Project.Key)} {_host.Project.Scale}, {_host.Project.Bars} bars";
    }

    private PatternRequest BuildRequest()
    {
        int seed = (int)_seed.Value;
        if (seed == 0) seed = Environment.TickCount & 0x7fffffff;
        return new PatternRequest
        {
            Role = _role.SelectedItem as string ?? "Lead",
            Style = _style.SelectedItem as string ?? "Any",
            Mood = _mood.SelectedItem as string ?? "Any",
            Bars = (int)_bars.Value,
            Energy = (double)_energy.Value,
            Complexity = (double)_complexity.Value,
            Density = (double)_density.Value,
            Seed = seed,
            Key = _host.Project.Key,
            Scale = _host.Project.Scale,
            BarBeats = _host.Project.BarBeats,
            SnapToScale = _snap.Checked,
            Humanize = _humanize.Checked
        };
    }

    private void PreviewPattern()
    {
        _preview = PatternComposer.Compose(BuildRequest());
        ShowPreview(_preview);
    }

    private void ShowPreview(PatternResult result)
    {
        _notes.Items.Clear();
        foreach (var note in result.Notes.Take(200))
            _notes.Items.Add($"{NoteName(note.Pitch)}  {note.Start:0.00}  len {note.Len:0.00}  vel {note.Vel}");
        if (result.Notes.Count > 200) _notes.Items.Add($"... {result.Notes.Count - 200} more notes");

        _result.Text = $"{result.Description}\r\n\r\nSeed: {result.Seed}\r\nNotes: {result.Notes.Count}\r\n" +
                       $"Role: {_role.SelectedItem}\r\nStyle: {_style.SelectedItem}\r\nMood: {_mood.SelectedItem}\r\n\r\n" +
                       "Preview only. Click Write to selected track to commit these notes.";
        _host.SetStatus($"Preview: {result.Name} ({result.Notes.Count} notes)");
    }

    private void WritePattern()
    {
        var t = _host.SelectedTrack;
        if (t == null) { _host.SetStatus("Select a track first."); return; }

        var result = PatternComposer.Compose(BuildRequest());
        if (result.Notes.Count == 0) { _host.SetStatus("The selected example produced no notes."); return; }

        _host.BeginEdit("pattern composer");
        if (_replace.Checked) t.Notes.Clear();
        t.Notes.AddRange(result.Notes);
        if (string.Equals(_role.SelectedItem as string, "Drums", StringComparison.OrdinalIgnoreCase))
            t.Synth = "drums";
        _host.MarkDirty();
        _host.NotifyTracksChanged(t);
        _preview = result;
        ShowPreview(result);
        _host.SetStatus($"Wrote {result.Notes.Count} notes from '{result.Name}' to '{t.Name}'.");
    }

    private static string NoteName(int midi)
    {
        string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int n = ((midi % 12) + 12) % 12;
        return names[n] + (midi / 12 - 1);
    }
}
