namespace HumbleBakerStudio.Audio;


public static class MidiWriter {
    static void Vlq(List<byte> b, long v) {
        Stack<byte> s = new Stack<byte>();
        s.Push((byte)(v & 0x7F)); v >>= 7;
        while (v > 0) { s.Push((byte)((v & 0x7F) | 0x80)); v >>= 7; }
        while (s.Count > 0) b.Add(s.Pop());
    }
    static void Be32(List<byte> b, int v) { b.Add((byte)(v >> 24)); b.Add((byte)(v >> 16)); b.Add((byte)(v >> 8)); b.Add((byte)v); }
    static void Chunk(Stream os, string id, List<byte> body) {
        foreach (char c in id) os.WriteByte((byte)c);
        List<byte> len = new List<byte>(); Be32(len, body.Count);
        os.Write(len.ToArray(), 0, 4); os.Write(body.ToArray(), 0, body.Count);
    }

    public static void Write(Project p, string path) {
        const int PPQ = 480;
        using (FileStream fs = new FileStream(path, FileMode.Create)) {
            List<byte> head = new List<byte>();
            head.Add(0); head.Add(1); head.Add(0); head.Add((byte)(p.Tracks.Count + 1)); head.Add((byte)(PPQ >> 8)); head.Add((byte)(PPQ & 0xFF));
            Chunk(fs, "MThd", head);
            List<byte> tr = new List<byte>();
            int us = (int)Math.Round(60000000.0 / p.Bpm);
            Vlq(tr, 0); tr.Add(0xFF); tr.Add(0x51); tr.Add(3); tr.Add((byte)(us >> 16)); tr.Add((byte)(us >> 8)); tr.Add((byte)us);
            Vlq(tr, 0); tr.Add(0xFF); tr.Add(0x58); tr.Add(4); tr.Add((byte)p.BeatsPerBar); tr.Add((byte)Math.Round(Math.Log(Math.Max(1, p.BeatUnit), 2))); tr.Add(24); tr.Add(8);
            Vlq(tr, 0); tr.Add(0xFF); tr.Add(0x2F); tr.Add(0);
            Chunk(fs, "MTrk", tr);
            int ch = 0;
            foreach (Track t in p.Tracks) {
                int channel;
                if (t.Synth == "drums") channel = 9; else { if (ch == 9) ch++; channel = ch % 16; ch++; }
                List<long[]> ev = new List<long[]>();
                foreach (Note n in t.Notes) {
                    long on = (long)Math.Round(n.Start * PPQ), off = on + Math.Max(1, (long)Math.Round(n.Len * PPQ));
                    ev.Add(new long[] { on, 1, n.Pitch, n.Vel }); ev.Add(new long[] { off, 0, n.Pitch, 0 });
                }
                ev.Sort(delegate (long[] a, long[] b) { int c = a[0].CompareTo(b[0]); return c != 0 ? c : a[1].CompareTo(b[1]); });
                List<byte> body = new List<byte>();
                byte[] name = System.Text.Encoding.ASCII.GetBytes(t.Name ?? "Track");
                Vlq(body, 0); body.Add(0xFF); body.Add(0x03); Vlq(body, name.Length); body.AddRange(name);
                long last = 0;
                foreach (long[] e in ev) {
                    Vlq(body, e[0] - last); last = e[0];
                    body.Add((byte)((e[1] == 1 ? 0x90 : 0x80) | channel)); body.Add((byte)e[2]); body.Add((byte)e[3]);
                }
                Vlq(body, 0); body.Add(0xFF); body.Add(0x2F); body.Add(0);
                Chunk(fs, "MTrk", body);
            }
        }
    }
}
