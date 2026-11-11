using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

internal sealed class AiTab : UserControl
{
    private readonly MainForm _host;
    private readonly ComboBox _models = new() { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _endpoint = new() { Width = 250 };
    private readonly TextBox _style = new() { Width = 330, Text = "dark rolling techno, hypnotic, spacious" };
    private readonly NumericUpDown _bars = new() { Minimum = 1, Maximum = 32, Value = 4, Width = 60 };
    private readonly CheckBox _replace = new() { Text = "Replace existing notes", AutoSize = true, Checked = true };
    private readonly TextBox _log = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        BackColor = Theme.PanelAlt,
        ForeColor = Theme.Text,
        Font = Theme.Mono,
        BorderStyle = BorderStyle.FixedSingle
    };
    private readonly Label _busy = new() { AutoSize = true, ForeColor = Theme.Muted };
    private CancellationTokenSource? _cts;

    public AiTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 76, BackColor = Theme.Panel, Padding = new Padding(10, 6, 6, 4), WrapContents = true };
        top.Controls.Add(Theme.Label("Ollama", Theme.Muted, Theme.UiBold));
        top.Controls.Add(_endpoint);
        top.Controls.Add(Theme.Button("Save endpoint", (_, _) => SaveEndpoint()));
        top.Controls.Add(Theme.Button("Refresh models", (_, _) => RefreshModels()));
        top.Controls.Add(_models);
        top.Controls.Add(Theme.Label("Style / description", Theme.Muted, Theme.UiBold));
        top.Controls.Add(_style);
        top.Controls.Add(Theme.Label("Bars", Theme.Muted));
        top.Controls.Add(_bars);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.Panel, Padding = new Padding(10, 6, 6, 4), WrapContents = false };
        actions.Controls.Add(Theme.Button("Write melody / bass", (_, _) => GenerateNotes(false), true));
        actions.Controls.Add(Theme.Button("Write drum groove", (_, _) => GenerateNotes(true)));
        actions.Controls.Add(Theme.Button("Design synth patch", (_, _) => DesignPatch()));
        actions.Controls.Add(Theme.Button("Review mix", (_, _) => ReviewMix()));
        actions.Controls.Add(Theme.Button("Stop", (_, _) => Cancel()));
        actions.Controls.Add(_replace);
        actions.Controls.Add(_busy);

        var body = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 360,
            BackColor = Theme.Back,
            Panel1MinSize = 260
        };
        var help = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Back,
            ForeColor = Theme.Muted,
            Font = Theme.Ui,
            Text = "Local AI\r\n\r\n" +
                   "Everything here runs against your own Ollama server. No audio or project data leaves the machine.\r\n\r\n" +
                   "1. Pick a model (a 7B-14B instruct model is a good start).\r\n" +
                   "2. Choose a track in the list on the left.\r\n" +
                   "3. Describe the style and press an action.\r\n\r\n" +
                   "The model returns structured JSON. It is validated and clamped before it touches the project; generated notes can always be undone by loading the previous save.\r\n\r\n" +
                   "Tips:\r\n" +
                   "• Be concrete: genre, energy, instrumentation, reference artist.\r\n" +
                   "• Use shorter bars for motifs, longer for full sections.\r\n" +
                   "• Patch design works best for a track already using the patch synth."
        };
        body.Panel1.Controls.Add(help);
        body.Panel2.Controls.Add(_log);

        Controls.Add(body);
        Controls.Add(actions);
        Controls.Add(top);

        _endpoint.Text = _host.Settings.OllamaUrl;
        Load += (_, _) => RefreshModels();
    }

    private void SaveEndpoint()
    {
        _host.Settings.OllamaUrl = _endpoint.Text.Trim();
        _host.ApplySettings();
        _endpoint.Text = _host.Settings.OllamaUrl;
        Log("Endpoint saved: " + _host.Settings.OllamaUrl);
        RefreshModels();
    }

    private async void RefreshModels()
    {
        try
        {
            _busy.Text = "Checking Ollama…";
            var models = await _host.Ollama.ListModelsAsync();
            _models.Items.Clear();
            if (models.Length == 0)
            {
                Log("No models found. Is Ollama running at " + _host.Settings.OllamaUrl + "?");
                _busy.Text = "Ollama unavailable";
                return;
            }
            _models.Items.AddRange(models);
            var preferred = models.FirstOrDefault(m => m == _host.Settings.OllamaModel) ?? models[0];
            _models.SelectedItem = preferred;
            if (_host.Settings.OllamaModel != preferred)
            {
                _host.Settings.OllamaModel = preferred;
                _host.Ai.Model = preferred;
                _host.ApplySettings();
            }
            _busy.Text = $"{models.Length} model(s)";
            Log("Models: " + string.Join(", ", models));
        }
        catch (Exception ex)
        {
            _busy.Text = "Ollama unavailable";
            Log(ex.Message);
        }
    }

    private async void GenerateNotes(bool drums)
    {
        var track = _host.SelectedTrack;
        if (track == null) { Log("Select a track first."); return; }
        if (_models.SelectedItem is not string model) { Log("Choose an Ollama model first."); return; }
        _host.Ai.Model = model;
        _host.Settings.OllamaModel = model;
        _host.ApplySettings();

        _cts = new CancellationTokenSource();
        SetBusy(true, "Composing…");
        try
        {
            var notes = drums
                ? await _host.Ai.GenerateDrumsAsync(_host.Project, track, _style.Text, (int)_bars.Value, _cts.Token)
                : await _host.Ai.GenerateNotesAsync(_host.Project, track, _style.Text, (int)_bars.Value, _cts.Token);

            if (notes.Count == 0) { Log("The model returned no notes."); return; }
            _host.BeginEdit("ai");
            if (_replace.Checked) track.Notes.Clear();
            track.Notes.AddRange(notes);
            Log($"Added {notes.Count} notes to \"{track.Name}\" ({(drums ? "drums" : "melodic")}).");
            _host.MarkDirty();
            _host.NotifyTracksChanged(track);
            _host.SelectTrack(track, fromList: true);
            SetBusy(false, "Done");
        }
        catch (OperationCanceledException)
        {
            SetBusy(false, "Stopped");
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message);
            SetBusy(false, "Failed");
        }
    }

    private async void DesignPatch()
    {
        var track = _host.SelectedTrack;
        if (track == null) { Log("Select a track first."); return; }
        if (_models.SelectedItem is not string model) { Log("Choose an Ollama model first."); return; }
        _host.Ai.Model = model;

        _cts = new CancellationTokenSource();
        SetBusy(true, "Designing patch…");
        try
        {
            track.Patch ??= new Patch();
            if (track.Synth != "patch") track.Synth = "patch";
            _host.BeginEdit("ai");
            await _host.Ai.DesignPatchAsync(track.Patch, _style.Text, _cts.Token);
            Log("Patch updated for \"" + track.Name + "\".");
            _host.MarkDirty();
            _host.NotifyTracksChanged(track);
            SetBusy(false, "Done");
        }
        catch (OperationCanceledException) { SetBusy(false, "Stopped"); }
        catch (Exception ex) { Log("Error: " + ex.Message); SetBusy(false, "Failed"); }
    }

    private async void ReviewMix()
    {
        if (_models.SelectedItem is not string model) { Log("Choose an Ollama model first."); return; }
        _host.Ai.Model = model;
        _cts = new CancellationTokenSource();
        SetBusy(true, "Reviewing mix…");
        try
        {
            var notes = await _host.Ai.ReviewMixAsync(_host.Project, _cts.Token);
            Log(notes);
            SetBusy(false, "Done");
        }
        catch (OperationCanceledException) { SetBusy(false, "Stopped"); }
        catch (Exception ex) { Log("Error: " + ex.Message); SetBusy(false, "Failed"); }
    }

    private void Cancel()
    {
        _cts?.Cancel();
        SetBusy(false, "Stopping…");
    }

    private void SetBusy(bool busy, string text)
    {
        _busy.Text = text;
        _busy.ForeColor = busy ? Theme.AccentWarm : Theme.Muted;
    }

    private void Log(string text)
    {
        if (_log.IsDisposed) return;
        if (_log.InvokeRequired) { _log.BeginInvoke(() => Log(text)); return; }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }
}
