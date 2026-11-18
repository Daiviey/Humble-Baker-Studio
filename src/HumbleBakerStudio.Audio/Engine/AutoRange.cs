namespace HumbleBakerStudio.Audio;


// Value range + scaling for one automatable parameter. Shared by the lane editor and the spectral
// automation painter so every lane draws and maps the same way. Params use the same keys as AutoLane:
// vol / pan / lp / hp, "p:Name" (synth patch) and "f:Name" (track FX). log=true means the value maps
// logarithmically across the lane height (frequencies).
public static class AutoRange {
    public static void Info(string param, out double min, out double max, out bool log) {
        log = false; min = 0; max = 1;
        switch (param) {
            case "vol": min = 0; max = 2; return;
            case "pan": min = -1; max = 1; return;
            case "lp": case "hp": min = 20; max = 20000; log = true; return;
            case "p:Cutoff": case "f:Cutoff": min = 30; max = 12000; log = true; return;
            case "p:Resonance": min = 0; max = 1; return;
            case "p:FilterEnv": min = -1; max = 1; return;
            case "p:Bits": case "f:Bits": min = 2; max = 16; return;
            case "p:FmAmount": min = 0; max = 10; return;
            case "p:Unison": min = 1; max = 7; return;
            case "p:LfoRate": min = 0; max = 40; return;
            case "p:LfoPitch": min = 0; max = 1200; return;
            case "f:Resonance": min = 0; max = 0.95; return;
            case "f:ChorusRate": min = 0.05; max = 10; return;
            case "f:RingFreq": min = 20; max = 5000; log = true; return;
            case "f:DelayBeats": min = 0.0625; max = 8; return;
            case "f:DelayFeedback": case "f:FlangerFb": case "f:TapeDelayFb": min = 0; max = 0.95; return;
            case "f:PhaserRate": min = 0.01; max = 8; return;
            case "f:FlangerRate": min = 0.02; max = 10; return;
            case "f:TapeStopRate": min = 0.05; max = 3; return;
            case "f:TapeDelayTime": min = 10; max = 2000; log = true; return;
            case "f:CompThresh": min = -40; max = 0; return;
            case "f:CompRatio": min = 1; max = 12; return;
            case "f:CompAttack": min = 0.1; max = 100; log = true; return;
            case "f:CompRelease": case "f:SidechainRelease": min = 10; max = 1000; log = true; return;
            case "f:CompMakeup": min = -12; max = 24; return;
            case "f:SidechainAttack": min = 0.5; max = 100; return;
            case "f:LowShelf": case "f:HighShelf": case "f:MidPeak": case "f:PitchShift": min = -12; max = 12; return;
            case "f:Width": min = 0; max = 1.6; return;
        }
        // Unknown p:/f: fall back to a safe 0..1 range.
        if (param.StartsWith("p:") || param.StartsWith("f:")) { min = 0; max = 1; return; }
    }

    public static double Neutral(string param) {
        switch (param) {
            case "vol": return 1.0; case "pan": return 0.0; case "lp": return 20000; case "hp": return 20;
            case "p:Cutoff": case "f:Cutoff": return 800;
            case "f:CompThresh": return -18; case "f:CompRatio": return 1; case "f:CompMakeup": return 0;
            case "p:Unison": return 1; case "p:Bits": case "f:Bits": return 16;
        }
        if (param.StartsWith("p:")) return double.NaN;
        double mn, mx; bool lg; Info(param, out mn, out mx, out lg);
        if (lg) return Math.Exp((Math.Log(Math.Max(mn, 1.0)) + Math.Log(Math.Max(mx, 1.0))) / 2.0);
        return (mn + mx) / 2.0;
    }

    public static string Label(string param) {
        if (param.StartsWith("p:")) return param.Substring(2);
        if (param.StartsWith("f:")) return param.Substring(2);
        switch (param) { case "vol": return "Volume"; case "pan": return "Pan"; case "lp": return "Low-pass"; case "hp": return "High-pass"; }
        return param;
    }

    // PowerShell-friendly helpers (no out/ref parameters).
    public static double Map(string param, double frac) { double mn, mx; bool lg; Info(param, out mn, out mx, out lg); frac = Math.Max(0, Math.Min(1, frac)); return lg ? mn * Math.Pow(mx / mn, frac) : mn + frac * (mx - mn); }
    public static double Clamp(string param, double v) { double mn, mx; bool lg; Info(param, out mn, out mx, out lg); return Math.Max(mn, Math.Min(mx, v)); }
    public static double MinOf(string param) { double mn, mx; bool lg; Info(param, out mn, out mx, out lg); return mn; }
    public static double MaxOf(string param) { double mn, mx; bool lg; Info(param, out mn, out mx, out lg); return mx; }
}
