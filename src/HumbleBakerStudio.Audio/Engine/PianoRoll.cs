namespace HumbleBakerStudio.Audio;


public class PianoRoll : Control {
    public Project Proj; public Track Cur;
    public double Grid = 0.25; public double Playhead = -1; public bool SnapScale;
    public double MarkerBeat = -1; public List<Note> Clip = new List<Note>(); public double ClipBase = 0;
    public bool[] Mask = new bool[12]; public int Root = 0;
    public double Zoom = 80; public int RowH = 14;
    public HashSet<Note> Sel = new HashSet<Note>();
    public event EventHandler Changed, BeginEdit, UndoRequested, SpaceRequested;
    public event EventHandler MarkersChanged;
    public event Action<Note> NotePlaced;

    const int KeyW = 58, RulerH = 22, VelH = 52;
    HScrollBar hs = new HScrollBar(); VScrollBar vs = new VScrollBar();
    int mode; Note dragNote; double startBeat; int startPitch; Rectangle box; Point boxStart;
    Dictionary<Note, Note> orig = new Dictionary<Note, Note>();
    double lastLen = 0.25; int lastVel = 100;
    static readonly bool[] Black = new bool[] { false, true, false, true, false, false, true, false, true, false, true, false };
    static readonly string[] NoteNames = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public PianoRoll() {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        TabStop = true; DoubleBuffered = true; BackColor = Color.FromArgb(24, 26, 32);
        hs.Scroll += delegate { Invalidate(); }; vs.Scroll += delegate { Invalidate(); };
        Controls.Add(hs); Controls.Add(vs);
        vs.Value = 0;
        for (int i = 0; i < 12; i++) Mask[i] = true;
    }

    public void ScrollToPitch(int pitch) { SetScroll(vs, Math.Max(0, (127 - pitch) * RowH - Math.Max(0, ViewH / 2))); }
    // Brings a timeline position into view so notes can be written or pasted far past the loop.
    public void ScrollToBeat(double beat) { UpdateScroll(); SetScroll(hs, (int)(beat * Zoom - ViewW / 2.0)); }
    // The canvas always covers the loop, the whole song arrangement and some writing room past the end.
    double CanvasBeats { get { return Math.Max(Proj.LoopBeats, Proj.Song == null ? 0 : Arranger.SongBeats(Proj)) + 64; } }
    int ViewW { get { return Math.Max(10, Width - vs.Width - KeyW); } }
    int GridH { get { return Math.Max(10, Height - hs.Height - RulerH - VelH); } }
    int ViewH { get { return GridH; } }
    void SetScroll(ScrollBar b, int v) { v = Math.Max(b.Minimum, Math.Min(v, Math.Max(b.Minimum, b.Maximum - b.LargeChange + 1))); b.Value = v; Invalidate(); }

