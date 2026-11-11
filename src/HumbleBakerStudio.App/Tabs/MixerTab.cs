using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

/// <summary>Channel-strip mixer. Every strip edits the same live Track object as the sequencer.</summary>
internal sealed class MixerTab : UserControl
{
    private readonly MainForm _host;
    private readonly FlowLayoutPanel _strips = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = false,
        Padding = new Padding(10),
        BackColor = Theme.Back
    };
    private readonly Label _master = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small };
    private bool _building;

    public MixerTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Panel, Padding = new Padding(10, 7, 6, 4), WrapContents = false };
        bar.Controls.Add(Theme.Button("Add track", (_, _) => AddTrack()));
        bar.Controls.Add(Theme.Button("Analyze mix", (_, _) => AnalyzeMix()));
        bar.Controls.Add(Theme.Button("AI mix notes", (_, _) => AiMixNotes()));
        bar.Controls.Add(Theme.Button("Normalize levels", (_, _) => NormalizeLevels()));
        bar.Controls.Add(_master);

        Controls.Add(_strips);
        Controls.Add(bar);
    }

    public void Rebuild()
    {
        _building = true;
        try
        {
            _strips.SuspendLayout();
            foreach (Control c in _strips.Controls.Cast<Control>().ToList()) { _strips.Controls.Remove(c); c.Dispose(); }
            foreach (var t in _host.Project.Tracks) _strips.Controls.Add(Strip(t));
            _strips.Controls.Add(MasterStrip());
            _strips.ResumeLayout();
            _master.Text = $"Master: {( _host.Project.Master.Enabled ? "on" : "off" )} · {_host.Project.Tracks.Count} tracks";
        }
        finally
        {
            _building = false;
        }
    }

    public void RefreshTrack()
    {
        // The strips bind directly to the live track objects, so a repaint is enough.
        _strips.Invalidate(true);
    }

    private Control Strip(Track t)
    {
        var p = new Panel { Width = 104, Height = 430, BackColor = Theme.Panel, Margin = new Padding(4), Padding = new Padding(6) };
        var name = new Label
        {
            Text = t.Name,
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.UiBold,
            ForeColor = Theme.Text,
            AutoEllipsis = true
        };
        var volLabel = new Label { Text = "Vol " + t.Vol.ToString("0.00"), Dock = DockStyle.Top, Height = 18, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Muted, Font = Theme.Small };
        var vol = new TrackBar
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 200,
            TickFrequency = 50,
            Value = Math.Clamp((int)Math.Round(t.Vol * 100), 0, 200),
            Dock = DockStyle.Fill,
            BackColor = Theme.Panel
        };
        vol.ValueChanged += (_, _) =>
        {
            if (_building) return;
            t.Vol = vol.Value / 100.0;
            volLabel.Text = "Vol " + t.Vol.ToString("0.00");
            _host.MarkDirty();
        };

        var panLabel = new Label { Text = PanText(t.Pan), Dock = DockStyle.Bottom, Height = 18, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Muted, Font = Theme.Small };
        var pan = new TrackBar
        {
            Minimum = -100,
            Maximum = 100,
            TickFrequency = 50,
            Value = Math.Clamp((int)Math.Round(t.Pan * 100), -100, 100),
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = Theme.Panel
        };
        pan.ValueChanged += (_, _) =>
        {
            if (_building) return;
            t.Pan = pan.Value / 100.0;
            panLabel.Text = PanText(t.Pan);
            _host.MarkDirty();
        };

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 30, WrapContents = false };
        var mute = Theme.Button("M", (_, _) => { t.Mute = !t.Mute; _host.MarkDirty(); _host.NotifyTracksChanged(); });
        var solo = Theme.Button("S", (_, _) => { t.Solo = !t.Solo; _host.MarkDirty(); _host.NotifyTracksChanged(); });
        mute.BackColor = t.Mute ? Theme.AccentWarm : Theme.PanelAlt;
        solo.BackColor = t.Solo ? Theme.Good : Theme.PanelAlt;
        mute.Width = solo.Width = 38;
        buttons.Controls.Add(mute);
        buttons.Controls.Add(solo);

        var bus = new ComboBox { Dock = DockStyle.Bottom, DropDownStyle = ComboBoxStyle.DropDownList, Height = 22, BackColor = Theme.PanelAlt, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat };
        bus.Items.Add("Master");
        foreach (var b in _host.Project.Buses) bus.Items.Add(b.Name);
        bus.SelectedIndex = Math.Clamp(t.Bus, 0, bus.Items.Count - 1);
        bus.SelectedIndexChanged += (_, _) =>
        {
            if (_building) return;
            t.Bus = bus.SelectedIndex;
            _host.MarkDirty();
        };

        var fx = new Label
        {
            Text = t.Fx.Active ? "FX on" : "FX off",
            Dock = DockStyle.Bottom,
            Height = 18,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = t.Fx.Active ? Theme.Good : Theme.Muted,
            Font = Theme.Small
        };

        p.Controls.Add(vol);
        p.Controls.Add(volLabel);
        p.Controls.Add(name);
        p.Controls.Add(pan);
        p.Controls.Add(panLabel);
        p.Controls.Add(buttons);
        p.Controls.Add(bus);
        p.Controls.Add(fx);

        p.MouseDown += (_, _) => _host.SelectTrack(t, fromList: true);
        return p;
    }

    private Control MasterStrip()
    {
        var m = _host.Project.Master;
        var p = new Panel { Width = 120, Height = 430, BackColor = Theme.PanelAlt, Margin = new Padding(4), Padding = new Padding(8) };
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.PanelAlt
        };
        stack.Controls.Add(new Label { Text = "MASTER", AutoSize = true, Font = Theme.Title, ForeColor = Theme.Accent, Margin = new Padding(0, 6, 0, 10) });
        stack.Controls.Add(new Label
        {
            Text = m.Enabled ? "Chain on" : "Chain off",
            AutoSize = true,
            ForeColor = m.Enabled ? Theme.Good : Theme.Muted,
            Margin = new Padding(0, 0, 0, 12)
        });
        stack.Controls.Add(new Label
        {
            Text = $"Target\n{m.TargetLufs:0.#} LUFS",
            AutoSize = true,
            ForeColor = Theme.Text,
            Margin = new Padding(0, 0, 0, 12)
        });
        stack.Controls.Add(new Label
        {
            Text = $"Peak ceiling\n{m.Ceiling:0.#} dB",
            AutoSize = true,
            ForeColor = Theme.Text,
            Margin = new Padding(0, 0, 0, 12)
        });
        stack.Controls.Add(new Label
        {
            Text = "Open the Master tab for the full chain and analysis.",
            AutoSize = false,
            Width = 100,
            Height = 70,
            ForeColor = Theme.Muted,
            Font = Theme.Small
        });
        p.Controls.Add(stack);
        return p;
    }

    private static string PanText(double pan) =>
        Math.Abs(pan) < 0.02 ? "Pan C" : pan < 0 ? $"L {Math.Abs(pan) * 100:0}" : $"R {pan * 100:0}";

    private void AddTrack()
    {
        var t = ProjectStore.NewTrack("Track " + (_host.Project.Tracks.Count + 1), "acid");
        _host.Project.Tracks.Add(t);
        _host.NotifyTracksChanged(t);
    }

    private void NormalizeLevels()
    {
        // Render once, estimate each track's contribution, then nudge volumes toward a common target.
        try
        {
            var project = _host.Project;
            var result = Task.Run(() =>
            {
                var levels = new List<(Track Track, double Rms)>();
                foreach (var t in project.Tracks)
                {
                    if (t.Mute) continue;
                    var (l, r) = ExportService.Render(project, song: false, loop: true, master: false);
                    double sum = 0;
                    foreach (var v in l) sum += v * v;
                    double rms = Math.Sqrt(sum / Math.Max(1, l.Length));
                    levels.Add((t, rms));
                }
                return levels;
            }).Result;

            double avg = result.Where(x => x.Rms > 1e-6).Select(x => x.Rms).DefaultIfEmpty(0).Average();
            if (avg <= 1e-6) return;
            foreach (var (t, rms) in result)
            {
                if (rms <= 1e-6) continue;
                double gain = Math.Clamp(avg / rms, 0.5, 2.0);
                t.Vol = Math.Clamp(t.Vol * gain, 0, 2);
            }
            _host.MarkDirty();
            _host.NotifyTracksChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Normalize levels", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AnalyzeMix()
    {
        try
        {
            var project = _host.Project;
            var report = Task.Run(() =>
            {
                var sb = new System.Text.StringBuilder();
                foreach (var t in project.Tracks)
                {
                    var (l, r) = ExportService.Render(project, song: false, loop: true, master: false);
                    double peak = 0, sum = 0;
                    for (int i = 0; i < l.Length; i++)
                    {
                        peak = Math.Max(peak, Math.Max(Math.Abs(l[i]), Math.Abs(r[i])));
                        sum += l[i] * l[i] + r[i] * r[i];
                    }
                    double rms = Math.Sqrt(sum / Math.Max(1, l.Length * 2));
                    sb.AppendLine($"{t.Name}: peak {Db(peak):0.0} dBFS, RMS {Db(rms):0.0} dBFS, pan {t.Pan:0.00}, bus {(t.Bus == 0 ? "master" : t.Bus.ToString())}");
                }
                return sb.ToString();
            }).Result;
            MessageBox.Show(this, report, "Mix analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Mix analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static double Db(double v) => 20 * Math.Log10(Math.Max(v, 1e-9));

    private async void AiMixNotes()
    {
        try
        {
            _master.Text = "AI: listening…";
            var notes = await _host.Ai.ReviewMixAsync(_host.Project, CancellationToken.None);
            _master.Text = "AI mix notes ready";
            MessageBox.Show(this, notes, "AI mix notes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _master.Text = "AI mix notes failed";
            MessageBox.Show(this, ex.Message, "AI mix notes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
