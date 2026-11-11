namespace HumbleBakerStudio.Audio;


// A rotary control: drag up/down (hold Shift for fine), mouse wheel, double-click to reset.
public class Knob : Control {
    public double Min = 0, Max = 1, Default = 0; public bool Log; public string Caption = "";
    public event EventHandler ValueChanged;
    double val; int dragY; double dragT; bool drag;
    public Knob() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Size = new Size(70, 92); Cursor = Cursors.SizeNS; }
    public double Value { get { return val; } set { double v = Math.Max(Min, Math.Min(Max, value)); if (v == val) return; val = v; Invalidate(); } }
    double ToT(double v) { if (Max <= Min) return 0; return Log ? Math.Log(Math.Max(v, Min) / Min) / Math.Log(Max / Min) : (v - Min) / (Max - Min); }
    double FromT(double t) { t = Math.Max(0, Math.Min(1, t)); return Log ? Min * Math.Pow(Max / Min, t) : Min + t * (Max - Min); }
    void Fire(double v) { Value = v; if (ValueChanged != null) ValueChanged(this, EventArgs.Empty); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { drag = true; dragY = e.Y; dragT = ToT(val); Capture = true; } }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (drag) Fire(FromT(dragT + (dragY - e.Y) / ((ModifierKeys & Keys.Shift) != 0 ? 600.0 : 150.0))); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); drag = false; Capture = false; }
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); Fire(FromT(ToT(val) + Math.Sign(e.Delta) * ((ModifierKeys & Keys.Shift) != 0 ? 0.005 : 0.03))); }
    protected override void OnDoubleClick(EventArgs e) { base.OnDoubleClick(e); Fire(Default); }
    string Fmt() { double a = Math.Abs(val); return val.ToString(a >= 1000 ? "0" : a >= 100 ? "0.#" : a >= 10 ? "0.0" : "0.00", CultureInfo.InvariantCulture); }
    protected override void OnPaint(PaintEventArgs e) {
        Graphics g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.Clear(BackColor);
        int d = Math.Min(Width - 16, Height - 42); if (d < 14) d = 14;
        Rectangle rc = new Rectangle((Width - d) / 2, 4, d, d);
        int textY = d + 7, capY = d + 21;
        double t = ToT(val);
        using (Pen bg = new Pen(Color.FromArgb(60, 66, 84), 5)) g.DrawArc(bg, rc, 135, 270);
        using (Pen fg = new Pen(ForeColor, 5)) { if (t > 0.002) g.DrawArc(fg, rc, 135, (float)(270 * t)); }
        using (SolidBrush b = new SolidBrush(Color.FromArgb(34, 38, 50))) g.FillEllipse(b, rc.X + 7, rc.Y + 7, d - 14, d - 14);
        double ang = (135 + 270 * t) * Math.PI / 180; float cx = rc.X + d / 2f, cy = rc.Y + d / 2f, rr = d / 2f - 9;
        using (Pen pt = new Pen(Color.White, 2)) g.DrawLine(pt, cx, cy, cx + (float)Math.Cos(ang) * rr, cy + (float)Math.Sin(ang) * rr);
        using (StringFormat sf = new StringFormat()) {
            sf.Alignment = StringAlignment.Center; sf.Trimming = StringTrimming.EllipsisCharacter; sf.FormatFlags = StringFormatFlags.NoWrap;
            using (SolidBrush tb = new SolidBrush(Color.White)) g.DrawString(Fmt(), Font, tb, new RectangleF(0, textY, Width, 15), sf);
            using (SolidBrush cb = new SolidBrush(Color.FromArgb(150, 158, 180))) g.DrawString(Caption, Font, cb, new RectangleF(0, capY, Width, 15), sf);
        }
    }
}
