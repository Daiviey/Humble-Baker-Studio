using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

internal sealed class SynthTab : UserControl
{
    private readonly MainForm _host;
    private readonly FlowLayoutPanel _knobs = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10), BackColor = Theme.Back };
    private readonly Label _heading = new() { Dock = DockStyle.Top, Height = 30, Padding = new Padding(12, 7, 0, 0), ForeColor = Theme.Text, Font = Theme.UiBold, BackColor = Theme.Panel };
    private readonly Label _hint = new() { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(12, 4, 8, 4), ForeColor = Theme.Muted, Font = Theme.Small, BackColor = Theme.Panel };
    private bool _building;

    public SynthTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Panel, Padding = new Padding(10, 7, 6, 4), WrapContents = false };
        bar.Controls.Add(Theme.Button("Convert to patch synth", (_, _) => ConvertToPatch()));
        bar.Controls.Add(Theme.Button("Randomize", (_, _) => Randomize(0.3)));
        bar.Controls.Add(Theme.Button("Reset patch", (_, _) => ResetPatch()));
        bar.Controls.Add(Theme.Button("Open Effects", (_, _) => _host.SelectTabByContent("Effects")));

        _hint.Text = "The patch synth is the deep voice: oscillators, unison, FM, filter, envelopes, modulation, wavetable and distortion. " +
                     "Every parameter is bounded and saved with the project.";

        Controls.Add(_knobs);
        Controls.Add(bar);
        Controls.Add(_heading);
        Controls.Add(_hint);
    }

    public void RefreshView()
    {
        var t = _host.SelectedTrack;
        _building = true;
        try
        {
            if (t == null)
            {
                _heading.Text = "Synth — no track selected";
                _knobs.Controls.Clear();
                return;
            }
            _heading.Text = "Synth — " + t.Name + " (" + t.Synth + ")";
            if (t.Synth != "patch")
            {
                _knobs.Controls.Clear();
                var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, BackColor = Theme.Back };
                panel.Controls.Add(Theme.Label($"This track uses the built-in \"{t.Synth}\" voice.", Theme.Text, Theme.UiBold));
                panel.Controls.Add(Theme.Label($"Cutoff {t.Cutoff:0} Hz · resonance {t.Res:0.##} · envelope {t.EnvAmt:0.##}", Theme.Muted));
                panel.Controls.Add(Theme.Label($"Attack {t.Atk:0.###} s · release {t.Rel:0.###} s · crush {t.Crush:0} bits", Theme.Muted));
                panel.Controls.Add(Theme.Label("Convert it to a patch synth to edit oscillators, filters and modulation.", Theme.Muted));
                panel.Controls.Add(Theme.Button("Convert to patch synth", (_, _) => ConvertToPatch()));
                _knobs.Controls.Add(panel);
                return;
            }

            t.Patch ??= new Patch();
            KnobPanel.Build(t.Patch, _knobs, (_, _) =>
            {
                if (_building) return;
                _host.MarkDirty();
            }, Theme.Back, Theme.Text, Theme.Accent);
            _knobs.AutoScrollPosition = new Point(0, 0);
        }
        finally
        {
            _building = false;
        }
    }

    private void ConvertToPatch()
    {
        var t = _host.SelectedTrack;
        if (t == null) return;
        if (t.Synth != "patch")
        {
            var patch = new Patch();
            // Carry over the simple track controls so the sound does not jump.
            patch.Cutoff = t.Cutoff;
            patch.Resonance = t.Res;
            patch.FilterEnv = t.EnvAmt;
            patch.Attack = t.Atk;
            patch.Release = t.Rel;
            patch.Bits = t.Crush;
            patch.Gain = Math.Clamp(t.Vol, 0, 1.5);
            t.Patch = patch;
            t.Synth = "patch";
            _host.MarkDirty();
            _host.NotifyTracksChanged(t);
            _host.SelectTrack(t, fromList: true);
        }
    }

    private void Randomize(double amount)
    {
        var t = _host.SelectedTrack;
        if (t?.Patch == null) return;
        PropIO.Randomize(t.Patch, amount, Environment.TickCount);
        _host.MarkDirty();
        RefreshView();
    }

    private void ResetPatch()
    {
        var t = _host.SelectedTrack;
        if (t == null) return;
        t.Patch = new Patch();
        _host.MarkDirty();
        RefreshView();
    }
}
