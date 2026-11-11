using System.Drawing.Drawing2D;

namespace HumbleBakerStudio;

/// <summary>Track overview for a live deck: waveform envelope and playhead.</summary>
internal sealed class DeckWave : Control
{
    private float[] _l = Array.Empty<float>();
    private float[] _r = Array.Empty<float>();
    private double _playhead = -1;

    public DeckWave()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(14, 16, 21);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public double Playhead
    {
        get => _playhead;
        set { _playhead = value; Invalidate(); }
    }

    public void SetData(float[] left, float[] right)
    {
        _l = left ?? Array.Empty<float>();
        _r = right ?? Array.Empty<float>();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        int w = Math.Max(1, Width - 2), h = Math.Max(1, Height - 2);
        int y0 = 1 + h / 2;
        using (var mid = new Pen(Color.FromArgb(40, 46, 58))) g.DrawLine(mid, 1, y0, 1 + w, y0);
        int n = Math.Min(_l.Length, _r.Length);
        if (n < 2)
        {
            using var empty = new SolidBrush(Theme.Muted);
            g.DrawString("Load a project to see its waveform.", Theme.Small, empty, 8, 8);
            return;
        }

        using (var pen = new Pen(Color.FromArgb(100, 168, 255), 1f))
        {
            for (int px = 0; px < w; px++)
            {
                int s0 = (int)((long)px * n / w);
                int s1 = (int)((long)(px + 1) * n / w);
                if (s1 <= s0) s1 = s0 + 1;
                if (s1 > n) s1 = n;
                double mn = 0, mx = 0;
                for (int i = s0; i < s1; i++)
                {
                    double v = (_l[i] + _r[i]) * 0.5;
                    if (v < mn) mn = v;
                    if (v > mx) mx = v;
                }
                int yTop = y0 - (int)(mx * (h / 2 - 2));
                int yBot = y0 - (int)(mn * (h / 2 - 2));
                g.DrawLine(pen, 1 + px, yTop, 1 + px, yBot);
            }
        }

        if (_playhead >= 0 && _playhead <= 1)
        {
            int x = 1 + (int)(_playhead * (w - 1));
            using var ph = new Pen(Color.FromArgb(255, 168, 92), 2f);
            g.DrawLine(ph, x, 1, x, 1 + h);
        }

        using var border = new Pen(Theme.Border);
        g.DrawRectangle(border, 0, 0, w + 1, h + 1);
    }
}

/// <summary>A simple stereo peak meter.</summary>
internal sealed class LevelMeter : Control
{
    private double _level;
    private double _peak;

    public LevelMeter()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(14, 16, 21);
        Height = 12;
        Width = 140;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public void SetLevel(double level)
    {
        _level = Math.Clamp(level, 0, 1);
        _peak = Math.Max(_level, _peak * 0.92);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        int w = Width - 2, h = Height - 2;
        using (var back = new SolidBrush(Color.FromArgb(30, 34, 44))) g.FillRectangle(back, 1, 1, w, h);
        int fill = (int)(_level * w);
        if (fill > 0)
        {
            using var brush = new LinearGradientBrush(new Rectangle(1, 1, Math.Max(2, w), h),
                Color.FromArgb(110, 220, 150), Color.FromArgb(240, 110, 120), 0f);
            g.FillRectangle(brush, 1, 1, fill, h);
        }
        int peak = (int)(_peak * w);
        if (peak > 0)
        {
            using var pen = new Pen(Color.FromArgb(230, 236, 245), 2f);
            g.DrawLine(pen, 1 + Math.Min(peak, w - 1), 1, 1 + Math.Min(peak, w - 1), 1 + h);
        }
        using var border = new Pen(Theme.Border);
        g.DrawRectangle(border, 0, 0, w + 1, h + 1);
    }
}
