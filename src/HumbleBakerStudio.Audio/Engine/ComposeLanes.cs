namespace HumbleBakerStudio.Audio;


// Reaper-style automation lane editor: every track is a header row with its automation lanes stacked
// beneath it. Left-click in a lane adds a point, drag a point to move it, right-click a point to remove
// it. Editing writes directly into the track's AutoLane list, so it stays in sync with the Automation
// Painting tab and the renderer.
public class ComposeLanes : Control {
    public Project? Proj;
    public event EventHandler? Changed;
    const int RulerH = 20, HeaderH = 22, LaneH = 58, LabelW = 168;
    public double Zoom = 44;
    HScrollBar hs = new HScrollBar(); VScrollBar vs = new VScrollBar();
    class Row { public int Track; public string Param = ""; public AutoLane? Lane; public bool Header; public int Y; public int H; }
    List<Row> rows = new List<Row>();
    int dragRow = -1, dragPt = -1; bool moved;
    int docH = 40;

    public ComposeLanes() {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true; BackColor = Color.FromArgb(24, 26, 32); TabStop = true;
        hs.Scroll += delegate { Invalidate(); }; vs.Scroll += delegate { Invalidate(); };
        Controls.Add(hs); Controls.Add(vs);
    }

    double CanvasBeats { get { double c = Proj != null ? Proj.LoopBeats : 16; if (Proj != null && Proj.Song != null && Proj.Song.Count > 0) c = Math.Max(c, Arranger.SongBeats(Proj)); return c + 16; } }
    int ViewW { get { return Math.Max(10, Width - vs.Width); } }

    void BuildRows() {
        rows.Clear();
        int y = RulerH;
        if (Proj != null) for (int ti = 0; ti < Proj.Tracks.Count; ti++) {
            Track t = Proj.Tracks[ti];
            rows.Add(new Row { Track = ti, Header = true, Y = y, H = HeaderH }); y += HeaderH;
            if (t.Auto != null) foreach (AutoLane al in t.Auto) { rows.Add(new Row { Track = ti, Param = al.Param, Lane = al, Y = y, H = LaneH }); y += LaneH; }
        }
        docH = y + 24;
    }

    void UpdateScroll() {
        if (Proj == null || Width < 80 || Height < 80) return;
        hs.SetBounds(0, Height - hs.Height, Width - vs.Width, hs.Height);
        vs.SetBounds(Width - vs.Width, 0, vs.Width, Height - hs.Height);
        int cw = (int)(CanvasBeats * Zoom);
        hs.LargeChange = Math.Max(1, ViewW); hs.SmallChange = 20; hs.Maximum = Math.Max(0, cw - ViewW) + hs.LargeChange - 1;
        vs.LargeChange = Math.Max(1, Height - hs.Height); vs.SmallChange = LaneH; vs.Maximum = Math.Max(0, docH - (Height - hs.Height)) + vs.LargeChange - 1;
        if (hs.Value > hs.Maximum - hs.LargeChange + 1) hs.Value = Math.Max(0, hs.Maximum - hs.LargeChange + 1);
        if (vs.Value > vs.Maximum - vs.LargeChange + 1) vs.Value = Math.Max(0, vs.Maximum - vs.LargeChange + 1);
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateScroll(); }

    public void Fit() { if (Proj == null) return; double beats = CanvasBeats; if (beats > 0 && ViewW > LabelW + 10) Zoom = Math.Max(2, (ViewW - LabelW - 10) / beats); UpdateScroll(); Invalidate(); }

    protected override void OnMouseWheel(MouseEventArgs e) {
        Keys m = Control.ModifierKeys;
        if ((m & Keys.Control) == Keys.Control) { double beat = XToBeat(e.X); Zoom = Math.Max(2, Math.Min(400, Zoom * (e.Delta > 0 ? 1.15 : 1 / 1.15))); UpdateScroll(); }
        else SetScrollV(vs.Value - e.Delta / 3);
    }
    void SetScrollV(int v) { v = Math.Max(vs.Minimum, Math.Min(v, Math.Max(vs.Minimum, vs.Maximum - vs.LargeChange + 1))); vs.Value = v; Invalidate(); }

    double XToBeat(int x) { return (x - LabelW + hs.Value) / Zoom; }
    int BeatToX(double b) { return (int)(LabelW + b * Zoom - hs.Value); }
    Row? RowAt(int y) { for (int i = 0; i < rows.Count; i++) { Row r = rows[i]; int top = r.Y - vs.Value; if (y >= top && y < top + r.H) return r; } return null; }
    double FracFromY(Row r, int y) { double top = r.Y - vs.Value, bot = top + r.H; return Math.Max(0, Math.Min(1, (bot - y) / (double)r.H)); }
    int YFromFrac(Row r, double f) { return (int)(r.Y - vs.Value + (1 - f) * r.H); }
    double FracToVal(string param, double f) { return AutoRange.Map(param, f); }
    double ValToFrac(string param, double v) { double mn, mx; bool lg; AutoRange.Info(param, out mn, out mx, out lg); if (lg) return Math.Log(v / mn) / Math.Log(mx / mn); return (v - mn) / (mx - mn); }
    double SnapBeat(double b) { return Math.Round(b / 0.25) * 0.25; }
    int PtAt(int x, int y, out Row? row) {
        row = RowAt(y);
        if (row == null || row.Header || row.Lane == null) return -1;
        double beat = XToBeat(x); int best = -1; double bd = 16;
        for (int i = 0; i < row.Lane.Points.Count; i++) {
            double px = BeatToX(row.Lane.Points[i][0]), py = YFromFrac(row, ValToFrac(row.Param, row.Lane.Points[i][1]));
            double d = Math.Abs(px - x) + Math.Abs(py - y);
            if (d < bd) { bd = d; best = i; }
        }
        return best;
    }

