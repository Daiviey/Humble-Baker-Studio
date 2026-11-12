#requires -Version 5.1
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 David Burr (Daiviey)
<#
  Live performance mode for Humble Baker Studio.

  Two independent projects, two decks, one crossfader, an optional separate cue output,
  per-deck gain and LP/HP filter, tempo sync and queues. The audio engine and window are
  compiled as C# with Add-Type; project loading and rendering reuse the studio's own
  HbStudio engine and project functions.

  Dot-source this file from the studio implementation, then call Show-LivePerformance <project>.
#>

if (-not ('HbLive.LiveDeck' -as [type])) {
Add-Type -ReferencedAssemblies System.Windows.Forms, System.Drawing -Language CSharp -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HbLive {

public class LiveDeck {
    object gate = new object();
    float[] l = new float[0];
    float[] r = new float[0];
    double pos, lpL, lpR, lastFilter = double.NaN, a = 1;
    public string Name = "";
    public string SourcePath = "";
    public string KeyText = "";
    public double Bpm = 120;
    public bool Playing;
    public bool Loop = true;
    public bool Cue;
    public bool Ended;
    public double Gain = 1;
    public double Filter;
    public double Rate = 1;

    public int Frames { get { lock (gate) return l.Length; } }
    public double Seconds { get { return Frames / 44100.0; } }
    public double PositionSeconds { get { lock (gate) return pos / 44100.0; } }
    public bool HasAudio { get { return Frames > 0; } }
    public float[] GetLeft() { lock (gate) return l; }
    public float[] GetRight() { lock (gate) return r; }

    public void SetBuffer(string name, string path, float[] left, float[] right, double bpm, string keyText, bool songMode) {
        lock (gate) {
            Name = name; SourcePath = path;
            l = left == null ? new float[0] : left;
            r = right == null ? new float[0] : right;
            Bpm = bpm <= 0 ? 120 : bpm;
            KeyText = keyText; Loop = !songMode;
            pos = 0; Ended = false; lpL = lpR = 0; lastFilter = double.NaN;
            Playing = false;
        }
    }

    public void Stop() { lock (gate) { Playing = false; pos = 0; Ended = false; } }
    public void Restart() { lock (gate) { pos = 0; Ended = false; } }

    public void Render(float[] dl, float[] dr, int off, int frames) {
        lock (gate) {
            if (!Playing || l.Length < 2 || frames <= 0) return;
            int n = l.Length;
            if (r.Length < n) n = r.Length;

            double f = Math.Max(-1, Math.Min(1, Filter));
            if (Math.Abs(f - lastFilter) > 0.0001) {
                lastFilter = f;
                double cutoff = f < 0 ? 20000.0 * Math.Pow(200.0 / 20000.0, -f)
                    : f > 0 ? 20.0 * Math.Pow(8000.0 / 20.0, f) : 20000.0;
                a = 1 - Math.Exp(-2 * Math.PI * cutoff / 44100.0);
            }

            for (int i = 0; i < frames; i++) {
                if (pos >= n) {
                    if (Loop) pos = pos % n;
                    else { Ended = true; Playing = false; break; }
                }
                int i0 = (int)pos;
                int i1 = i0 + 1;
                if (i1 >= n) i1 = Loop ? 0 : n - 1;
                double frac = pos - i0;
                double sL = l[i0] * (1 - frac) + l[i1] * frac;
                double sR = r[i0] * (1 - frac) + r[i1] * frac;
                if (f < -0.001 || f > 0.001) {
                    lpL += a * (sL - lpL);
                    lpR += a * (sR - lpR);
                    if (f < 0) { sL = lpL; sR = lpR; }
                    else { sL -= lpL; sR -= lpR; }
                }
                dl[off + i] += (float)(sL * Gain);
                dr[off + i] += (float)(sR * Gain);
                pos += Rate;
            }
        }
    }
}

public class LiveMixer {
    public LiveDeck A = new LiveDeck();
    public LiveDeck B = new LiveDeck();
    public double Crossfade = 0.5;
    public double MasterGain = 0.85;
    public double CueLevel = 0.8;
    public bool CueToMaster;
    public double LastMasterPeak;
    public double LastCuePeak;

    float[] aL = new float[0], aR = new float[0], bL = new float[0], bR = new float[0];

    public void Sync(LiveDeck deck) {
        LiveDeck other = object.ReferenceEquals(deck, A) ? B : A;
        if (deck.Bpm <= 0 || other.Bpm <= 0) return;
        deck.Rate = Math.Max(0.5, Math.Min(2.0, other.Bpm / deck.Bpm));
    }

    public void Fill(float[] mL, float[] mR, float[] cL, float[] cR, int frames) {
        Array.Clear(mL, 0, frames); Array.Clear(mR, 0, frames);
        Array.Clear(cL, 0, frames); Array.Clear(cR, 0, frames);
        if (frames <= 0) return;
        if (aL.Length < frames) {
            aL = new float[frames]; aR = new float[frames];
            bL = new float[frames]; bR = new float[frames];
        }
        Array.Clear(aL, 0, frames); Array.Clear(aR, 0, frames);
        Array.Clear(bL, 0, frames); Array.Clear(bR, 0, frames);

        A.Render(aL, aR, 0, frames);
        B.Render(bL, bR, 0, frames);

        double x = Math.Max(0, Math.Min(1, Crossfade));
        double ga = Math.Cos(x * Math.PI / 2);
        double gb = Math.Sin(x * Math.PI / 2);
        double mg = Math.Max(0, Math.Min(2, MasterGain));
        double clv = Math.Max(0, Math.Min(2, CueLevel));
        double pm = 0, pc = 0;

        for (int i = 0; i < frames; i++) {
            double ml = aL[i] * ga + bL[i] * gb;
            double mr = aR[i] * ga + bR[i] * gb;
            double cl = (A.Cue ? aL[i] : 0) + (B.Cue ? bL[i] : 0);
            double cr = (A.Cue ? aR[i] : 0) + (B.Cue ? bR[i] : 0);
            cl *= clv; cr *= clv;
            if (CueToMaster) { ml += cl; mr += cr; }
            double oL = Math.Max(-1, Math.Min(1, ml * mg));
            double oR = Math.Max(-1, Math.Min(1, mr * mg));
            mL[i] = (float)oL; mR[i] = (float)oR;
            cL[i] = (float)Math.Max(-1, Math.Min(1, cl));
            cR[i] = (float)Math.Max(-1, Math.Min(1, cr));
            pm = Math.Max(pm, Math.Max(Math.Abs(oL), Math.Abs(oR)));
            pc = Math.Max(pc, Math.Max(Math.Abs(cl), Math.Abs(cr)));
        }
        LastMasterPeak = pm; LastCuePeak = pc;
    }
}

public static class LiveDevices {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Caps {
        public ushort wMid, wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szPname;
        public uint dwFormats;
        public ushort wChannels, wReserved1;
        public uint dwSupport;
    }
    [DllImport("winmm.dll")] static extern int waveOutGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] static extern int waveOutGetDevCapsW(IntPtr device, ref Caps caps, int size);

    public static string[] Get() {
        int n = Math.Max(0, waveOutGetNumDevs());
        string[] r = new string[n];
        for (int i = 0; i < n; i++) {
            Caps c = new Caps();
            waveOutGetDevCapsW((IntPtr)i, ref c, Marshal.SizeOf(typeof(Caps)));
            r[i] = string.IsNullOrEmpty(c.szPname) ? ("Output " + (i + 1)) : c.szPname;
        }
        return r;
    }
}

public class LiveEngine {
    [StructLayout(LayoutKind.Sequential)]
    struct Wf { public short FormatTag, Channels; public int SamplesPerSec, AvgBytesPerSec; public short BlockAlign, BitsPerSample, Size; }
    [StructLayout(LayoutKind.Sequential)]
    struct Wh { public IntPtr Data; public int BufferLength, BytesRecorded; public IntPtr User; public int Flags, Loops; public IntPtr Next, Reserved; }
    class Slot { public IntPtr Header, Data; }

    const int Frames = 1024, Slots = 3, Prepared = 2, Done = 1;
    [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr h, int dev, ref Wf fmt, IntPtr cb, IntPtr inst, int flags);
    [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr h, IntPtr hdr, int size);
    [DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr h, IntPtr hdr, int size);
    [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr h, IntPtr hdr, int size);
    [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr h);
    [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr h);

    readonly LiveMixer mixer;
    readonly float[] mL = new float[Frames], mR = new float[Frames], cL = new float[Frames], cR = new float[Frames];
    readonly Queue<short[]> cueQ = new Queue<short[]>();
    System.Threading.Thread thread;
    volatile bool stop;
    IntPtr master, cue;
    bool cueOn;

    public string LastError = "";
    public bool IsRunning;
    public LiveEngine(LiveMixer m) { mixer = m; }

    public void Start(int masterDevice, int cueDevice) {
        Stop();
        stop = false; IsRunning = true;
        thread = new System.Threading.Thread(delegate () { Run(masterDevice, cueDevice); });
        thread.IsBackground = true;
        thread.Priority = System.Threading.ThreadPriority.AboveNormal;
        thread.Start();
    }

    public void Stop() {
        stop = true;
        if (thread != null && thread.IsAlive) { try { thread.Join(1200); } catch { } }
        thread = null;
        CloseAll(); IsRunning = false;
    }

    void CloseAll() {
        if (master != IntPtr.Zero) { try { waveOutReset(master); } catch { } try { waveOutClose(master); } catch { } master = IntPtr.Zero; }
        if (cue != IntPtr.Zero) { try { waveOutReset(cue); } catch { } try { waveOutClose(cue); } catch { } cue = IntPtr.Zero; }
        lock (cueQ) cueQ.Clear();
    }

    Slot[] MakeSlots() {
        int hs = Marshal.SizeOf(typeof(Wh));
        Slot[] s = new Slot[Slots];
        for (int i = 0; i < Slots; i++) {
            s[i] = new Slot();
            s[i].Header = Marshal.AllocHGlobal(hs);
            s[i].Data = Marshal.AllocHGlobal(Frames * 4);
            Wh w = new Wh();
            Marshal.StructureToPtr(w, s[i].Header, false);
        }
        return s;
    }

    void FreeSlots(Slot[] s) {
        foreach (Slot x in s) {
            if (x.Header != IntPtr.Zero) Marshal.FreeHGlobal(x.Header);
            if (x.Data != IntPtr.Zero) Marshal.FreeHGlobal(x.Data);
        }
    }

    void Reclaim(Slot[] s, IntPtr dev, int hs) {
        if (dev == IntPtr.Zero) return;
        foreach (Slot x in s) {
            Wh w = (Wh)Marshal.PtrToStructure(x.Header, typeof(Wh));
            if ((w.Flags & Done) == 0) continue;
            waveOutUnprepareHeader(dev, x.Header, hs);
            w.Flags &= ~Prepared;
            Marshal.StructureToPtr(w, x.Header, false);
        }
    }

    Slot Free(Slot[] s) {
        foreach (Slot x in s) {
            Wh w = (Wh)Marshal.PtrToStructure(x.Header, typeof(Wh));
            if ((w.Flags & Prepared) == 0) return x;
        }
        return null;
    }

    static short[] Pcm(float[] l, float[] r) {
        short[] p = new short[l.Length * 2];
        for (int i = 0; i < l.Length; i++) {
            p[2 * i] = (short)(Math.Max(-1f, Math.Min(1f, l[i])) * 32767);
            p[2 * i + 1] = (short)(Math.Max(-1f, Math.Min(1f, r[i])) * 32767);
        }
        return p;
    }

    void Write(IntPtr dev, Slot s, short[] p, int hs) {
        Marshal.Copy(p, 0, s.Data, p.Length);
        Wh w = new Wh(); w.Data = s.Data; w.BufferLength = p.Length * 2;
        Marshal.StructureToPtr(w, s.Header, false);
        if (waveOutPrepareHeader(dev, s.Header, hs) != 0) return;
        waveOutWrite(dev, s.Header, hs);
    }

    void Run(int masterDevice, int cueDevice) {
        int hs = Marshal.SizeOf(typeof(Wh));
        Slot[] ms = MakeSlots();
        Slot[] cs = MakeSlots();
        Wf fmt = new Wf();
        fmt.FormatTag = 1; fmt.Channels = 2; fmt.SamplesPerSec = 44100;
        fmt.AvgBytesPerSec = 44100 * 4; fmt.BlockAlign = 4; fmt.BitsPerSample = 16;
        try {
            if (waveOutOpen(out master, masterDevice, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0) {
                LastError = "Could not open the master output."; IsRunning = false; return;
            }
            cueOn = cueDevice >= 0 && cueDevice != masterDevice;
            if (cueOn && waveOutOpen(out cue, cueDevice, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0) {
                cueOn = false; LastError = "Cue output unavailable; cue is silent.";
            }
            while (!stop) {
                Reclaim(ms, master, hs);
                if (cueOn) Reclaim(cs, cue, hs);
                Slot free = Free(ms);
                if (free == null) { System.Threading.Thread.Sleep(2); continue; }

                mixer.Fill(mL, mR, cL, cR, Frames);
                Write(master, free, Pcm(mL, mR), hs);

                if (cueOn) {
                    lock (cueQ) {
                        cueQ.Enqueue(Pcm(cL, cR));
                        while (cueQ.Count > Slots + 1) cueQ.Dequeue();
                    }
                    Slot cf = Free(cs);
                    short[] cp = null;
                    lock (cueQ) { if (cf != null && cueQ.Count > 0) cp = cueQ.Dequeue(); }
                    if (cf != null && cp != null) Write(cue, cf, cp, hs);
                }
            }
        } catch (Exception ex) {
            LastError = ex.Message;
        } finally {
            CloseAll(); FreeSlots(ms); FreeSlots(cs); IsRunning = false;
        }
    }
}

public class DeckWave : Control {
    float[] l = new float[0], r = new float[0];
    double ph = -1;
    public DeckWave() {
        DoubleBuffered = true; BackColor = Color.FromArgb(14, 16, 21);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }
    public double Playhead { get { return ph; } set { ph = value; Invalidate(); } }
    public void SetData(float[] left, float[] right) { l = left == null ? new float[0] : left; r = right == null ? new float[0] : right; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) {
        Graphics g = e.Graphics; g.Clear(BackColor);
        int w = Math.Max(1, Width - 2), h = Math.Max(1, Height - 2), y0 = 1 + h / 2;
        using (Pen mid = new Pen(Color.FromArgb(40, 46, 58))) g.DrawLine(mid, 1, y0, 1 + w, y0);
        int n = Math.Min(l.Length, r.Length);
        if (n < 2) { using (Brush b = new SolidBrush(Color.FromArgb(150, 158, 175))) g.DrawString("Load a project to see its waveform.", Font, b, 8, 8); return; }
        using (Pen p = new Pen(Color.FromArgb(100, 168, 255))) {
            for (int px = 0; px < w; px++) {
                int s0 = (int)((long)px * n / w), s1 = (int)((long)(px + 1) * n / w);
                if (s1 <= s0) s1 = s0 + 1; if (s1 > n) s1 = n;
                double mn = 0, mx = 0;
                for (int i = s0; i < s1; i++) { double v = (l[i] + r[i]) * 0.5; if (v < mn) mn = v; if (v > mx) mx = v; }
                g.DrawLine(p, 1 + px, y0 - (int)(mx * (h / 2 - 2)), 1 + px, y0 - (int)(mn * (h / 2 - 2)));
            }
        }
        if (ph >= 0 && ph <= 1) {
            int x = 1 + (int)(ph * (w - 1));
            using (Pen p = new Pen(Color.FromArgb(255, 168, 92), 2f)) g.DrawLine(p, x, 1, x, 1 + h);
        }
        using (Pen b = new Pen(Color.FromArgb(70, 76, 94))) g.DrawRectangle(b, 0, 0, w + 1, h + 1);
    }
}

public class LevelMeter : Control {
    double level, peak;
    public LevelMeter() {
        DoubleBuffered = true; BackColor = Color.FromArgb(14, 16, 21); Height = 12; Width = 140;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }
    public void SetLevel(double v) { level = Math.Max(0, Math.Min(1, v)); peak = Math.Max(level, peak * 0.92); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) {
        Graphics g = e.Graphics; g.Clear(BackColor);
        int w = Width - 2, h = Height - 2;
        using (Brush b = new SolidBrush(Color.FromArgb(30, 34, 44))) g.FillRectangle(b, 1, 1, w, h);
        int fill = (int)(level * w);
        if (fill > 0) {
            using (Brush b = new SolidBrush(Color.FromArgb(110, 220, 150))) g.FillRectangle(b, 1, 1, fill, h);
        }
        int pk = (int)(peak * w);
        if (pk > 0) { using (Pen p = new Pen(Color.FromArgb(230, 236, 245), 2f)) g.DrawLine(p, 1 + Math.Min(pk, w - 1), 1, 1 + Math.Min(pk, w - 1), 1 + h); }
        using (Pen p = new Pen(Color.FromArgb(70, 76, 94))) g.DrawRectangle(p, 0, 0, w + 1, h + 1);
    }
}

public class DeckPanel : UserControl {
    readonly LiveForm host;
    readonly bool isA;
    public readonly DeckWave Wave;
    readonly Label title, source, info, status, gainText, filterText;
    readonly Button play, cue, sync;
    public readonly CheckBox Loop;
    readonly TrackBar gain, filter;
    public readonly ListBox Queue;
    public readonly List<string> QueuePaths = new List<string>();
    int queueIndex = -1;

    public event Action LoadFile;
    public event Action FromMain;
    public event Action AddQueueRequested;
    public event Action NewRequested;
    public event Action<string> PathRequested;
    public event Action LoopChanged;

    public LiveDeck Deck { get { return isA ? host.Mixer.A : host.Mixer.B; } }

    public DeckPanel(LiveForm h, bool a) {
        host = h; isA = a;
        BackColor = Color.FromArgb(32, 35, 44);
        Padding = new Padding(8);

        Panel bar = new Panel();
        bar.Dock = DockStyle.Top; bar.Height = 38;
        bar.BackColor = a ? Color.FromArgb(36, 52, 78) : Color.FromArgb(64, 42, 34);
        Label tag = new Label();
        tag.Text = a ? "A" : "B"; tag.Width = 32; tag.Dock = DockStyle.Left;
        tag.TextAlign = ContentAlignment.MiddleCenter;
        tag.Font = new Font("Segoe UI Semibold", 14f);
        tag.ForeColor = Color.FromArgb(224, 228, 236);
        title = new Label();
        title.Text = a ? "Deck A" : "Deck B"; title.Dock = DockStyle.Fill;
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.Font = new Font("Segoe UI Semibold", 11f);
        title.ForeColor = Color.FromArgb(224, 228, 236);
        bar.Controls.Add(title); bar.Controls.Add(tag);

        Panel wavePanel = new Panel();
        wavePanel.Dock = DockStyle.Top; wavePanel.Height = 104; wavePanel.Padding = new Padding(0, 4, 0, 4);
        Wave = new DeckWave(); Wave.Dock = DockStyle.Fill;
        wavePanel.Controls.Add(Wave);

        Panel infoPanel = new Panel();
        infoPanel.Dock = DockStyle.Top; infoPanel.Height = 44;
        info = MakeLabel(2, 4); source = MakeLabel(2, 24); source.Font = new Font("Segoe UI", 8f);
        source.ForeColor = Color.FromArgb(150, 158, 175);
        infoPanel.Controls.Add(info); infoPanel.Controls.Add(source);

        FlowLayoutPanel transport = new FlowLayoutPanel();
        transport.Dock = DockStyle.Top; transport.Height = 70; transport.WrapContents = true;
        transport.BackColor = Color.FromArgb(32, 35, 44);
        play = Btn("Play", 74); cue = Btn("Cue", 58); sync = Btn("Sync", 56);
        Loop = new CheckBox(); Loop.Text = "Loop"; Loop.AutoSize = true; Loop.Checked = true;
        Loop.ForeColor = Color.FromArgb(224, 228, 236);
        transport.Controls.Add(play); transport.Controls.Add(cue); transport.Controls.Add(sync);
        transport.Controls.Add(Loop);
        transport.Controls.Add(Btn("Load...", 66));
        transport.Controls.Add(Btn("From main", 84));
        transport.Controls.Add(Btn("New", 54));
        transport.Controls.Add(Btn("Queue...", 70));

        TableLayoutPanel controls = new TableLayoutPanel();
        controls.Dock = DockStyle.Top; controls.Height = 86; controls.ColumnCount = 2; controls.RowCount = 1;
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        Panel gcell = new Panel(); gcell.Dock = DockStyle.Fill;
        Panel fcell = new Panel(); fcell.Dock = DockStyle.Fill;
        gainText = new Label(); gainText.AutoSize = true; gainText.ForeColor = Color.FromArgb(150, 158, 175);
        gainText.Location = new Point(2, 2); gainText.Text = "GAIN 100%";
        filterText = new Label(); filterText.AutoSize = true; filterText.ForeColor = Color.FromArgb(150, 158, 175);
        filterText.Location = new Point(2, 2); filterText.Text = "FILTER bypass";
        gain = new TrackBar(); gain.Minimum = 0; gain.Maximum = 150; gain.Value = 100; gain.TickFrequency = 25;
        gain.Location = new Point(0, 22); gain.Width = 200; gain.BackColor = Color.FromArgb(32, 35, 44);
        filter = new TrackBar(); filter.Minimum = -100; filter.Maximum = 100; filter.Value = 0; filter.TickFrequency = 25;
        filter.Location = new Point(0, 22); filter.Width = 200; filter.BackColor = Color.FromArgb(32, 35, 44);
        gcell.Controls.Add(gain); gcell.Controls.Add(gainText);
        fcell.Controls.Add(filter); fcell.Controls.Add(filterText);
        controls.Controls.Add(gcell, 0, 0); controls.Controls.Add(fcell, 1, 0);

        status = new Label(); status.AutoSize = true; status.ForeColor = Color.FromArgb(150, 158, 175);
        status.Text = "No track loaded";

        Label qcap = new Label();
        qcap.Text = "QUEUE"; qcap.Dock = DockStyle.Top; qcap.Height = 18;
        qcap.ForeColor = Color.FromArgb(40, 200, 170);
        qcap.Font = new Font("Segoe UI Semibold", 8.5f);
        FlowLayoutPanel qbtns = new FlowLayoutPanel();
        qbtns.Dock = DockStyle.Bottom; qbtns.Height = 56; qbtns.WrapContents = true;
        qbtns.BackColor = Color.FromArgb(32, 35, 44);
        qbtns.Controls.Add(Btn("Next", 60));
        qbtns.Controls.Add(Btn("Clear", 60));
        qbtns.Controls.Add(Btn("Load selected", 96));
        Queue = new ListBox();
        Queue.Dock = DockStyle.Fill; Queue.BackColor = Color.FromArgb(26, 29, 38);
        Queue.ForeColor = Color.FromArgb(224, 228, 236); Queue.BorderStyle = BorderStyle.FixedSingle;
        Queue.IntegralHeight = false;
        Panel qpanel = new Panel();
        qpanel.Dock = DockStyle.Fill;
        qpanel.Controls.Add(Queue); qpanel.Controls.Add(qbtns); qpanel.Controls.Add(qcap);

        Controls.Add(qpanel); Controls.Add(controls); Controls.Add(transport);
        Controls.Add(infoPanel); Controls.Add(wavePanel); Controls.Add(bar);

        play.Click += delegate { TogglePlay(); };
        cue.Click += delegate { Deck.Cue = !Deck.Cue; UpdateUi(); };
        sync.Click += delegate { host.Mixer.Sync(Deck); UpdateUi(); };
        Loop.CheckedChanged += delegate { if (!host.IsBuilding) { if (LoopChanged != null) LoopChanged(); } };
        gain.ValueChanged += delegate { if (!host.IsBuilding) { Deck.Gain = gain.Value / 100.0; UpdateUi(); } };
        filter.ValueChanged += delegate { if (!host.IsBuilding) { Deck.Filter = filter.Value / 100.0; UpdateUi(); } };
        Button load = (Button)transport.Controls[3 + 1];
        load.Click += delegate { if (LoadFile != null) LoadFile(); };
        Button fromMain = (Button)transport.Controls[3 + 2];
        Button newBtn = (Button)transport.Controls[3 + 3];
        fromMain.Click += delegate { if (FromMain != null) FromMain(); };
        Button queueBtn = (Button)transport.Controls[3 + 4];
        queueBtn.Click += delegate { if (AddQueueRequested != null) AddQueueRequested(); };
        newBtn.Click += delegate { if (NewRequested != null) NewRequested(); };
        ((Button)qbtns.Controls[0]).Click += delegate { Next(); };
        ((Button)qbtns.Controls[1]).Click += delegate { QueuePaths.Clear(); Queue.Items.Clear(); queueIndex = -1; };
        ((Button)qbtns.Controls[2]).Click += delegate { LoadSelected(); };
        Queue.DoubleClick += delegate { LoadSelected(); };
        UpdateUi();
    }

    Label MakeLabel(int x, int y) {
        Label l = new Label(); l.AutoSize = true; l.Location = new Point(x, y);
        l.ForeColor = Color.FromArgb(224, 228, 236); return l;
    }

    Button Btn(string text, int w) {
        Button b = new Button();
        b.Text = text; b.Width = w; b.Height = 26; b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Color.FromArgb(44, 48, 60); b.ForeColor = Color.FromArgb(224, 228, 236);
        b.FlatAppearance.BorderColor = Color.FromArgb(70, 76, 94); b.Margin = new Padding(2);
        return b;
    }

    public void SetBuffer(string name, string path, float[] l, float[] r, double bpm, string keyText, bool song) {
        Deck.SetBuffer(name, path, l, r, bpm, keyText, song);
        title.Text = "Deck " + (isA ? "A" : "B") + "  -  " + name;
        source.Text = path == null || path.Length == 0 ? "Main project" : System.IO.Path.GetFileName(path);
        Wave.SetData(l, r); Wave.Playhead = 0;
        UpdateUi();
    }

    public void SetStatus(string text) { status.Text = text; }
    public void SetLoop(bool on) { Loop.Checked = on; }

    public void AddQueue(string[] files) {
        if (files == null) return;
        foreach (string f in files) {
            QueuePaths.Add(f);
            Queue.Items.Add(System.IO.Path.GetFileNameWithoutExtension(f));
        }
    }

    void LoadSelected() {
        int i = Queue.SelectedIndex;
        if (i < 0 || i >= QueuePaths.Count) return;
        queueIndex = i;
        if (PathRequested != null) PathRequested(QueuePaths[i]);
    }

    public void Next() {
        if (QueuePaths.Count == 0) return;
        queueIndex = queueIndex < 0 ? 0 : (queueIndex + 1) % QueuePaths.Count;
        if (PathRequested != null) PathRequested(QueuePaths[queueIndex]);
    }

    public bool AdvanceIfEnded() {
        if (QueuePaths.Count == 0) return false;
        Next(); return true;
    }

    public void TogglePlay() {
        if (!Deck.HasAudio) { if (LoadFile != null) LoadFile(); return; }
        Deck.Playing = !Deck.Playing;
        if (Deck.Playing && Deck.PositionSeconds >= Deck.Seconds - 0.01) Deck.Restart();
        UpdateUi();
    }

    public void UpdateUi() {
        host.IsBuilding = true;
        try {
            play.Text = Deck.Playing ? "Pause" : "Play";
            play.BackColor = Deck.Playing ? Color.FromArgb(54, 120, 84) : Color.FromArgb(54, 92, 156);
            cue.BackColor = Deck.Cue ? Color.FromArgb(255, 168, 92) : Color.FromArgb(44, 48, 60);
            sync.BackColor = Math.Abs(Deck.Rate - 1) > 0.001 ? Color.FromArgb(54, 92, 156) : Color.FromArgb(44, 48, 60);
            gain.Value = Math.Max(gain.Minimum, Math.Min(gain.Maximum, (int)Math.Round(Deck.Gain * 100)));
            filter.Value = Math.Max(filter.Minimum, Math.Min(filter.Maximum, (int)Math.Round(Deck.Filter * 100)));
            info.Text = Deck.HasAudio
                ? Deck.Bpm.ToString("0.##") + " BPM  -  " + Deck.KeyText + "  -  " + Deck.Seconds.ToString("0.0") + " s  -  rate " + Deck.Rate.ToString("0.000") + "x  -  " + (Deck.Loop ? "loop" : "song")
                : "No track loaded";
            if (Deck.Seconds > 0) Wave.Playhead = Math.Max(0, Math.Min(1, Deck.PositionSeconds / Deck.Seconds));
            else Wave.Playhead = -1;
            status.Text = Deck.HasAudio ? (Deck.Playing ? "Playing" : "Ready") : "Load a project to begin";
            gainText.Text = "GAIN " + ((int)Math.Round(Deck.Gain * 100)) + "%";
            filterText.Text = Math.Abs(Deck.Filter) < 0.01 ? "FILTER bypass" : (Deck.Filter < 0 ? "FILTER LP " + ((int)Math.Round(-Deck.Filter * 100)) + "%" : "FILTER HP " + ((int)Math.Round(Deck.Filter * 100)) + "%");
        } finally {
            host.IsBuilding = false;
        }
    }
}

public class LiveForm : Form {
    public LiveMixer Mixer = new LiveMixer();
    public LiveEngine Engine;
    public bool IsBuilding;

    readonly DeckPanel deckA, deckB;
    readonly ComboBox masterOut, cueOut;
    readonly NumericUpDown masterGain, cueLevel;
    readonly CheckBox cueToMaster;
    TrackBar crossfader;
    LevelMeter masterMeter, cueMeter;
    readonly Label engineState, state;
    readonly Button audioBtn;
    readonly Timer timer;
    bool advA, advB;

    public event Action<bool> LoadFileRequested;
    public event Action<bool> FromMainRequested;
    public event Action<bool> NewRequested;
    public event Action<bool> AddQueueRequested;
    public event Action<bool, string> PathRequested;
    public event Action<bool> LoopChanged;

    public LiveForm() {
        Engine = new LiveEngine(Mixer);
        Text = "Live Performance - Humble Baker Studio";
        Size = new Size(1420, 820);
        MinimumSize = new Size(1100, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 26, 32);
        ForeColor = Color.FromArgb(224, 228, 236);
        Font = new Font("Segoe UI", 9f);
        AutoScaleMode = AutoScaleMode.Dpi;

        FlowLayoutPanel top = new FlowLayoutPanel();
        top.Dock = DockStyle.Top; top.Height = 82; top.WrapContents = true;
        top.BackColor = Color.FromArgb(32, 35, 44); top.Padding = new Padding(8, 6, 4, 4);
        masterOut = Combo(230); cueOut = Combo(230);
        masterGain = Num(85); cueLevel = Num(80);
        cueToMaster = new CheckBox(); cueToMaster.Text = "Cue on master"; cueToMaster.AutoSize = true;
        cueToMaster.ForeColor = Color.FromArgb(224, 228, 236);
        audioBtn = new Button();
        audioBtn.Text = "Audio off"; audioBtn.Width = 90; audioBtn.Height = 26; audioBtn.FlatStyle = FlatStyle.Flat;
        Button apply = new Button();
        apply.Text = "Apply outputs"; apply.Width = 100; apply.Height = 26; apply.FlatStyle = FlatStyle.Flat;
        engineState = Small("Audio: stopped"); state = Small("120 BPM  -  120 BPM");
        top.Controls.Add(Small("Master out")); top.Controls.Add(masterOut);
        top.Controls.Add(Small("Cue out")); top.Controls.Add(cueOut);
        top.Controls.Add(apply); top.Controls.Add(audioBtn);
        top.Controls.Add(Small("Master")); top.Controls.Add(masterGain);
        top.Controls.Add(Small("Cue")); top.Controls.Add(cueLevel);
        top.Controls.Add(cueToMaster); top.Controls.Add(engineState); top.Controls.Add(state);

        deckA = new DeckPanel(this, true); deckA.Dock = DockStyle.Fill;
        deckB = new DeckPanel(this, false); deckB.Dock = DockStyle.Fill;

        TableLayoutPanel cols = new TableLayoutPanel();
        cols.Dock = DockStyle.Fill; cols.ColumnCount = 3; cols.RowCount = 1;
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        cols.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cols.Controls.Add(deckA, 0, 0); cols.Controls.Add(BuildCenter(), 1, 0); cols.Controls.Add(deckB, 2, 0);

        Controls.Add(cols); Controls.Add(top);

        deckA.LoadFile += delegate { if (LoadFileRequested != null) LoadFileRequested(true); };
        deckB.LoadFile += delegate { if (LoadFileRequested != null) LoadFileRequested(false); };
        deckA.FromMain += delegate { if (FromMainRequested != null) FromMainRequested(true); };
        deckB.FromMain += delegate { if (FromMainRequested != null) FromMainRequested(false); };
        deckA.NewRequested += delegate { if (NewRequested != null) NewRequested(true); };
        deckB.NewRequested += delegate { if (NewRequested != null) NewRequested(false); };
        deckA.AddQueueRequested += delegate { if (AddQueueRequested != null) AddQueueRequested(true); };
        deckB.AddQueueRequested += delegate { if (AddQueueRequested != null) AddQueueRequested(false); };
        deckA.PathRequested += delegate (string p) { if (PathRequested != null) PathRequested(true, p); };
        deckB.PathRequested += delegate (string p) { if (PathRequested != null) PathRequested(false, p); };
        deckA.LoopChanged += delegate { if (LoopChanged != null) LoopChanged(true); };
        deckB.LoopChanged += delegate { if (LoopChanged != null) LoopChanged(false); };

        crossfader.ValueChanged += delegate { Mixer.Crossfade = (crossfader.Value + 100) / 200.0; };
        masterGain.ValueChanged += delegate { Mixer.MasterGain = (double)masterGain.Value / 100.0; };
        cueLevel.ValueChanged += delegate { Mixer.CueLevel = (double)cueLevel.Value / 100.0; };
        cueToMaster.CheckedChanged += delegate { Mixer.CueToMaster = cueToMaster.Checked; };
        apply.Click += delegate { StartAudio(); };
        audioBtn.Click += delegate { if (Engine.IsRunning) StopAudio(); else StartAudio(); };

        timer = new Timer(); timer.Interval = 60;
        timer.Tick += delegate { Tick(); };
        Shown += delegate { LoadDevices(); StartAudio(); timer.Start(); };
        FormClosing += delegate { timer.Stop(); Engine.Stop(); Mixer.A.Stop(); Mixer.B.Stop(); };
        ApplyTheme(this);
    }

    Label Small(string text) {
        Label l = new Label(); l.Text = text; l.AutoSize = true;
        l.ForeColor = Color.FromArgb(150, 158, 175); l.Margin = new Padding(8, 8, 2, 2);
        return l;
    }

    ComboBox Combo(int w) {
        ComboBox c = new ComboBox(); c.Width = w; c.DropDownStyle = ComboBoxStyle.DropDownList;
        c.BackColor = Color.FromArgb(44, 48, 60); c.ForeColor = Color.FromArgb(224, 228, 236); c.FlatStyle = FlatStyle.Flat;
        return c;
    }

    NumericUpDown Num(int v) {
        NumericUpDown n = new NumericUpDown(); n.Minimum = 0; n.Maximum = 150; n.Value = v; n.Width = 56;
        n.BackColor = Color.FromArgb(44, 48, 60); n.ForeColor = Color.FromArgb(224, 228, 236);
        return n;
    }

    Control BuildCenter() {
        Panel panel = new Panel(); panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.FromArgb(32, 35, 44); panel.Padding = new Padding(10);
        FlowLayoutPanel stack = new FlowLayoutPanel();
        stack.Dock = DockStyle.Fill; stack.FlowDirection = FlowDirection.TopDown;
        stack.WrapContents = false; stack.AutoScroll = true; stack.BackColor = Color.FromArgb(32, 35, 44);
        panel.Controls.Add(stack);

        Label cap = new Label(); cap.Text = "CROSSFADER"; cap.AutoSize = true;
        cap.ForeColor = Color.FromArgb(40, 200, 170); cap.Font = new Font("Segoe UI Semibold", 8.5f);
        cap.Margin = new Padding(0, 8, 0, 0);
        crossfader = new TrackBar(); crossfader.Minimum = -100; crossfader.Maximum = 100;
        crossfader.TickFrequency = 25; crossfader.Value = 0; crossfader.Width = 230;
        crossfader.BackColor = Color.FromArgb(32, 35, 44);
        stack.Controls.Add(cap); stack.Controls.Add(crossfader);
        FlowLayoutPanel ab = new FlowLayoutPanel(); ab.AutoSize = true; ab.WrapContents = false;
        ab.BackColor = Color.FromArgb(32, 35, 44);
        ab.Controls.Add(Small("A")); ab.Controls.Add(Small("<-  blend  ->")); ab.Controls.Add(Small("B"));
        stack.Controls.Add(ab);
        Button centre = new Button(); centre.Text = "Centre"; centre.Width = 80; centre.Height = 26;
        centre.FlatStyle = FlatStyle.Flat; centre.Click += delegate { crossfader.Value = 0; };
        stack.Controls.Add(centre);
        stack.Controls.Add(Spacer());
        stack.Controls.Add(Small("MASTER")); masterMeter = new LevelMeter(); masterMeter.Width = 210; stack.Controls.Add(masterMeter);
        stack.Controls.Add(Small("CUE / HEADPHONES")); cueMeter = new LevelMeter(); cueMeter.Width = 210; stack.Controls.Add(cueMeter);
        stack.Controls.Add(Spacer());
        Button pause = new Button(); pause.Text = "Pause both"; pause.Width = 110; pause.Height = 26; pause.FlatStyle = FlatStyle.Flat;
        pause.Click += delegate { Mixer.A.Playing = false; Mixer.B.Playing = false; deckA.UpdateUi(); deckB.UpdateUi(); };
        Button stop = new Button(); stop.Text = "Stop both"; stop.Width = 110; stop.Height = 26; stop.FlatStyle = FlatStyle.Flat;
        stop.Click += delegate { Mixer.A.Stop(); Mixer.B.Stop(); deckA.UpdateUi(); deckB.UpdateUi(); };
        stack.Controls.Add(pause); stack.Controls.Add(stop); stack.Controls.Add(Spacer());
        Label help = new Label();
        help.Text = "Load a project into each deck. Play A, cue B in headphones, press Sync to match tempo, then move the crossfader.\n\nCue needs a second Windows output. Choose (none) to keep cue silent.\n\nLoop plays the current pattern; turn it off to render the song.";
        help.AutoSize = false; help.Width = 230; help.Height = 240;
        help.ForeColor = Color.FromArgb(150, 158, 175); help.Font = new Font("Segoe UI", 8.25f);
        stack.Controls.Add(help);
        return panel;
    }

    Control Spacer() { Panel p = new Panel(); p.Width = 8; p.Height = 8; p.BackColor = Color.FromArgb(32, 35, 44); return p; }

    void ApplyTheme(Control root) {
        root.BackColor = root is Form ? Color.FromArgb(24, 26, 32) : root.BackColor;
        root.ForeColor = Color.FromArgb(224, 228, 236);
        foreach (Control c in root.Controls) ApplyTheme(c);
    }

    void LoadDevices() {
        string[] devs = LiveDevices.Get();
        masterOut.Items.Clear(); cueOut.Items.Clear(); cueOut.Items.Add("(none)");
        foreach (string d in devs) { masterOut.Items.Add(d); cueOut.Items.Add(d); }
        if (devs.Length == 0) { masterOut.Items.Add("(no output devices)"); masterOut.Enabled = false; }
        if (masterOut.Items.Count > 0) masterOut.SelectedIndex = 0;
        cueOut.SelectedIndex = 0;
    }

    void StartAudio() {
        int m = masterOut.SelectedIndex < 0 ? -1 : masterOut.SelectedIndex;
        int c = cueOut.SelectedIndex <= 0 ? -1 : cueOut.SelectedIndex - 1;
        Engine.Start(m, c);
        audioBtn.Text = "Audio on"; audioBtn.BackColor = Color.FromArgb(54, 120, 84);
        engineState.Text = c >= 0 ? "Audio: master + cue" : "Audio: master only";
    }

    void StopAudio() {
        Engine.Stop(); audioBtn.Text = "Audio off"; audioBtn.BackColor = Color.FromArgb(44, 48, 60);
        engineState.Text = "Audio: stopped";
    }

    void Tick() {
        deckA.UpdateUi(); deckB.UpdateUi();
        masterMeter.SetLevel(Mixer.LastMasterPeak); cueMeter.SetLevel(Mixer.LastCuePeak);
        if (!Engine.IsRunning && Engine.LastError.Length > 0) engineState.Text = Engine.LastError;
        if (Mixer.A.Ended && !Mixer.A.Playing && !advA && deckA.AdvanceIfEnded()) advA = true;
        if (Mixer.B.Ended && !Mixer.B.Playing && !advB && deckB.AdvanceIfEnded()) advB = true;
        state.Text = Mixer.A.Bpm.ToString("0.#") + " BPM  -  " + Mixer.B.Bpm.ToString("0.#") + " BPM";
    }

    public bool GetLoop(bool isA) { return isA ? deckA.Loop.Checked : deckB.Loop.Checked; }
    public void SetLoop(bool isA, bool on) { if (isA) deckA.SetLoop(on); else deckB.SetLoop(on); }

    public void SetDeckBuffer(bool isA, string name, string path, float[] l, float[] r, double bpm, string keyText, bool song) {
        if (isA) { deckA.SetBuffer(name, path, l, r, bpm, keyText, song); advA = false; }
        else { deckB.SetBuffer(name, path, l, r, bpm, keyText, song); advB = false; }
        state.Text = "Loaded deck " + (isA ? "A" : "B") + ": " + name;
    }

    public void SetDeckStatus(bool isA, string text) { if (isA) deckA.SetStatus(text); else deckB.SetStatus(text); }
    public void AddQueue(bool isA, string[] files) { if (isA) deckA.AddQueue(files); else deckB.AddQueue(files); }
    public void ShowStatus(string text) { state.Text = text; }
}
}
'@
}

$script:LivePerformanceForm = $null

function Get-LiveProjectFile([string]$path) {
    $json = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
    return (Read-StudioProjectData $json)
}

function Set-LiveDeckProject($form, [bool]$isA, $proj, [string]$path) {
    if ($null -eq $proj) { return }
    if ($null -eq $script:LiveState) { $script:LiveState = @{ ProjA = $null; ProjB = $null; PathA = ''; PathB = '' } }
    $song = (-not $form.GetLoop($isA)) -and ($proj.Song.Count -gt 0)
    $form.SetDeckStatus($isA, $(if ($song) { 'Rendering song...' } else { 'Rendering loop...' }))
    $form.Refresh()
    try {
        $l = $null; $r = $null
        [HbStudio.Dsp]::Mix($proj, $song, (-not $song), [ref]$l, [ref]$r)
        $names = @('C','C#','D','D#','E','F','F#','G','G#','A','A#','B')
        $pc = (($proj.Key % 12) + 12) % 12
        $key = $names[$pc] + ' ' + $proj.Scale
        $name = if ($path) { [IO.Path]::GetFileNameWithoutExtension($path) } else { 'Main project' }
        $form.SetDeckBuffer($isA, $name, $path, $l, $r, $proj.Bpm, $key, $song)
        if ($isA) { $script:LiveState.ProjA = $proj; $script:LiveState.PathA = $path }
        else { $script:LiveState.ProjB = $proj; $script:LiveState.PathB = $path }
    } catch {
        $form.SetDeckStatus($isA, 'Render failed')
        [Windows.Forms.MessageBox]::Show(($_ | Out-String), 'Live performance') | Out-Null
    }
}
function Show-LivePerformance {
    param($CurrentProject)

    if ($script:LivePerformanceForm -and -not $script:LivePerformanceForm.IsDisposed) {
        $script:LivePerformanceForm.Show()
        $script:LivePerformanceForm.Activate()
        return
    }

    $form = New-Object HbLive.LiveForm
    $script:LivePerformanceForm = $form
    $script:LiveState = @{ ProjA = $null; ProjB = $null; PathA = ''; PathB = '' }

    $form.add_LoadFileRequested({
        param($isA)
        $dlg = New-Object Windows.Forms.OpenFileDialog
        $dlg.Filter = 'Humble Baker project (*.json;*.hbstudio.json)|*.json;*.hbstudio.json|All files (*.*)|*.*'
        $dlg.Title = 'Load a project into deck ' + $(if ($isA) { 'A' } else { 'B' })
        if ($dlg.ShowDialog($form) -ne [Windows.Forms.DialogResult]::OK) { return }
        try { Set-LiveDeckProject $form $isA (Get-LiveProjectFile $dlg.FileName) $dlg.FileName }
        catch { [Windows.Forms.MessageBox]::Show(($_ | Out-String), 'Live performance') | Out-Null }
    })

    $form.add_FromMainRequested({
        param($isA)
        $src = if ($script:S -and $script:S.Proj) { $script:S.Proj } else { $CurrentProject }
        if ($null -eq $src) { return }
        $clone = Read-StudioProjectData (Get-StudioProjectData $src)
        Set-LiveDeckProject $form $isA $clone ''
    })

    $form.add_NewRequested({
        param($isA)
        $proj = New-StudioProject
        try { Get-StudioDemoNotes $proj } catch { }
        Set-LiveDeckProject $form $isA $proj 'New project'
    })
    $form.add_AddQueueRequested({
        param($isA)
        $dlg = New-Object Windows.Forms.OpenFileDialog
        $dlg.Filter = 'Humble Baker project (*.json;*.hbstudio.json)|*.json;*.hbstudio.json|All files (*.*)|*.*'
        $dlg.Multiselect = $true
        $dlg.Title = 'Add projects to the deck queue'
        if ($dlg.ShowDialog($form) -ne [Windows.Forms.DialogResult]::OK) { return }
        $form.AddQueue($isA, $dlg.FileNames)
    })

    $form.add_PathRequested({
        param($isA, $path)
        try { Set-LiveDeckProject $form $isA (Get-LiveProjectFile $path) $path }
        catch { [Windows.Forms.MessageBox]::Show(($_ | Out-String), 'Live performance') | Out-Null }
    })

    $form.add_LoopChanged({
        param($isA)
        $proj = if ($isA) { $script:LiveState.ProjA } else { $script:LiveState.ProjB }
        $path = if ($isA) { $script:LiveState.PathA } else { $script:LiveState.PathB }
        if ($null -ne $proj) { Set-LiveDeckProject $form $isA $proj $path }
    })

    $form.Show()
}
