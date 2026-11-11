namespace HumbleBakerStudio.Audio;


// Windows MIDI input (winmm). Raw events are queued for the UI: kind 1 = note on, 0 = note off,
// 2 = control change, 3 = pitch bend; data1/data2 are the MIDI bytes and data3 is TickCount.
public class MidiInPort : IDisposable {
    [DllImport("winmm.dll")] static extern int midiInGetNumDevs();
    [DllImport("winmm.dll")] static extern int midiInGetDevCaps(IntPtr uDeviceID, ref MIDIINCAPS caps, int cbMidiInCaps);
    [DllImport("winmm.dll")] static extern int midiInOpen(out IntPtr hMidiIn, int uDeviceID, MidiInProc lpCallback, IntPtr dwInstance, int dwFlags);
    [DllImport("winmm.dll")] static extern int midiInStart(IntPtr hMidiIn);
    [DllImport("winmm.dll")] static extern int midiInStop(IntPtr hMidiIn);
    [DllImport("winmm.dll")] static extern int midiInReset(IntPtr hMidiIn);
    [DllImport("winmm.dll")] static extern int midiInClose(IntPtr hMidiIn);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MIDIINCAPS {
        public short wMid; public short wPid; public int vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szPname;
    }
    delegate void MidiInProc(IntPtr hMidiIn, int wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

    const int CALLBACK_FUNCTION = 0x00030000, MIM_DATA = 0x3C3;
    IntPtr handle = IntPtr.Zero; MidiInProc callback;
    readonly List<int[]> events = new List<int[]>();
    readonly object gate = new object();
    public bool IsOpen = false; public string LastError = ""; public int DeviceIndex = -1; public string DeviceName = "";

    public static string[] GetDevices() {
        int n = midiInGetNumDevs(); if (n < 0) n = 0;
        string[] r = new string[n];
        for (int i = 0; i < n; i++) {
            MIDIINCAPS c = new MIDIINCAPS();
            midiInGetDevCaps((IntPtr)i, ref c, Marshal.SizeOf(typeof(MIDIINCAPS)));
            r[i] = string.IsNullOrEmpty(c.szPname) ? ("MIDI device " + (i + 1)) : c.szPname;
        }
        return r;
    }
    public bool Open(int index) {
        Close();
        callback = OnMessage;
        int rc = midiInOpen(out handle, index, callback, IntPtr.Zero, CALLBACK_FUNCTION);
        if (rc != 0) { LastError = "midiInOpen failed (" + rc + ")"; handle = IntPtr.Zero; return false; }
        rc = midiInStart(handle);
        if (rc != 0) { LastError = "midiInStart failed (" + rc + ")"; midiInClose(handle); handle = IntPtr.Zero; return false; }
        DeviceIndex = index; IsOpen = true; LastError = ""; return true;
    }
    public void Close() {
        if (handle != IntPtr.Zero) {
            try { midiInStop(handle); } catch { }
            try { midiInReset(handle); } catch { }
            try { midiInClose(handle); } catch { }
            handle = IntPtr.Zero;
        }
        IsOpen = false;
    }
    void OnMessage(IntPtr h, int msg, IntPtr inst, IntPtr p1, IntPtr p2) {
        if (msg != MIM_DATA) return;
        int raw = p1.ToInt32(), status = raw & 0xFF, d1 = (raw >> 8) & 0x7F, d2 = (raw >> 16) & 0x7F, kind = -1;
        if (status >= 0x90 && status <= 0x9F) kind = d2 > 0 ? 1 : 0;
        else if (status >= 0x80 && status <= 0x8F) kind = 0;
        else if (status >= 0xB0 && status <= 0xBF) kind = 2;
        else if (status >= 0xE0 && status <= 0xEF) kind = 3;
        if (kind < 0) return;
        lock (gate) { events.Add(new int[] { kind, d1, d2, Environment.TickCount }); if (events.Count > 8192) events.RemoveRange(0, 4096); }
    }
    public int[][] Drain() { lock (gate) { int[][] r = events.ToArray(); events.Clear(); return r; } }
    public void Dispose() { Close(); }
}