    protected override void OnPaint(PaintEventArgs pe) {
        BuildRows(); UpdateScroll();
        Graphics g = pe.Graphics; g.Clear(BackColor);
        if (Proj == null) return;
        int vw = ViewW, gb = Height - hs.Height;
        double barB = Proj.BarBeats > 0 ? Proj.BarBeats : 4;
        using (SolidBrush rb = new SolidBrush(Color.FromArgb(30, 33, 42))) g.FillRectangle(rb, 0, 0, vw, RulerH);
        using (Pen beat = new Pen(Color.FromArgb(58, 64, 82)), bar = new Pen(Color.FromArgb(120, 130, 150))) {
            for (long i = 0; i * 4.0 <= CanvasBeats; i++) { int x = BeatToX(i * 4.0); if (x >= LabelW && x < vw) g.DrawLine(beat, x, 0, x, RulerH); }
            for (long i = 0; i * barB <= CanvasBeats; i++) { int x = BeatToX(i * barB); if (x >= LabelW && x < vw) g.DrawLine(bar, x, 0, x, RulerH); }
        }
        using (SolidBrush txt = new SolidBrush(Color.FromArgb(150, 158, 175))) g.DrawString("beats", Font, txt, LabelW + 3, 2);
        g.SetClip(new Rectangle(0, RulerH, vw, gb - RulerH));
        foreach (Row r in rows) {
            int top = r.Y - vs.Value;
            if (top + r.H < RulerH || top > gb) continue;
            if (r.Header) {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(36, 39, 51))) g.FillRectangle(hb, 0, top, vw, r.H);
                Track t = Proj.Tracks[r.Track];
                using (SolidBrush tb = new SolidBrush(t.Mute ? Color.FromArgb(120, 126, 140) : Color.FromArgb(224, 228, 236)))
                    g.DrawString((t.Mute ? "(" : "") + t.Name + (t.Mute ? ")" : "") + "   [" + t.Synth + "]", Font, tb, 4, top + 3);
            } else if (r.Lane != null) {
                using (SolidBrush lb = new SolidBrush(Color.FromArgb(29, 32, 41))) g.FillRectangle(lb, 0, top, vw, r.H);
                using (SolidBrush lb2 = new SolidBrush(Color.FromArgb(36, 39, 51))) g.FillRectangle(lb2, 0, top, LabelW, r.H);
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(150, 208, 196))) g.DrawString(AutoRange.Label(r.Param), Font, tb, 4, top + 3);
                using (Pen gd = new Pen(Color.FromArgb(46, 50, 62))) g.DrawLine(gd, LabelW, top + r.H / 2, vw, top + r.H / 2);
                double neut = AutoRange.Neutral(r.Param);
                if (!double.IsNaN(neut)) { double nf = ValToFrac(r.Param, neut); if (nf >= 0 && nf <= 1) { int ny = YFromFrac(r, nf); using (Pen np = new Pen(Color.FromArgb(92, 120, 132)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }) g.DrawLine(np, LabelW, ny, vw, ny); } }
                List<double[]> pts = new List<double[]>(r.Lane.Points); pts.Sort(delegate (double[] a, double[] b) { return a[0].CompareTo(b[0]); });
                using (Pen env = new Pen(Color.FromArgb(40, 200, 170), 2))
                    for (int i = 0; i < pts.Count - 1; i++)
                        g.DrawLine(env, BeatToX(pts[i][0]), YFromFrac(r, ValToFrac(r.Param, pts[i][1])), BeatToX(pts[i + 1][0]), YFromFrac(r, ValToFrac(r.Param, pts[i + 1][1])));
                using (SolidBrush pbr = new SolidBrush(Color.FromArgb(255, 200, 120)))
                    foreach (double[] p in pts) { int px = BeatToX(p[0]), py = YFromFrac(r, ValToFrac(r.Param, p[1])); g.FillEllipse(pbr, px - 4, py - 4, 8, 8); g.DrawEllipse(Pens.Black, px - 4, py - 4, 8, 8); }
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e); Focus();
        if (e.Button == MouseButtons.Right) {
            Row? row; int i = PtAt(e.X, e.Y, out row);
            if (i >= 0 && row != null && row.Lane != null) { row.Lane.Points.RemoveAt(i); if (Changed != null) Changed(this, EventArgs.Empty); Invalidate(); }
            return;
        }
        Row? r2; int pi = PtAt(e.X, e.Y, out r2);
        if (pi >= 0 && r2 != null) { dragRow = rows.IndexOf(r2); dragPt = pi; moved = false; Capture = true; return; }
        Row? r3 = RowAt(e.Y);
        if (r3 != null && !r3.Header && r3.Lane != null && e.X >= LabelW) {
            double beat = SnapBeat(Math.Max(0, XToBeat(e.X)));
            double v = FracToVal(r3.Param, FracFromY(r3, e.Y));
            r3.Lane.Points.Add(new double[] { beat, v });
            if (Changed != null) Changed(this, EventArgs.Empty); Invalidate();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if (dragRow >= 0 && dragRow < rows.Count && rows[dragRow].Lane != null) {
            Row r = rows[dragRow];
            double beat = SnapBeat(Math.Max(0, XToBeat(e.X)));
            double v = FracToVal(r.Param, FracFromY(r, e.Y));
            r.Lane!.Points[dragPt][0] = beat; r.Lane.Points[dragPt][1] = v; moved = true; Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        if (dragRow >= 0 && moved && Changed != null) Changed(this, EventArgs.Empty);
        dragRow = -1; dragPt = -1; Capture = false;
    }
}
