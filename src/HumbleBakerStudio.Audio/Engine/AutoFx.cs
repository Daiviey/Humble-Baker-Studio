namespace HumbleBakerStudio.Audio;


// Applies a track's automation lanes (volume, pan, sweeping low-pass and high-pass filters) to its rendered audio.
public static class AutoFx {
    const int SR = Dsp.SR;
    class Svf {
        double ic1, ic2;
        public double Tick(double x, double a1, double a2, double a3, double k, bool hp) {
            double v3 = x - ic2, v1 = a1 * ic1 + a2 * v3, v2 = ic2 + a2 * ic1 + a3 * v3;
            ic1 = 2 * v1 - ic1; ic2 = 2 * v2 - ic2;
            return hp ? x - k * v1 - v2 : v2;
        }
    }
    public static void Apply(Track t, float[] L, float[] R, double spb) { ApplyLanes(t.Auto, t.Fx, L, R, spb); }
    public static void ApplyLanes(List<AutoLane> lanes, Fx fx, float[] L, float[] R, double spb) {
        if (lanes == null) return;
        int n = L.Length; double sampPerBeat = spb * SR;
        foreach (AutoLane lane in lanes) {
            if (lane.Points.Count == 0) continue;
            string p = lane.Param;
            if (p == "lp" || p == "hp") {
                // Mixer low-pass / high-pass (post-FX) automation.
                double zl = 0, zr = 0;
                for (int i0 = 0; i0 < n; i0 += 32) {
                    double fc = Math.Max(20, Math.Min(20000, lane.At(i0 / sampPerBeat)));
                    double ea = Math.Exp(-2 * Math.PI * fc / SR); int end = Math.Min(n, i0 + 32);
                    for (int i = i0; i < end; i++) {
                        zl = (1 - ea) * L[i] + ea * zl; zr = (1 - ea) * R[i] + ea * zr;
                        if (p == "lp") { L[i] = (float)zl; R[i] = (float)zr; }
                        else { L[i] = (float)(L[i] - zl); R[i] = (float)(R[i] - zr); }
                    }
                }
            } else if (p == "vol" || p == "pan") {
                for (int i0 = 0; i0 < n; i0 += 32) {
                    double v = lane.At(i0 / sampPerBeat), gl, gr; int end = Math.Min(n, i0 + 32);
                    if (p == "vol") { gl = gr = Math.Max(0, v); }
                    else { double pn = Math.Max(-1, Math.Min(1, v)); gl = Math.Min(1, 1 - pn); gr = Math.Min(1, 1 + pn); }
                    if (gl == 1 && gr == 1) continue;
                    for (int i = i0; i < end; i++) { L[i] *= (float)gl; R[i] *= (float)gr; }
                }
            } else if (p.StartsWith("f:")) {
                string fpn = p.Substring(2);
                if (fpn == "Cutoff") {
                    Svf[] slf = { new Svf(), new Svf() }, srf = { new Svf(), new Svf() };
                    AutoLane resLane = null;
                    foreach (AutoLane ra in lanes) if (ra.Param == "f:Resonance") { resLane = ra; break; }
                    for (int i0 = 0; i0 < n; i0 += 32) {
                        double fc = lane.At(i0 / sampPerBeat); int end = Math.Min(n, i0 + 32);
                        if (fc >= 19000) continue;
                        fc = Math.Max(20, Math.Min(0.45 * SR, fc));
                        double rq = resLane != null ? Math.Max(0, Math.Min(0.95, resLane.At(i0 / sampPerBeat))) : 0;
                        double g = Math.Tan(Math.PI * fc / SR), k = Math.Max(0.15, 1.0 - rq), a1 = 1 / (1 + g * (g + k)), a2 = g * a1, a3 = g * a2;
                        for (int i = i0; i < end; i++) {
                            double x = L[i], y = R[i];
                            x = slf[1].Tick(slf[0].Tick(x, a1, a2, a3, k, false), a1, a2, a3, k, false); y = srf[1].Tick(srf[0].Tick(y, a1, a2, a3, k, false), a1, a2, a3, k, false);
                            L[i] = (float)Math.Max(-2, Math.Min(2, x)); R[i] = (float)Math.Max(-2, Math.Min(2, y));
                        }
                    }
                } else if (fpn == "Resonance") {
                    // Standalone resonance: only when no Cutoff lane exists (Cutoff already applies resonance as a modifier).
                    if (!lanes.Exists(delegate (AutoLane x) { return x.Param == "f:Cutoff"; })) {
                        Svf[] slf = { new Svf(), new Svf() }, srf = { new Svf(), new Svf() };
                        double fc0 = fx != null ? Math.Max(20, Math.Min(0.45 * SR, fx.Cutoff)) : 12000;
                        for (int i0 = 0; i0 < n; i0 += 32) {
                            double rq = Math.Max(0, Math.Min(0.95, lane.At(i0 / sampPerBeat))); int end = Math.Min(n, i0 + 32);
                            double g = Math.Tan(Math.PI * fc0 / SR), k = Math.Max(0.15, 1.0 - rq), a1 = 1 / (1 + g * (g + k)), a2 = g * a1, a3 = g * a2;
                            for (int i = i0; i < end; i++) {
                                double x = L[i], y = R[i];
                                x = slf[1].Tick(slf[0].Tick(x, a1, a2, a3, k, false), a1, a2, a3, k, false); y = srf[1].Tick(srf[0].Tick(y, a1, a2, a3, k, false), a1, a2, a3, k, false);
                                L[i] = (float)Math.Max(-2, Math.Min(2, x)); R[i] = (float)Math.Max(-2, Math.Min(2, y));
                            }
                        }
                    }
                } else if (fpn == "Drive" || fpn == "Fold") {
                    for (int i0 = 0; i0 < n; i0 += 32) {
                        double v = Math.Max(0, Math.Min(1, lane.At(i0 / sampPerBeat))); int end = Math.Min(n, i0 + 32);
                        double g = 1 + v * 12;
                        for (int i = i0; i < end; i++) { if (fpn == "Drive") { L[i] = (float)(Math.Tanh(L[i] * g) * 0.5); R[i] = (float)(Math.Tanh(R[i] * g) * 0.5); } else { L[i] = (float)(L[i] * (1 - v) + v * Math.Sin(L[i] * (1 + v * 8) * 1.5707963)); R[i] = (float)(R[i] * (1 - v) + v * Math.Sin(R[i] * (1 + v * 8) * 1.5707963)); } }
                    }
                } else if (fpn == "Bits") {
                    float ha = 0, hb = 0; int ds = 1;
                    for (int i0 = 0; i0 < n; i0 += 32) {
                        double v = Math.Max(2, Math.Min(16, lane.At(i0 / sampPerBeat))); double steps = Math.Pow(2, v - 1); int end = Math.Min(n, i0 + 32);
                        for (int i = i0; i < end; i++) { if (i % ds == 0) { ha = L[i]; hb = R[i]; } L[i] = (float)(Math.Round(ha * steps) / steps); R[i] = (float)(Math.Round(hb * steps) / steps); }
                    }
                } else if (fpn == "RingMix" || fpn == "RingFreq") {
                    for (int i0 = 0; i0 < n; i0 += 32) {
                        double m = Math.Max(0, Math.Min(1, lane.At(i0 / sampPerBeat))); double fr = fx != null && fx.RingFreq > 0 ? fx.RingFreq : 200; int end = Math.Min(n, i0 + 32);
                        double w = 2 * Math.PI * Math.Max(20, Math.Min(5000, fr)) / SR;
                        for (int i = i0; i < end; i++) { double s = Math.Sin(w * i); L[i] = (float)(L[i] * (1 - m) + L[i] * s * m); R[i] = (float)(R[i] * (1 - m) + R[i] * s * m); }
                    }
                } else if (fpn == "DelayMix") {
                    int d = (int)Math.Round((fx != null && fx.DelayBeats > 0 ? fx.DelayBeats : 0.75) * spb * SR); if (d < 1) d = 1; if (d < n) {
                        float[] wl = new float[n], wr = new float[n]; double fb = 0.4;
                        for (int pass = 0; pass < 2; pass++) { for (int i = 0; i < n; i++) { int j = i - d; if (j < 0) continue; wr[i] = L[j] + (float)fb * wl[j]; wl[i] = R[j] + (float)fb * wr[j]; } }
                        for (int i0 = 0; i0 < n; i0 += 32) { double m = Math.Max(0, Math.Min(1, lane.At(i0 / sampPerBeat))); int end = Math.Min(n, i0 + 32); for (int i = i0; i < end; i++) { L[i] += (float)m * wl[i]; R[i] += (float)m * wr[i]; } }
                    }
                } else if (fpn == "ReverbMix") {
                    int dl = 1557; float[] delayBufL = new float[dl], delayBufR = new float[dl]; int pos = 0; double lpL = 0, lpR = 0;
                    for (int i = 0; i < n; i++) {
                        double m = Math.Max(0, Math.Min(1, lane.At(i / sampPerBeat)));
                        double xL = L[i], xR = R[i];
                        double yL = delayBufL[pos], yR = delayBufR[pos];
                        lpL += 0.25 * (xL - lpL); lpR += 0.25 * (xR - lpR);
                        delayBufL[pos] = (float)(xL + yL * 0.7); delayBufR[pos] = (float)(xR + yR * 0.7); pos = (pos + 1) % dl;
                        L[i] = (float)(xL * (1 - m) + yL * m); R[i] = (float)(xR * (1 - m) + yR * m);
                    }
                } else if (fpn == "Chorus") {
                    double baseD = 0.005 * SR; double depth = 0.003 * SR;
                    for (int i0 = 0; i0 < n; i0 += 32) {
                        double m = Math.Max(0, Math.Min(1, lane.At(i0 / sampPerBeat))); int end = Math.Min(n, i0 + 32);
                        for (int i = i0; i < end; i++) { int di = (int)(baseD + depth * Math.Sin(2 * Math.PI * 0.7 * i / SR)); if (di < 1) di = 1; if (di < i) { L[i] = (float)(L[i] * (1 - m) + L[i - di] * m); R[i] = (float)(R[i] * (1 - m) + R[i - di] * m); } }
                    }
                } else if (fpn == "TapeStop") {
                    // Variable playback speed: 1 = full stop, 0 = normal. The read head freezes while stopped.
                    float[] srcL = (float[])L.Clone(), srcR = (float[])R.Clone();
                    double pos = 0;
                    for (int i = 0; i < n; i++) {
                        double amt = Math.Max(0, Math.Min(1, lane.At(i / sampPerBeat)));
                        if (amt < 1e-4) { pos = i; L[i] = srcL[i]; R[i] = srcR[i]; continue; }
                        int j = (int)pos; double fr = pos - j;
                        if (j >= 0 && j + 1 < n) { L[i] = (float)(srcL[j] * (1 - fr) + srcL[j + 1] * fr); R[i] = (float)(srcR[j] * (1 - fr) + srcR[j + 1] * fr); }
                        pos += (1 - amt);
                        if (pos > n - 2) pos = n - 2;
                    }
                } else if (fpn == "Width") {
                    for (int i0 = 0; i0 < n; i0 += 32) {
                        double w = Math.Max(0, Math.Min(1.6, lane.At(i0 / sampPerBeat))); int end = Math.Min(n, i0 + 32);
                        for (int i = i0; i < end; i++) { double mid = (L[i] + R[i]) * 0.5, side = (L[i] - R[i]) * 0.5 * w; L[i] = (float)(mid + side); R[i] = (float)(mid - side); }
                    }
                } else if (fpn == "CompThresh") {
                    double ratio = fx != null && fx.CompRatio > 1 ? fx.CompRatio : 4;
                    double atk = fx != null ? Math.Max(0.1, fx.CompAttack) : 10;
                    double rel = fx != null ? Math.Max(10, fx.CompRelease) : 100;
                    double makeup = fx != null ? fx.CompMakeup : 0;
                    double ea = 1 - Math.Exp(-1.0 / (atk * 0.001 * SR)), er = 1 - Math.Exp(-1.0 / (rel * 0.001 * SR));
                    double env = 0;
                    for (int i0 = 0; i0 < n; i0 += 32) {
                        double thr = lane.At(i0 / sampPerBeat); int end = Math.Min(n, i0 + 32);
                        for (int i = i0; i < end; i++) {
                            double x = Math.Max(Math.Abs(L[i]), Math.Abs(R[i]));
                            env += (x > env ? ea : er) * (x - env);
                            double db = 20 * Math.Log10(Math.Max(env, 1e-9));
                            double over = Math.Max(0, db - thr);
                            double g = Math.Pow(10, (-over * (1 - 1 / ratio) + makeup) / 20);
                            L[i] = (float)(L[i] * g); R[i] = (float)(R[i] * g);
                        }
                    }
                }
            }
        }
    }
}
