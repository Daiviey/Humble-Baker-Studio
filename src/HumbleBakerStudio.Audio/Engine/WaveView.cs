namespace HumbleBakerStudio.Audio;


// Waveform display with drag selection and slice markers. Samples are mono 44.1 kHz.
public class WaveView : Control {
    public float[] Samples;
    public int SelStart = 0, SelEnd = 0;
    public int[] Markers = new int[0];
    public event EventHandler SelectionChanged;
    bool dragging; int dragAnchor;
    public WaveView() { DoubleBuffered = true; BackColor = Color.FromArgb(20, 22, 28); ForeColor = Color.FromArgb(110, 215, 195); }
    public int SampleAt(int x) {
        if (Samples == null || Samples.Length == 0) return 0;
        double f = Math.Max(0, Math.Min(1, (x - 4) / (double)Math.Max(1, Width - 8)));
        return (int)(f * (Samples.Length - 1));
    }
    public int XAt(int sample) {
        if (Samples == null || Samples.Length < 2) return 4;
        return 4 + (int)((double)sample / (Samples.Length - 1) * (Width - 8));
    }
    protected override void OnPaint(PaintEventArgs e) {
        Graphics g = e.Graphics; g.Clear(BackColor);
        int w = Math.Max(1, Width - 8), h = Math.Max(1, Height - 8);
        using (Pen mid = new Pen(Color.FromArgb(48, 52, 62))) g.DrawLine(mid, 4, 4 + h / 2, 4 + w, 4 + h / 2);
        if (Samples == null || Samples.Length < 2) { using (Brush b = new SolidBrush(Color.FromArgb(110, 118, 132))) g.DrawString("Import a WAV to see its waveform.", Font, b, 10, 10); return; }
        int n = Samples.Length;
        if (SelEnd > SelStart) { int x1 = XAt(SelStart), x2 = XAt(SelEnd); using (Brush sb = new SolidBrush(Color.FromArgb(60, 70, 180, 255))) g.FillRectangle(sb, Math.Min(x1, x2), 4, Math.Max(1, Math.Abs(x2 - x1)), h); }
        using (Pen p = new Pen(ForeColor)) {
            for (int px = 0; px < w; px++) {
                int s0 = (int)((double)px / w * n), s1 = (int)((double)(px + 1) / w * n);
                if (s1 <= s0) s1 = s0 + 1; if (s1 > n) s1 = n;
                double mn = 0, mx = 0;
                for (int i = s0; i < s1; i++) { double v = Samples[i]; if (v < mn) mn = v; if (v > mx) mx = v; }
                int y1 = 4 + h / 2 - (int)(mx * (h / 2)), y2 = 4 + h / 2 - (int)(mn * (h / 2));
                g.DrawLine(p, 4 + px, y1, 4 + px, y2);
            }
        }
        using (Pen mp = new Pen(Color.FromArgb(235, 170, 60))) {
            if (Markers != null) foreach (int m in Markers) { int x = XAt(m); g.DrawLine(mp, x, 4, x, 4 + h); }
        }
        using (Brush tb = new SolidBrush(Color.FromArgb(145, 152, 166))) {
            double dur = n / (double)Dsp.SR;
            g.DrawString(dur.ToString("0.000") + " s", Font, tb, 6, Height - 18);
            if (SelEnd > SelStart) g.DrawString((SelStart / (double)Dsp.SR).ToString("0.000") + " - " + (SelEnd / (double)Dsp.SR).ToString("0.000") + " s", Font, tb, 90, Height - 18);
        }
    }
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e); dragging = true; dragAnchor = SampleAt(e.X); SelStart = dragAnchor; SelEnd = dragAnchor; Invalidate();
        if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e); if (!dragging) return;
        int s = SampleAt(e.X); SelStart = Math.Min(dragAnchor, s); SelEnd = Math.Max(dragAnchor, s); Invalidate();
        if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
    }
    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e); dragging = false; if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
    }
}
