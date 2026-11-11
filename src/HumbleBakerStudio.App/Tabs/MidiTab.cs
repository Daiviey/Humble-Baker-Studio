using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

/// <summary>Live MIDI input, note monitor and quantized recording into the selected track.</summary>
internal sealed class MidiTab : UserControl
{
    private readonly MainForm _host;
    private readonly ComboBox _devices = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _quantize = new() { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
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
    private readonly Label _state = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.UiBold };
    private readonly Label _target = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private readonly System.Windows.Forms.Timer _poll = new() { Interval = 25 };

    private MidiInPort? _port;
    private bool _recording;
    private long _recordStart;
    private readonly Dictionary<int, (long Tick, int Vel)> _active = new();
    private readonly List<(int Pitch, double Start, double Len, int Vel)> _captured = new();

    public MidiTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, BackColor = Theme.Panel, Padding = new Padding(10, 7, 6, 4), WrapContents = false };
        bar.Controls.Add(Theme.Label("Input", Theme.Muted));
        bar.Controls.Add(_devices);
        bar.Controls.Add(Theme.Button("Open", (_, _) => OpenPort(), true));
        bar.Controls.Add(Theme.Button("Close", (_, _) => ClosePort()));
        bar.Controls.Add(Theme.Button("Refresh", (_, _) => LoadDevices()));
        bar.Controls.Add(Theme.Button("Record", (_, _) => ToggleRecord(), true));
        bar.Controls.Add(Theme.Label("Quantize", Theme.Muted));
        _quantize.Items.AddRange(new object[] { "Off", "1/4", "1/8", "1/16", "1/32" });
        _quantize.SelectedIndex = 2;
        bar.Controls.Add(_quantize);
        bar.Controls.Add(Theme.Button("Clear log", (_, _) => _log.Clear()));

        var info = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Theme.Panel, Padding = new Padding(12, 4, 4, 0) };
        info.Controls.Add(_state);
        info.Controls.Add(_target);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var help = Theme.Label(
            "Connect a controller, watch the live note/CC log, then press Record and play. " +
            "Notes are quantized to the selected grid and added to the current track when you press Record again to stop.",
            Theme.Muted, Theme.Small);
        help.Dock = DockStyle.Bottom;
        help.AutoSize = false;
        help.Height = 34;
        help.Padding = new Padding(0, 6, 0, 0);
        body.Controls.Add(_log);
        body.Controls.Add(help);

        Controls.Add(body);
        Controls.Add(info);
        Controls.Add(bar);

        _poll.Tick += (_, _) => Poll();
        Load += (_, _) => { LoadDevices(); UpdateState(); };
    }

    public void RefreshView() => UpdateState();

    private void LoadDevices()
    {
        var devices = MidiInPort.GetDevices();
        _devices.Items.Clear();
        if (devices.Length == 0)
        {
            _devices.Items.Add("(no MIDI input devices)");
            _devices.SelectedIndex = 0;
            _devices.Enabled = false;
            return;
        }
        _devices.Enabled = true;
        _devices.Items.AddRange(devices);
        _devices.SelectedIndex = 0;
    }

    private void OpenPort()
    {
        if (_devices.SelectedIndex < 0) return;
        var devices = MidiInPort.GetDevices();
        if (devices.Length == 0) { Log("No MIDI input devices found."); return; }

        _port ??= new MidiInPort();
        if (!_port.Open(_devices.SelectedIndex))
        {
            Log("Could not open the device: " + _port.LastError);
            return;
        }
        _poll.Start();
        Log("Opened " + _port.DeviceName + " (" + _port.DeviceIndex + ")");
        UpdateState();
    }

    private void ClosePort()
    {
        if (_recording) StopRecording();
        _poll.Stop();
        _port?.Close();
        Log("MIDI input closed.");
        UpdateState();
    }

    private void Poll()
    {
        if (_port == null || !_port.IsOpen) return;
        foreach (var e in _port.Drain())
        {
            int kind = e[0], d1 = e[1], d2 = e[2];
            if (kind == 1)
            {
                _active[d1] = (Environment.TickCount64, d2);
                Log($"note on  {AiTools.NoteName(d1)} ({d1})  vel {d2}");
            }
            else if (kind == 0)
            {
                if (_active.TryGetValue(d1, out var start))
                {
                    _active.Remove(d1);
                    if (_recording)
                    {
                        double startSec = (start.Tick - _recordStart) / 1000.0;
                        double lenSec = (Environment.TickCount64 - start.Tick) / 1000.0;
                        _captured.Add((d1, Math.Max(0, startSec), Math.Max(0.03, lenSec), start.Vel));
                    }
                }
                Log($"note off {AiTools.NoteName(d1)} ({d1})");
            }
            else if (kind == 2)
            {
                Log($"CC {d1} = {d2}");
            }
        }
    }

    private void ToggleRecord()
    {
        if (_recording) { StopRecording(); return; }
        if (_port == null || !_port.IsOpen)
        {
            MessageBox.Show(this, "Open a MIDI input device first.", "MIDI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_host.SelectedTrack == null)
        {
            MessageBox.Show(this, "Select a destination track first.", "MIDI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _captured.Clear();
        _active.Clear();
        _recording = true;
        _recordStart = Environment.TickCount64;
        _host.SetStatus("Recording MIDI into \"" + _host.SelectedTrack.Name + "\" — press Record again to stop");
        UpdateState();
    }

    private void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        // Close any notes that never received a note-off.
        long now = Environment.TickCount64;
        foreach (var (pitch, start) in _active)
            _captured.Add((pitch, Math.Max(0, (start.Tick - _recordStart) / 1000.0), Math.Max(0.03, (now - start.Tick) / 1000.0), start.Vel));
        _active.Clear();

        var track = _host.SelectedTrack;
        if (track == null || _captured.Count == 0)
        {
            _host.SetStatus("MIDI recording stopped — nothing captured");
            UpdateState();
            return;
        }

        double spb = 60.0 / _host.Project.Bpm;
        double grid = _quantize.SelectedIndex switch { 1 => 1.0, 2 => 0.5, 3 => 0.25, 4 => 0.125, _ => 0.0 };
        _host.BeginEdit("midi");
        foreach (var (pitch, start, len, vel) in _captured)
        {
            double s = start / spb;
            if (grid > 0) s = Math.Round(s / grid) * grid;
            double l = Math.Max(0.03, len / spb);
            track.Notes.Add(new Note(pitch, Math.Max(0, s), l, Math.Clamp(vel, 1, 127)));
        }
        _host.MarkDirty();
        _host.NotifyTracksChanged(track);
        _host.SetStatus($"Recorded {_captured.Count} MIDI note(s) into \"" + track.Name + "\"");
        Log($"Captured {_captured.Count} note(s) -> {track.Name}");
        UpdateState();
    }

    private void UpdateState()
    {
        bool open = _port?.IsOpen == true;
        _state.Text = _recording ? "● RECORDING" : open ? "Input ready" : "Input closed";
        _state.ForeColor = _recording ? Theme.Bad : open ? Theme.Good : Theme.Muted;
        var track = _host.SelectedTrack;
        _target.Text = track == null ? "" : "   target: " + track.Name + (track.Synth == "drums" ? " (drum map)" : "");
    }

    private void Log(string text)
    {
        if (_log.IsDisposed) return;
        if (_log.InvokeRequired) { _log.BeginInvoke(() => Log(text)); return; }
        if (_log.TextLength > 200_000) _log.Clear();
        _log.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}{Environment.NewLine}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _poll.Stop();
            _port?.Dispose();
        }
        base.Dispose(disposing);
    }
}
