namespace HumbleBakerStudio.Audio;


// Minimal OSC UDP client for the SuperCollider live bridge. Sends to sclang on 57120,
// receives status/meter replies on 57121.
public class OscUdp : IDisposable {
    UdpClient udp;
    IPEndPoint sendTo;

    public OscUdp(int listenPort = 57121, int sendPort = 57120) {
        udp = new UdpClient(listenPort);
        sendTo = new IPEndPoint(IPAddress.Loopback, sendPort);
    }

    static void WriteString(BinaryWriter w, string s) {
        byte[] b = Encoding.UTF8.GetBytes(s ?? "");
        w.Write(b); w.Write((byte)0);
        while ((w.BaseStream.Position % 4) != 0) w.Write((byte)0);
    }

    // PowerShell passes values from variables inside a PSObject wrapper. Left alone, the
    // wrapper is neither string nor number and every value would be coerced to int.
    static object Unwrap(object a) {
        if (a == null) return null;
        if (a.GetType().FullName != "System.Management.Automation.PSObject") return a;
        string s = a.ToString();
        double d;
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
        return s;
    }

    public void Send(string address, params object[] args) {
        object[] vals = new object[args == null ? 0 : args.Length];
        for (int i = 0; i < vals.Length; i++) vals[i] = Unwrap(args[i]);
        using (MemoryStream ms = new MemoryStream()) {
            using (BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8, true)) {
                WriteString(w, address);
                StringBuilder types = new StringBuilder(",");
                foreach (object a in vals) {
                    if (a is string) types.Append('s');
                    else if (a is float || a is double) types.Append('f');
                    else if (a is bool) types.Append('i');
                    else types.Append('i');
                }
                WriteString(w, types.ToString());
                foreach (object a in vals) {
                    if (a is string) WriteString(w, (string)a);
                    else if (a is float) w.Write((float)a);
                    else if (a is double) w.Write((float)(double)a);
                    else if (a is bool) w.Write((bool)a ? 1 : 0);
                    else w.Write(Convert.ToInt32(a, CultureInfo.InvariantCulture));
                }
            }
            byte[] data = ms.ToArray();
            try { udp.Send(data, data.Length, sendTo); } catch { }
        }
    }

    // Returns human-readable replies. Parsing is intentionally light: the bridge only needs
    // ready/error addresses and a short argument summary for the log.
    public string[] Drain() {
        List<string> lines = new List<string>();
        try {
            while (udp.Available > 0) {
                IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udp.Receive(ref from);
                int end = Array.IndexOf(data, (byte)0);
                string addr = end > 0 ? Encoding.UTF8.GetString(data, 0, end) : "(osc)";
                lines.Add(addr);
            }
        } catch { }
        return lines.ToArray();
    }

    public void Dispose() { try { udp.Close(); } catch { } }
}
