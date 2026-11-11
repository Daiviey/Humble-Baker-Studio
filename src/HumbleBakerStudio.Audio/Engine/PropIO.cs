namespace HumbleBakerStudio.Audio;


// Reflection helpers so the UI grid, the AI and the randomizer all share the same bounded parameter set.
public static class PropIO {
    public static System.Reflection.PropertyInfo Find(object o, string name) {
        foreach (System.Reflection.PropertyInfo pi in o.GetType().GetProperties())
            if (string.Equals(pi.Name, name, StringComparison.OrdinalIgnoreCase) && pi.CanWrite) return pi;
        return null;
    }
    public static RngAttribute RangeOf(System.Reflection.PropertyInfo pi) {
        object[] a = pi.GetCustomAttributes(typeof(RngAttribute), true);
        return a.Length > 0 ? (RngAttribute)a[0] : null;
    }
    public static bool Set(object o, string name, string value) {
        System.Reflection.PropertyInfo pi = Find(o, name);
        if (pi == null || value == null) return false;
        try {
            if (pi.PropertyType.IsEnum) {
                string v = value.Trim().ToLowerInvariant();
                string hit = null;
                foreach (string en in Enum.GetNames(pi.PropertyType)) if (en.ToLowerInvariant() == v) { hit = en; break; }
                if (hit == null) {
                    string[] names = Enum.GetNames(pi.PropertyType); Array.Sort(names, (a, b) => b.Length.CompareTo(a.Length));
                    foreach (string en in names) {
                        if (v.StartsWith(en.ToLowerInvariant()) || (en == "lowpass" && v == "lp") || (en == "highpass" && v == "hp") || (en == "bandpass" && v == "bp") || (en == "lowpass24" && (v == "lp24" || v == "24db")) || (en == "tri" && v.StartsWith("tri")) || (en == "square" && v == "sqr") || (en == "sh" && v.StartsWith("sample")) || (en == "pluck" && (v == "string" || v == "karplus")) || (en == "vowel" && (v == "formant" || v == "voice")) || (en == "additive" && (v == "organ" || v == "harmonic")) || (en == "bell" && (v == "modal" || v == "metal"))) { hit = en; break; }
                    }
                }
                if (hit == null) return false;
                pi.SetValue(o, Enum.Parse(pi.PropertyType, hit), null); return true;
            }
            if (pi.PropertyType == typeof(string)) { pi.SetValue(o, value.Trim(), null); return true; }
            double d;
            if (pi.PropertyType == typeof(bool)) { string bv = value.Trim().ToLowerInvariant(); pi.SetValue(o, bv == "true" || bv == "1" || bv == "yes" || bv == "on", null); return true; }
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out d) || double.IsNaN(d) || double.IsInfinity(d)) return false;
            RngAttribute r = RangeOf(pi);
            if (r != null) d = Math.Max(r.Min, Math.Min(r.Max, d));
            pi.SetValue(o, d, null);
            return true;
        } catch { return false; }
    }
    public static string Describe(Type t) {
        object def = Activator.CreateInstance(t);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (System.Reflection.PropertyInfo pi in t.GetProperties()) {
            if (!pi.CanWrite) continue;
            object dv = pi.GetValue(def, null);
            string desc = "";
            object[] da = pi.GetCustomAttributes(typeof(DescriptionAttribute), true);
            if (da.Length > 0) desc = ((DescriptionAttribute)da[0]).Description;
            if (pi.PropertyType == typeof(string)) sb.AppendLine("- " + pi.Name + ": text (default \"" + dv + "\"). " + desc);
            else if (pi.PropertyType == typeof(bool)) sb.AppendLine("- " + pi.Name + ": true or false (default " + ((bool)dv ? "true" : "false") + "). " + desc);
            else if (pi.PropertyType.IsEnum) sb.AppendLine("- " + pi.Name + ": one of " + string.Join("|", Enum.GetNames(pi.PropertyType)) + " (default " + dv + "). " + desc);
            else { RngAttribute r = RangeOf(pi); sb.AppendLine("- " + pi.Name + ": number " + (r == null ? "" : r.Min.ToString(CultureInfo.InvariantCulture) + " to " + r.Max.ToString(CultureInfo.InvariantCulture)) + " (default " + Convert.ToString(dv, CultureInfo.InvariantCulture) + "). " + desc); }
        }
        return sb.ToString();
    }
    // amount 0..1: how far each parameter moves toward a fresh random value (1 = fully random).
    public static void Randomize(object o, double amount, int seed) {
        Random rnd = new Random(seed);
        foreach (System.Reflection.PropertyInfo pi in o.GetType().GetProperties()) {
            if (!pi.CanWrite) continue;
            if (pi.PropertyType.IsEnum) {
                if (rnd.NextDouble() < amount * 0.6) { Array vals = Enum.GetValues(pi.PropertyType); pi.SetValue(o, vals.GetValue(rnd.Next(vals.Length)), null); }
                continue;
            }
            RngAttribute r = RangeOf(pi);
            if (r == null || r.NoRandom) continue;
            double cur = Convert.ToDouble(pi.GetValue(o, null), CultureInfo.InvariantCulture), nv;
            if (r.Min > 0 && r.Max / r.Min > 20) {
                double lc = Math.Log(Math.Max(cur, r.Min)), lr = Math.Log(r.Min) + rnd.NextDouble() * (Math.Log(r.Max) - Math.Log(r.Min));
                nv = Math.Exp(lc + (lr - lc) * amount);
            } else {
                double rv = r.Min + rnd.NextDouble() * (r.Max - r.Min);
                // keep random values sparse for level-like parameters so patches do not turn into mud
                if (r.Min == 0 && r.Max <= 1.5 && rnd.NextDouble() < 0.45) rv = r.Min;
                nv = cur + (rv - cur) * amount;
            }
            pi.SetValue(o, Math.Max(r.Min, Math.Min(r.Max, nv)), null);
        }
    }
}
