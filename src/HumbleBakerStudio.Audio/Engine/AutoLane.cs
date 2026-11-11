namespace HumbleBakerStudio.Audio;


// Automation of one track parameter over time (beats). Params: vol (gain 0-2), pan (-1..1), lp (low-pass Hz), hp (high-pass Hz),
// and "p:Name" for any numeric synth-patch parameter (for example p:FmAmount or p:Morph) so a sound can develop through the song.
// Before the first point the parameter is neutral (patch lanes: unchanged); after the last point the last value is held.
public class AutoLane {
    public int Track; public string Param = "vol"; public List<double[]> Points = new List<double[]>();
    public AutoLane() { }
    public AutoLane(int track, string param) { Track = track; Param = param; }
    public AutoLane Clone() { AutoLane a = new AutoLane(Track, Param); foreach (double[] p in Points) a.Points.Add(new double[] { p[0], p[1] }); return a; }
    public static double Neutral(string param) { return param.StartsWith("p:") ? double.NaN : param == "lp" ? 20000 : param == "hp" ? 20 : param == "pan" ? 0 : 1; }
    public double At(double beat) {
        if (Points.Count == 0 || beat < Points[0][0]) return Neutral(Param);
        for (int i = 0; i < Points.Count - 1; i++) {
            double a = Points[i][0], b = Points[i + 1][0];
            if (beat >= a && beat < b) {
                double t = b - a < 1e-9 ? 1 : (beat - a) / (b - a), va = Points[i][1], vb = Points[i + 1][1];
                if ((Param == "lp" || Param == "hp") && va > 0 && vb > 0) return Math.Exp(Math.Log(va) * (1 - t) + Math.Log(vb) * t);
                return va + (vb - va) * t;
            }
        }
        return Points[Points.Count - 1][1];
    }
}
