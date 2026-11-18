namespace HumbleBakerStudio.Audio;


// A synth patch with the track's patch-parameter automation lanes applied at one moment in the song (values snap to 32 steps so notes can share renders).
// Multiple lanes that share the same patch parameter are layered: their offsets from the patch's current
// value are summed, so the spectral automation painter can stack many bands onto one parameter.
public static class PatchAuto {
    public static Patch Resolve(Patch basePatch, List<AutoLane> lanes, double beat, out string sig) {
        Patch pv = null; System.Text.StringBuilder sb = new System.Text.StringBuilder();
        Dictionary<string, List<AutoLane>> groups = new Dictionary<string, List<AutoLane>>();
        foreach (AutoLane al in lanes) {
            if (al == null || !al.Param.StartsWith("p:")) continue;
            string name = al.Param.Substring(2);
            if (!groups.ContainsKey(name)) groups[name] = new List<AutoLane>();
            groups[name].Add(al);
        }
        foreach (KeyValuePair<string, List<AutoLane>> kv in groups) {
            string name = kv.Key; List<AutoLane> list = kv.Value;
            System.Reflection.PropertyInfo pi = PropIO.Find(basePatch, name); if (pi == null || pi.PropertyType != typeof(double)) continue;
            RngAttribute r = PropIO.RangeOf(pi);
            double v;
            if (list.Count == 1) {
                v = list[0].At(beat); if (double.IsNaN(v)) continue;
            } else {
                double base0 = Convert.ToDouble(pi.GetValue(basePatch, null), CultureInfo.InvariantCulture);
                double sum = 0; bool any = false;
                foreach (AutoLane al in list) { double x = al.At(beat); if (double.IsNaN(x)) continue; sum += x - base0; any = true; }
                if (!any) continue;
                v = base0 + sum;
            }
            if (r != null) { double step = (r.Max - r.Min) / 32.0; v = Math.Max(r.Min, Math.Min(r.Max, r.Min + Math.Round((v - r.Min) / step) * step)); }
            if (pv == null) pv = basePatch.Clone();
            pi.SetValue(pv, v, null); sb.Append(name).Append('=').Append(v.ToString("0.###", CultureInfo.InvariantCulture)).Append(';');
        }
        sig = sb.ToString(); return pv;
    }
}