    public void UpdateScroll() {
        if (Proj == null || Width < 120 || Height < 120) return;
        hs.SetBounds(0, Height - hs.Height, Width - vs.Width, hs.Height);
        vs.SetBounds(Width - vs.Width, 0, vs.Width, Height - hs.Height);
        int cw = (int)(CanvasBeats * Zoom), vw = ViewW;
        hs.LargeChange = Math.Max(1, vw); hs.SmallChange = 20; hs.Maximum = Math.Max(0, cw - vw) + hs.LargeChange - 1;
        int ch = 128 * RowH, vh = GridH;
        vs.LargeChange = Math.Max(1, vh); vs.SmallChange = RowH; vs.Maximum = Math.Max(0, ch - vh) + vs.LargeChange - 1;
        if (hs.Value > hs.Maximum - hs.LargeChange + 1) hs.Value = Math.Max(0, hs.Maximum - hs.LargeChange + 1);
        if (vs.Value > vs.Maximum - vs.LargeChange + 1) vs.Value = Math.Max(0, vs.Maximum - vs.LargeChange + 1);
    }
    protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateScroll(); }

    double XToBeat(int x) { return (x - KeyW + hs.Value) / Zoom; }
    int BeatToX(double b) { return (int)(KeyW + b * Zoom - hs.Value); }
    int YToPitch(int y) { return 127 - (int)Math.Floor((double)(y - RulerH + vs.Value) / RowH); }
    int PitchToY(int p) { return RulerH + (127 - p) * RowH - vs.Value; }
    double Snap(double d) { return Math.Round(d / Grid) * Grid; }
    void Fire(EventHandler h) { if (h != null) h(this, EventArgs.Empty); }
    int FixPitch(int p) { p = Math.Max(0, Math.Min(127, p)); return SnapScale ? Scales.Snap(p, Mask) : p; }

    Note NoteAt(int x, int y) {
        if (Cur == null) return null;
        for (int i = Cur.Notes.Count - 1; i >= 0; i--) {
            Note n = Cur.Notes[i];
            Rectangle r = NoteRect(n);
            if (r.Contains(x, y)) return n;
        }
        return null;
    }
    Rectangle NoteRect(Note n) {
        int x = BeatToX(n.Start), w = Math.Max(4, (int)(n.Len * Zoom));
        return new Rectangle(x, PitchToY(n.Pitch), w, RowH);
    }

    protected override void OnPaint(PaintEventArgs pe) {
        Graphics g = pe.Graphics;
        g.Clear(BackColor);
        if (Proj == null) return;
        int vw = Width - vs.Width, gb = Height - hs.Height - VelH;
        Rectangle gridRect = new Rectangle(KeyW, RulerH, vw - KeyW, gb - RulerH);
        g.SetClip(gridRect);
        int p0 = Math.Max(0, YToPitch(gridRect.Bottom)), p1 = Math.Min(127, YToPitch(gridRect.Top));
        using (SolidBrush white = new SolidBrush(Color.FromArgb(34, 37, 46)), blk = new SolidBrush(Color.FromArgb(26, 28, 35)), tone = new SolidBrush(Color.FromArgb(40, 52, 78)), root = new SolidBrush(Color.FromArgb(52, 66, 100))) {
            for (int p = p0; p <= p1; p++) {
                int pc = p % 12; int y = PitchToY(p);
                Brush b = Black[pc] ? blk : white;
                if (Mask[pc]) b = (pc == Root) ? root : tone;
                g.FillRectangle(b, KeyW, y, vw - KeyW, RowH);
            }
        }
        double barB = Proj.BarBeats, phraseB = barB * Math.Max(1, Proj.PhraseBars);
        double viewStart = Math.Max(0, XToBeat(KeyW)), viewEnd = XToBeat(vw) + 1;
        // alternate phrases get a faint tint so the song structure reads at a glance
        using (SolidBrush tint = new SolidBrush(Color.FromArgb(26, 255, 190, 80))) {
            for (long ph = (long)Math.Floor(viewStart / phraseB); ph * phraseB <= viewEnd; ph++) {
                if ((ph & 1) == 0) continue;
                int x0 = Math.Max(KeyW, BeatToX(ph * phraseB)), x1 = Math.Min(vw, BeatToX((ph + 1) * phraseB));
                if (x1 > x0) g.FillRectangle(tint, x0, RulerH, x1 - x0, gb - RulerH);
            }
        }
        using (Pen row = new Pen(Color.FromArgb(46, 50, 62)), s16 = new Pen(Color.FromArgb(40, 45, 58)), sub = new Pen(Color.FromArgb(52, 58, 80)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot },
               beat = new Pen(Color.FromArgb(78, 88, 112)), whole = new Pen(Color.FromArgb(104, 122, 170), 2), bar = new Pen(Color.FromArgb(170, 180, 205), 2), phrase = new Pen(Color.FromArgb(255, 176, 64), 3)) {
            for (int p = p0; p <= p1 + 1; p++) g.DrawLine(row, KeyW, PitchToY(p), vw, PitchToY(p));
            // 16th-note divisions are always drawn (when zoomed in enough); a finer or triplet snap grid adds dotted lines
            if (Zoom * 0.25 >= 5) for (long i = (long)Math.Floor(viewStart * 4); i <= viewEnd * 4; i++) { int x = BeatToX(i * 0.25); if (i % 4 != 0) g.DrawLine(s16, x, RulerH, x, gb); }
            if (Math.Abs(Grid * 4 - Math.Round(Grid * 4)) > 1e-6 || Grid < 0.25 - 1e-9) {
                for (long i = (long)Math.Floor(viewStart / Grid); i * Grid <= viewEnd; i++) {
                    double bb = i * Grid; if (Math.Abs(bb * 4 - Math.Round(bb * 4)) < 1e-6) continue;
                    int x = BeatToX(bb); g.DrawLine(sub, x, RulerH, x, gb);
                }
            }
            for (long i = (long)Math.Floor(viewStart); i <= viewEnd; i++) g.DrawLine(beat, BeatToX(i), RulerH, BeatToX(i), gb);
            for (long i = (long)Math.Floor(viewStart / 4); i * 4.0 <= viewEnd; i++) g.DrawLine(whole, BeatToX(i * 4.0), RulerH, BeatToX(i * 4.0), gb);
            for (long i = (long)Math.Floor(viewStart / barB); i * barB <= viewEnd; i++) g.DrawLine(bar, BeatToX(i * barB), RulerH, BeatToX(i * barB), gb);
            for (long i = (long)Math.Floor(viewStart / phraseB); i * phraseB <= viewEnd; i++) g.DrawLine(phrase, BeatToX(i * phraseB), RulerH, BeatToX(i * phraseB), gb);
        }
        int loopX = BeatToX(Proj.LoopBeats);
        using (SolidBrush shade = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) { if (loopX < vw) g.FillRectangle(shade, Math.Max(KeyW, loopX), RulerH, vw, gb - RulerH); }
        // other tracks as ghosts
        using (SolidBrush ghost = new SolidBrush(Color.FromArgb(50, 200, 200, 255))) {
            foreach (Track t in Proj.Tracks) {
                if (t == Cur || t.Mute) continue;
                foreach (Note n in t.Notes) { Rectangle r = NoteRect(n); if (r.Right >= KeyW && r.Left < vw) g.FillRectangle(ghost, r.X, r.Y + 3, r.Width, RowH - 6); }
            }
        }
        if (Cur != null) {
            foreach (Note n in Cur.Notes) {
                Rectangle r = NoteRect(n);
                if (r.Right < KeyW || r.Left > vw || r.Bottom < RulerH || r.Top > gb) continue;
                int sh = 80 + n.Vel * 175 / 127;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(40, Math.Min(255, sh), 190)))
                    g.FillRectangle(b, r.X, r.Y + 1, r.Width - 1, RowH - 2);
                bool sel = Sel.Contains(n);
                using (Pen pn = new Pen(sel ? Color.White : Color.FromArgb(20, 90, 80), sel ? 2 : 1)) g.DrawRectangle(pn, r.X, r.Y + 1, r.Width - 1, RowH - 2);
            }
        }
        if (mode == 4) using (Pen pn = new Pen(Color.FromArgb(200, 255, 255, 255)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }) g.DrawRectangle(pn, box);
        if (Playhead >= 0) using (Pen pn = new Pen(Color.FromArgb(255, 90, 90), 2)) { int x = BeatToX(Playhead); g.DrawLine(pn, x, RulerH, x, gb + VelH); }
        g.ResetClip();

        // piano keys
        g.SetClip(new Rectangle(0, RulerH, KeyW, gb - RulerH));
        using (Font f = new Font("Segoe UI", 7f)) {
            for (int p = p0; p <= p1; p++) {
                int y = PitchToY(p); int pc = p % 12;
                using (SolidBrush b = new SolidBrush(Black[pc] ? Color.FromArgb(20, 20, 24) : Color.FromArgb(222, 224, 230))) g.FillRectangle(b, 0, y, KeyW - 1, RowH - 1);
                if (pc == 0 || Mask[pc]) using (SolidBrush tb = new SolidBrush(Black[pc] ? Color.Gainsboro : Color.FromArgb(40, 40, 50))) g.DrawString(NoteNames[pc] + ((p / 12) - 1), f, tb, 4, y + 1);
            }
        }
        g.ResetClip();
        if (Proj.Markers != null && Proj.Markers.Count > 0) {
            g.SetClip(gridRect);
            using (Pen mp = new Pen(Color.FromArgb(120, 70, 140, 220), 1)) { mp.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash; foreach (Marker mk in Proj.Markers) { int mx = BeatToX(mk.Beat); if (mx >= KeyW && mx <= vw) g.DrawLine(mp, mx, RulerH, mx, gb); } }
            g.ResetClip();
        }

        // ruler
        using (SolidBrush rb = new SolidBrush(Color.FromArgb(18, 20, 26))) g.FillRectangle(rb, 0, 0, vw, RulerH);
        g.SetClip(new Rectangle(KeyW, 0, vw - KeyW, RulerH));
        using (Font f = new Font("Segoe UI", 7.5f)) using (Font fb = new Font("Segoe UI", 7.5f, FontStyle.Bold)) using (SolidBrush tb = new SolidBrush(Color.Silver)) using (SolidBrush pb = new SolidBrush(Color.FromArgb(255, 190, 90))) using (SolidBrush mb = new SolidBrush(Color.FromArgb(120, 128, 150)))
        using (Pen tick = new Pen(Color.FromArgb(90, 98, 120)), ptick = new Pen(Color.FromArgb(255, 176, 64), 2)) {
            double endB = XToBeat(vw) + 1;
            if (Zoom * 0.25 >= 5) for (long i = (long)Math.Floor(Math.Max(0, XToBeat(KeyW)) * 4); i <= endB * 4; i++) { int x = BeatToX(i * 0.25); g.DrawLine(tick, x, RulerH - (i % 4 == 0 ? 8 : 3), x, RulerH); }
            for (int bar = 0; bar * barB <= endB; bar++) {
                int x = BeatToX(bar * barB);
                if (x < KeyW - 40) continue;
                bool isPhrase = (bar % Math.Max(1, Proj.PhraseBars)) == 0;
                if (isPhrase) {
                    g.DrawLine(ptick, x, 0, x, RulerH);
                    double sec = bar * barB * 60.0 / Math.Max(1, Proj.Bpm); string time = ((int)(sec / 60)) + ":" + ((int)sec % 60).ToString("00");
                    g.DrawString("Phrase " + (bar / Math.Max(1, Proj.PhraseBars) + 1) + "  " + time, fb, pb, x + 3, 1);
                    g.DrawString((bar + 1).ToString(), f, tb, x + 3, 11);
                } else g.DrawString((bar + 1).ToString(), f, tb, x + 3, 4);
                if (Zoom >= 36) for (int k = 1; k < Math.Ceiling(barB) && k < 16; k++) { if (k >= barB - 1e-9) break; int bx = BeatToX(bar * barB + k); g.DrawString((k + 1).ToString(), f, mb, bx + 2, 8); }
            }
            if (Proj.Markers != null) foreach (Marker mk in Proj.Markers) {
                int mx = BeatToX(mk.Beat); if (mx < KeyW - 300 || mx > vw) continue;
                string label = mk.Name + (mk.Energy > 0 ? " (" + mk.Energy + ")" : ""); SizeF sz = g.MeasureString(label, fb);
                using (SolidBrush chip = new SolidBrush(Color.FromArgb(70, 140, 220))) g.FillRectangle(chip, mx, 9, (int)sz.Width + 6, 12);
                g.DrawString(label, fb, Brushes.White, mx + 3, 8);
            }
            if (MarkerBeat >= 0) { int mx = BeatToX(MarkerBeat); if (mx >= KeyW && mx <= vw) { using (SolidBrush mb2 = new SolidBrush(Color.FromArgb(255, 20, 147))) { g.FillPolygon(mb2, new Point[] { new Point(mx - 6, 1), new Point(mx + 6, 1), new Point(mx, 10) }); } using (Pen mp2 = new Pen(Color.FromArgb(255, 20, 147), 1)) g.DrawLine(mp2, mx, 10, mx, RulerH); string ml = MarkerBeat.ToString("0.0"); g.DrawString(ml, f, Brushes.White, mx + 7, 4); } }
        }
        g.ResetClip();

        // velocity lane
        Rectangle vr = new Rectangle(KeyW, gb, vw - KeyW, VelH);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(18, 20, 26))) g.FillRectangle(b, 0, gb, vw, VelH);
        g.SetClip(vr);
        if (Cur != null) foreach (Note n in Cur.Notes) {
            int x = BeatToX(n.Start); int h = (int)((VelH - 6) * n.Vel / 127.0);
            using (SolidBrush b = new SolidBrush(Sel.Contains(n) ? Color.White : Color.FromArgb(40, 200, 170))) g.FillRectangle(b, x, gb + VelH - 3 - h, 4, h);
        }
        g.ResetClip();
        using (Pen pn = new Pen(Color.FromArgb(64, 70, 86))) g.DrawLine(pn, 0, gb, vw, gb);
    }

    void BeginChange() { Fire(BeginEdit); }

    // Double-click on the ruler: adds an arrangement marker (energy 5 = neutral) or removes
    // the marker under the cursor, so song structure can be sketched straight on the piano roll.
    void ToggleMarkerAt(int x, double beat) {
        Marker hit = null; double best = 8;
        if (Proj.Markers != null) foreach (Marker m in Proj.Markers) {
            double d = Math.Abs(BeatToX(m.Beat) - x);
            if (d < best) { best = d; hit = m; }
        }
        BeginChange();
        if (hit != null) Proj.Markers.Remove(hit);
        else {
            if (Proj.Markers == null) Proj.Markers = new List<Marker>();
            Proj.Markers.Add(new Marker(beat, "Section", 5));
        }
        Fire(Changed); Fire(MarkersChanged); Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e) {
        Focus();
        if (Proj == null || Cur == null) return;
        int gb = Height - hs.Height - VelH;
        if (e.Y < RulerH && e.X >= KeyW) {
            double b = Math.Max(0, Math.Round(XToBeat(e.X) / Grid) * Grid);
            if (e.Clicks >= 2) { ToggleMarkerAt(e.X, b); return; }
            if (e.Button == MouseButtons.Left) { MarkerBeat = b; Playhead = b; Fire(Changed); Invalidate(); }
            else if (e.Button == MouseButtons.Right) { MarkerBeat = -1; Playhead = -1; Fire(Changed); Invalidate(); }
            return;
        }
        if (e.X < KeyW || e.Y < RulerH) return;
        if (e.Y >= gb) { // velocity lane: pick the note under the cursor by start position
            mode = 5; VelAt(e); return;
        }
        Note hit = NoteAt(e.X, e.Y);
        if (e.Button == MouseButtons.Right) {
            mode = 3; BeginChange();
            if (hit != null) { Cur.Notes.Remove(hit); Sel.Remove(hit); Fire(Changed); Invalidate(); }
            return;
        }
        startBeat = XToBeat(e.X); startPitch = YToPitch(e.Y);
        if (hit == null && (Control.ModifierKeys & Keys.Control) == Keys.Control) {
            mode = 4; boxStart = e.Location; box = new Rectangle(e.Location, new Size(0, 0)); Sel.Clear(); Invalidate(); return;
        }
        BeginChange();
        if (hit == null) {
            Sel.Clear();
            Note n = new Note(FixPitch(startPitch), Math.Max(0, Math.Floor(startBeat / Grid) * Grid), lastLen, lastVel);
            Cur.Notes.Add(n); Sel.Add(n); dragNote = n;
            mode = 2; orig.Clear(); orig[n] = n.Clone();
            Fire(Changed); Invalidate();
            if (NotePlaced != null) NotePlaced(n);
            return;
        }
        if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) { if (!Sel.Remove(hit)) Sel.Add(hit); }
        else if (!Sel.Contains(hit)) { Sel.Clear(); Sel.Add(hit); }
        dragNote = hit; orig.Clear();
        foreach (Note n in Sel) orig[n] = n.Clone();
        Rectangle r = NoteRect(hit);
        mode = (e.X >= r.Right - 6) ? 2 : 1;
        Invalidate();
    }

    void VelAt(MouseEventArgs e) {
        int gb = Height - hs.Height - VelH;
        double b = XToBeat(e.X);
        Note best = null; double bd = 1e9;
        foreach (Note n in Cur.Notes) { double d = Math.Abs(n.Start - b) * Zoom; if (d < bd) { bd = d; best = n; } }
        if (best == null || bd > 10) return;
        int v = (int)Math.Round(127.0 * (gb + VelH - 3 - e.Y) / (VelH - 6));
        v = Math.Max(1, Math.Min(127, v));
        if (Sel.Contains(best)) foreach (Note n in Sel) n.Vel = v; else best.Vel = v;
        lastVel = v; Fire(Changed); Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        if (Proj == null || Cur == null) return;
        if (mode == 5) { VelAt(e); return; }
        if (mode == 3) {
            Note hit = NoteAt(e.X, e.Y);
            if (hit != null) { Cur.Notes.Remove(hit); Sel.Remove(hit); Fire(Changed); Invalidate(); }
            return;
        }
        if (mode == 4) {
            box = new Rectangle(Math.Min(boxStart.X, e.X), Math.Min(boxStart.Y, e.Y), Math.Abs(e.X - boxStart.X), Math.Abs(e.Y - boxStart.Y));
            Sel.Clear();
            foreach (Note n in Cur.Notes) if (NoteRect(n).IntersectsWith(box)) Sel.Add(n);
            Invalidate(); return;
        }
        double cur = XToBeat(e.X);
        if (mode == 1) {
            double db = Snap(cur - startBeat); int dp = YToPitch(e.Y) - startPitch;
            foreach (Note n in Sel) {
                Note o; if (!orig.TryGetValue(n, out o)) continue;
                n.Start = Math.Max(0, o.Start + db); n.Pitch = FixPitch(o.Pitch + dp);
            }
            Fire(Changed); Invalidate();
        } else if (mode == 2) {
            double dl = Snap(cur - startBeat);
            foreach (Note n in Sel) {
                Note o; if (!orig.TryGetValue(n, out o)) continue;
                n.Len = Math.Max(Grid / 4, o.Len + dl);
            }
            Fire(Changed); Invalidate();
        } else {
            Cursor = (NoteAt(e.X, e.Y) != null && (e.X >= NoteRect(NoteAt(e.X, e.Y)).Right - 6)) ? Cursors.SizeWE : Cursors.Default;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        if ((mode == 1 || mode == 2) && dragNote != null && mode == 2) lastLen = dragNote.Len;
        mode = 0; dragNote = null; Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
        Keys m = Control.ModifierKeys;
        if ((m & Keys.Control) == Keys.Control) {
            double beat = XToBeat(e.X);
            Zoom = Math.Max(3, Math.Min(400, Zoom * (e.Delta > 0 ? 1.15 : 1 / 1.15)));
            UpdateScroll(); SetScroll(hs, (int)(beat * Zoom - (e.X - KeyW)));
        } else if ((m & Keys.Shift) == Keys.Shift) SetScroll(hs, hs.Value - e.Delta / 2);
        else SetScroll(vs, vs.Value - e.Delta / 3);
    }

    protected override bool IsInputKey(Keys k) {
        Keys b = k & Keys.KeyCode;
        return b == Keys.Up || b == Keys.Down || b == Keys.Left || b == Keys.Right || base.IsInputKey(k);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys k) {
        if (k == (Keys.Control | Keys.Z)) { Fire(UndoRequested); return true; }
        if (k == Keys.Space) { Fire(SpaceRequested); return true; }
        return base.ProcessCmdKey(ref msg, k);
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        if (Cur == null) return;
        bool shift = e.Shift;
        if (e.KeyCode == Keys.A && e.Control) { Sel.Clear(); foreach (Note n in Cur.Notes) Sel.Add(n); Invalidate(); e.Handled = true; return; }
        if (e.KeyCode == Keys.C && e.Control) { if (Sel.Count > 0) { Clip.Clear(); double b0 = 1e9; foreach (Note n in Sel) { Clip.Add(n.Clone()); b0 = Math.Min(b0, n.Start); } ClipBase = b0; } e.Handled = true; return; }
        if (e.KeyCode == Keys.X && e.Control) { if (Sel.Count > 0) { Clip.Clear(); double b0 = 1e9; foreach (Note n in Sel) { Clip.Add(n.Clone()); b0 = Math.Min(b0, n.Start); } ClipBase = b0; BeginChange(); foreach (Note n in new List<Note>(Sel)) Cur.Notes.Remove(n); Sel.Clear(); Fire(Changed); Invalidate(); } e.Handled = true; return; }
        if (e.KeyCode == Keys.V && e.Control) { if (Clip.Count > 0) { double at = MarkerBeat >= 0 ? MarkerBeat : (Playhead >= 0 ? Playhead : 0); BeginChange(); List<Note> add = new List<Note>(); foreach (Note n in Clip) { Note c = n.Clone(); c.Start = at + (n.Start - ClipBase); if (c.Start >= 0) add.Add(c); } Sel.Clear(); foreach (Note c in add) { Cur.Notes.Add(c); Sel.Add(c); } MarkerBeat = at; Fire(Changed); Invalidate(); } e.Handled = true; return; }
        if (Sel.Count == 0) return;
        if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back) {
            BeginChange(); foreach (Note n in new List<Note>(Sel)) Cur.Notes.Remove(n); Sel.Clear(); Fire(Changed); Invalidate(); e.Handled = true;
        } else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) {
            BeginChange(); int d = (e.KeyCode == Keys.Up ? 1 : -1) * (shift ? 12 : 1);
            foreach (Note n in Sel) n.Pitch = Math.Max(0, Math.Min(127, n.Pitch + d));
            Fire(Changed); Invalidate(); e.Handled = true;
        } else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) {
            BeginChange(); double d = (e.KeyCode == Keys.Right ? 1 : -1) * Grid;
            foreach (Note n in Sel) n.Start = Math.Max(0, n.Start + d);
            Fire(Changed); Invalidate(); e.Handled = true;
        } else if (e.KeyCode == Keys.D && e.Control) {
            BeginChange();
            double lo = 1e9, hi = 0; foreach (Note n in Sel) { lo = Math.Min(lo, n.Start); hi = Math.Max(hi, n.Start + n.Len); }
            double shiftB = Math.Max(Grid, Math.Ceiling((hi - lo) / Grid) * Grid);
            List<Note> add = new List<Note>();
            foreach (Note n in Sel) { Note c = n.Clone(); c.Start += shiftB; add.Add(c); }
            Sel.Clear(); foreach (Note c in add) { Cur.Notes.Add(c); Sel.Add(c); }
            Fire(Changed); Invalidate(); e.Handled = true;
        }
    }
}
