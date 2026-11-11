namespace HumbleBakerStudio.Audio;


// A synth patch with the track's patch-parameter automation lanes applied at one moment in the song (values snap to 32 steps so notes can share renders).
public static class PatchAuto {
    public static Patch Resolve(Patch basePatch, List<AutoLane> lanes, double beat, out string sig) {
        Patch pv = null; System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (AutoLane al in lanes) {
            double v = al.At(beat); if (double.IsNaN(v)) continue;
            string name = al.Param.Substring(2);
            System.Reflection.PropertyInfo pi = PropIO.Find(basePatch, name); if (pi == null || pi.PropertyType != typeof(double)) continue;
            RngAttribute r = PropIO.RangeOf(pi);
            if (r != null) { double step = (r.Max - r.Min) / 32.0; v = Math.Max(r.Min, Math.Min(r.Max, r.Min + Math.Round((v - r.Min) / step) * step)); }
            if (pv == null) pv = basePatch.Clone();
            pi.SetValue(pv, v, null); sb.Append(name).Append('=').Append(v.ToString("0.###", CultureInfo.InvariantCulture)).Append(';');
        }
        sig = sb.ToString(); return pv;
    }
}
