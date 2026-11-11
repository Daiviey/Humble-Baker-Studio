using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Tabs;

internal sealed class EffectsTab : UserControl
{
    private readonly MainForm _host;
    private readonly FlowLayoutPanel _knobs = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10), BackColor = Theme.Back };
    private readonly Label _heading = new() { Dock = DockStyle.Top, Height = 30, Padding = new Padding(12, 7, 0, 0), ForeColor = Theme.Text, Font = Theme.UiBold, BackColor = Theme.Panel };
    private readonly Label _hint = new() { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(12, 4, 8, 4), ForeColor = Theme.Muted, Font = Theme.Small, BackColor = Theme.Panel };
    private readonly Label _chainState = new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(6, 8, 8, 3) };
    private bool _building;

    public EffectsTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Panel, Padding = new Padding(10, 7, 6, 4), WrapContents = false };
        bar.Controls.Add(_chainState);
        bar.Controls.Add(Theme.Button("Randomize", (_, _) => Randomize(0.35)));
        bar.Controls.Add(Theme.Button("Reset chain", (_, _) => ResetChain()));
        bar.Controls.Add(Theme.Button("Copy to all tracks", (_, _) => CopyToAll()));
        bar.Controls.Add(Theme.Button("Reset all", (_, _) => ResetAll()));
        bar.Controls.Add(Theme.Label("Changes apply to the selected track only.", Theme.Muted, Theme.Small));

        _hint.Text = "Effects are per-track processors: filter, drive, delay, reverb, modulation, dynamics and the full 113-effect catalogue. " +
                     "Right-click a knob to reset it to its default.";

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
            _heading.Text = t == null ? "Effects — no track selected" : "Effects — " + t.Name;
            _chainState.Text = t == null ? "" : "Chain: " + (t.Fx.Active ? "active" : "off");
            if (t == null)
            {
                _knobs.Controls.Clear();
                return;
            }
            KnobPanel.Build(t.Fx, _knobs, (_, _) =>
            {
                if (_building) return;
                _host.BeginEdit("effects");
                _chainState.Text = "Chain: " + (t.Fx.Active ? "active" : "off");
                _host.MarkDirty();
            }, Theme.Back, Theme.Text, Theme.Accent);
            _knobs.AutoScrollPosition = new Point(0, 0);
        }
        finally
        {
            _building = false;
        }
    }

    private void Randomize(double amount)
    {
        var t = _host.SelectedTrack;
        if (t == null) return;
        _host.BeginEdit("effects");
        PropIO.Randomize(t.Fx, amount, Environment.TickCount);
        _host.MarkDirty();
        RefreshView();
    }

    private void ResetChain()
    {
        var t = _host.SelectedTrack;
        if (t == null) return;
        _host.BeginEdit("effects");
        t.Fx = new Fx();
        _host.MarkDirty();
        RefreshView();
    }

    private void CopyToAll()
    {
        var src = _host.SelectedTrack;
        if (src == null) return;
        _host.BeginEdit("effects");
        foreach (var t in _host.Project.Tracks)
            if (!ReferenceEquals(t, src)) CopyFx(src.Fx, t.Fx);
        _host.MarkDirty();
        _host.NotifyTracksChanged();
    }

    private void ResetAll()
    {
        if (MessageBox.Show(this, "Reset the effects chain on every track?", "Effects", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _host.BeginEdit("effects");
        foreach (var t in _host.Project.Tracks) t.Fx = new Fx();
        _host.MarkDirty();
        _host.NotifyTracksChanged();
    }

    private static void CopyFx(Fx from, Fx to)
    {
        foreach (var pi in typeof(Fx).GetProperties())
            if (pi.CanWrite && pi.GetIndexParameters().Length == 0)
                pi.SetValue(to, pi.GetValue(from));
    }
}
