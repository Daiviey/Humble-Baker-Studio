using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio;

/// <summary>The left-hand track list. Mute/solo badges are clickable, matching the original studio.</summary>
internal sealed class TrackListPanel : UserControl
{
    private readonly MainForm _host;
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 42, BorderStyle = BorderStyle.None, BackColor = Theme.Panel, ForeColor = Theme.Text, IntegralHeight = false };
    private readonly Label _count = new() { AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Small, Margin = new Padding(4, 2, 4, 4) };

    public TrackListPanel(MainForm host)
    {
        _host = host;
        BackColor = Theme.Panel;
        Padding = new Padding(0);

        var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.Panel };
        var title = new Label { Text = "TRACKS", Dock = DockStyle.Top, Height = 22, ForeColor = Theme.Muted, Font = Theme.UiBold, Padding = new Padding(4, 4, 0, 0) };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 34, WrapContents = false, BackColor = Theme.Panel };
        buttons.Controls.Add(Small("+", (_, _) => AddTrack()));
        buttons.Controls.Add(Small("Dup", (_, _) => DuplicateTrack()));
        buttons.Controls.Add(Small("Del", (_, _) => DeleteTrack()));
        _count.Dock = DockStyle.Bottom;
        _count.Height = 18;
        header.Controls.Add(buttons);
        header.Controls.Add(title);

        Controls.Add(_list);
        Controls.Add(_count);
        Controls.Add(header);

        _list.SelectedIndexChanged += (_, _) =>
        {
            if (_list.SelectedIndex >= 0 && _list.SelectedIndex < _host.Project.Tracks.Count)
                _host.SelectTrack(_host.Project.Tracks[_list.SelectedIndex], fromList: true);
        };
        _list.MouseDown += OnListMouseDown;
        _list.DrawItem += DrawItem;
    }

    private static Button Small(string text, EventHandler click)
    {
        var b = Theme.Button(text, click);
        b.Padding = new Padding(6, 1, 6, 1);
        b.Font = Theme.Small;
        b.Margin = new Padding(2);
        return b;
    }

    public void Rebuild()
    {
        int keep = _list.SelectedIndex;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var t in _host.Project.Tracks) _list.Items.Add(t);
        _list.EndUpdate();
        _count.Text = $"{_host.Project.Tracks.Count} track(s) · {_host.Project.Bars} bars";
        if (keep >= 0 && keep < _list.Items.Count) _list.SelectedIndex = keep;
        _list.Invalidate();
    }

    public void Select(int index)
    {
        if (index >= 0 && index < _list.Items.Count) _list.SelectedIndex = index;
    }

    public void Select(Track? track)
    {
        if (track == null) return;
        int i = _host.Project.Tracks.IndexOf(track);
        if (i >= 0 && _list.SelectedIndex != i) _list.SelectedIndex = i;
    }

    private void AddTrack()
    {
        _host.BeginEdit("tracks");
        var t = ProjectStore.NewTrack("Track " + (_host.Project.Tracks.Count + 1), "acid");
        _host.Project.Tracks.Add(t);
        _host.NotifyTracksChanged(t);
    }

    private void DuplicateTrack()
    {
        var src = _host.SelectedTrack;
        if (src == null) return;
        _host.BeginEdit("tracks");
        var t = ProjectStore.NewTrack(src.Name + " copy", src.Synth);
        t.Vol = src.Vol; t.Pan = src.Pan; t.Cutoff = src.Cutoff; t.Res = src.Res; t.EnvAmt = src.EnvAmt;
        t.Atk = src.Atk; t.Rel = src.Rel; t.Crush = src.Crush; t.Bus = src.Bus; t.DrumKit = src.DrumKit;
        t.Patch = src.Patch?.Clone()!;
        t.Notes = src.Notes.Select(n => n.Clone()).ToList();
        _host.Project.Tracks.Add(t);
        _host.NotifyTracksChanged(t);
    }

    private void DeleteTrack()
    {
        var t = _host.SelectedTrack;
        if (t == null || _host.Project.Tracks.Count <= 1) return;
        if (MessageBox.Show(this, $"Delete track \"{t.Name}\"?", "Tracks", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _host.BeginEdit("tracks");
        int i = _host.Project.Tracks.IndexOf(t);
        _host.Project.Tracks.Remove(t);
        foreach (var pattern in _host.Project.Patterns) pattern.RemoveTrack(i);
        _host.NotifyTracksChanged(_host.Project.Tracks[Math.Min(i, _host.Project.Tracks.Count - 1)]);
    }

    private void OnListMouseDown(object? sender, MouseEventArgs e)
    {
        int i = _list.IndexFromPoint(e.Location);
        if (i < 0 || i >= _host.Project.Tracks.Count) return;
        var t = _host.Project.Tracks[i];
        int w = _list.ClientSize.Width;
        if (e.X >= w - 52 && e.X < w - 26) { _host.BeginEdit("tracks"); t.Mute = !t.Mute; MarkChanged(t); }
        else if (e.X >= w - 26) { _host.BeginEdit("tracks"); t.Solo = !t.Solo; MarkChanged(t); }
    }

    private void MarkChanged(Track t)
    {
        _host.MarkDirty();
        _list.Invalidate();
        _host.SelectTrack(t, fromList: true);
    }

    private void DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _host.Project.Tracks.Count) return;
        var t = _host.Project.Tracks[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using var back = new SolidBrush(selected ? Theme.PanelAlt : Theme.Panel);
        e.Graphics.FillRectangle(back, e.Bounds);
        if (selected)
        {
            using var stripe = new SolidBrush(Theme.Accent);
            e.Graphics.FillRectangle(stripe, e.Bounds.Left, e.Bounds.Top + 4, 3, e.Bounds.Height - 8);
        }

        using (var dot = new SolidBrush(SynthColor(t.Synth)))
            e.Graphics.FillRectangle(dot, e.Bounds.Left + 8, e.Bounds.Top + 12, 4, e.Bounds.Height - 24);
        var nameRect = new Rectangle(e.Bounds.Left + 18, e.Bounds.Top + 4, e.Bounds.Width - 78, 19);
        TextRenderer.DrawText(e.Graphics, t.Name, Theme.UiBold, nameRect, Theme.Text, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        string sub = t.Synth + (t.Synth == "drums" ? " · " + t.DrumKit : t.Patch != null ? " · patch" : "") + $" · {t.Vol * 100:0}% · {t.Notes.Count} notes";
        var subRect = new Rectangle(e.Bounds.Left + 18, e.Bounds.Top + 22, e.Bounds.Width - 78, 16);
        TextRenderer.DrawText(e.Graphics, sub, Theme.Small, subRect, Theme.Muted, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        DrawBadge(e.Graphics, new Rectangle(e.Bounds.Right - 50, e.Bounds.Top + 11, 20, 18), "M", t.Mute, Theme.AccentWarm);
        DrawBadge(e.Graphics, new Rectangle(e.Bounds.Right - 24, e.Bounds.Top + 11, 20, 18), "S", t.Solo, Theme.Good);
    }

    private static Color SynthColor(string synth) => synth switch
    {
        "drums" => Theme.AccentWarm,
        "pad" => Color.FromArgb(186, 140, 255),
        "pluck" => Color.FromArgb(120, 220, 200),
        "patch" => Theme.Accent,
        "sample" => Color.FromArgb(240, 200, 110),
        "noise" => Theme.Muted,
        _ => Theme.Good
    };

    private static void DrawBadge(Graphics g, Rectangle r, string text, bool on, Color color)
    {
        using var fill = new SolidBrush(on ? color : Theme.PanelAlt);
        using var border = new Pen(Theme.Border);
        g.FillRectangle(fill, r);
        g.DrawRectangle(border, r);
        TextRenderer.DrawText(g, text, Theme.Small, r, on ? Theme.Back : Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
