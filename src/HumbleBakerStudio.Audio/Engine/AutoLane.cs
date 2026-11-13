namespace HumbleBakerStudio.Audio;


// Automation of one track parameter over time (beats). Params: vol (gain 0-2), pan (-1..1), lp (low-pass Hz), hp (high-pass Hz),
// and "p:Name" for any numeric synth-patch parameter (for example p:FmAmount or p:Morph) so a sound can develop through the song.
// Before the first point the parameter is neutral (patch lanes: unchanged); after the last point the last value is held.
public class AutoLane {
    public int Track; public string Param = "vol"; public List<double[]> Points = new List<double[]>();
    // Display-only painted-canvas snapshot (deflated 8-bit cells). Never used by the renderer.
    public byte[]? Preview; public int PreviewCols, PreviewRows;
    public AutoLane() { }
    public AutoLane(int track, string param) { Track = track; Param = param; }
    public AutoLane Clone() {
        AutoLane a = new AutoLane(Track, Param);
        foreach (double[] p in Points) a.Points.Add((double[])p.Clone());
        if (Preview != null) a.Preview = (byte[])Preview.Clone();
        a.PreviewCols = PreviewCols; a.PreviewRows = PreviewRows;
        return a;
    }
    public static double Neutral(string param) { return param.StartsWith("p:") ? double.NaN : param == "lp" ? 20000 : param == "hp" ? 20 : param == "pan" ? 0 : 1; }
    public double At(double beat) {
        int n = Points.Count;
        if (n == 0 || beat < Points[0][0]) return Neutral(Param);
        if (beat >= Points[n - 1][0]) return Points[n - 1][1];

        // Points are sorted by beat. Binary search keeps very dense lanes (tens of
        // thousands of points) cheap during playback instead of scanning every point.
        int lo = 0, hi = n - 1;
        while (lo + 1 < hi) {
            int mid = (lo + hi) >> 1;
            if (Points[mid][0] <= beat) lo = mid; else hi = mid;
        }

        double a = Points[lo][0], b = Points[hi][0];
        double t = b - a < 1e-9 ? 1 : (beat - a) / (b - a);
        double va = Points[lo][1], vb = Points[hi][1];
        if ((Param == "lp" || Param == "hp") && va > 0 && vb > 0) return Math.Exp(Math.Log(va) * (1 - t) + Math.Log(vb) * t);
        return va + (vb - va) * t;
    }
}
