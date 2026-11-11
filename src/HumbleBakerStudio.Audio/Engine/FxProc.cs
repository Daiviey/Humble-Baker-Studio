namespace HumbleBakerStudio.Audio;


public static class FxProc {
    const int SR = Dsp.SR;
    // In loop mode the buffers are processed twice end to end and the second pass is kept, so delay and reverb tails wrap seamlessly.
    public static void Process(Fx fx, float[] L, float[] R, bool loop, double spb) {
        int N = L.Length;
        if (!loop) { Run(fx, L, R, spb); return; }
        float[] a = new float[N * 2], b = new float[N * 2];
        Array.Copy(L, 0, a, 0, N); Array.Copy(L, 0, a, N, N); Array.Copy(R, 0, b, 0, N); Array.Copy(R, 0, b, N, N);
        Run(fx, a, b, spb);
        Array.Copy(a, N, L, 0, N); Array.Copy(b, N, R, 0, N);
    }

    static void Run(Fx fx, float[] a, float[] b, double spb) {
        int n = a.Length;
        if (fx.GateAmount > 0) GateFx(fx, a, b);
        if (fx.Saturator > 0) SaturatorFx(fx, a, b);
        if (fx.Exciter > 0) ExciterFx(fx, a, b);
        if (fx.RectifyMix > 0) RectifyFx(fx, a, b);
        if (Math.Abs(fx.TransientAttack) > 0.001 || Math.Abs(fx.TransientSustain) > 0.001) TransientFx(fx, a, b);
        if (fx.TremoloMix > 0) TremoloFx(fx, a, b);
        if (fx.AutoPanMix > 0) AutoPanFx(fx, a, b);
        if (fx.VibratoMix > 0) VibratoFx(fx, a, b);
        if (fx.AutoWahMix > 0) AutoWahFx(fx, a, b);
        if (fx.CombMix > 0) CombFx(fx, a, b);
        if (fx.SpringMix > 0) SpringFx(fx, a, b);
        if (fx.ShimmerMix > 0) ShimmerFx(fx, a, b);
        if (fx.HaasMix > 0) HaasFx(fx, a, b);
        if (fx.EnsembleMix > 0) EnsembleFx(fx, a, b);
        if (fx.RotaryMix > 0) RotaryFx(fx, a, b);
        if (fx.OctaverMix > 0) OctaverFx(fx, a, b);
        if (fx.HarmonizerMix > 0) HarmonizerFx(fx, a, b);
        if (fx.ResonatorMix > 0) ResonatorFx(fx, a, b);
        if (fx.TiltGain != 0) TiltEqFx(fx, a, b);
        if (fx.MidGain != 0 || fx.SideGain != 0) MidSideEqFx(fx, a, b);
        if (fx.DeEsserAmount > 0) DeEsserFx(fx, a, b);
        if (fx.MBOn) MultibandCompFx(fx, a, b);
        if (fx.ClipperAmount > 0) ClipperFx(fx, a, b);
        if (fx.AutoFilterMix > 0) AutoFilterFx(fx, a, b);
        if (fx.GateVerbMix > 0) GateVerbFx(fx, a, b);
        if (fx.ReverseVerbMix > 0) ReverseVerbFx(fx, a, b);
        if (fx.MultiTapMix > 0) MultiTapFx(fx, a, b);
        if (fx.DoublerMix > 0) DoublerFx(fx, a, b);
        if (fx.FormantMix > 0) FormantFx(fx, a, b);
        if (fx.LadderMix > 0) LadderFx(fx, a, b);
        if (fx.SubBassMix > 0) SubBassFx(fx, a, b);
        if (fx.BassEnhMix > 0) BassEnhFx(fx, a, b);
        if (fx.AirMix > 0) AirBandFx(fx, a, b);
        if (fx.TubeMix > 0) TubeFx(fx, a, b);
        if (fx.TapeSatMix > 0) TapeSatFx(fx, a, b);
        if (fx.DiodeMix > 0) DiodeFx(fx, a, b);
        if (fx.FuzzAmount > 0) FuzzFx(fx, a, b);
        if (fx.ChipMix > 0) ChipTuneFx(fx, a, b);
        if (fx.FreqShiftMix > 0) FreqShiftFx(fx, a, b);
        if (fx.BeatRepeatMix > 0) BeatRepeatFx(fx, a, b);
        if (fx.GrainMix > 0) GrainFx(fx, a, b);
        if (fx.PingPongMix > 0) PingPongFx(fx, a, b);
        if (fx.DubMix > 0) DubDelayFx(fx, a, b);
        if (fx.CathedralMix > 0) VerbCore(fx, a, b, fx.CathedralMix, fx.CathedralSize, fx.CathedralDamp, 3.0);
        if (fx.RoomMix > 0) VerbCore(fx, a, b, fx.RoomMix, fx.RoomSize, fx.RoomDamp, 1.0);
        if (fx.AmbienceMix > 0) VerbCore(fx, a, b, fx.AmbienceMix, fx.AmbienceSize, fx.AmbienceDamp, 0.55);
        if (fx.WahPedalMix > 0) WahPedalFx(fx, a, b);
        if (fx.TalkMix > 0) TalkBoxFx(fx, a, b);
        if (fx.DuckerMix > 0) DuckerFx(fx, a, b);
        if (fx.RiderAmount > 0) RiderFx(fx, a, b);
        if (Math.Abs(fx.StereoWidth - 1) > 0.001 || fx.BassMonoFreq > 0) StereoImageFx(fx, a, b);
        if (fx.HarmTremMix > 0) HarmTremFx(fx, a, b);
        if (fx.GlitchMix > 0) GlitchGateFx(fx, a, b);
        if (fx.SweepMix > 0) SweepFx(fx, a, b);
        if (fx.OptoMix > 0) OptoCompFx(fx, a, b);
        if (fx.FetMix > 0) FetCompFx(fx, a, b);
        if (fx.VcaMix > 0) VcaCompFx(fx, a, b);
        if (fx.ExpMix > 0) ExpanderFx(fx, a, b);
        if (fx.MSCompMix > 0) MSCompFx(fx, a, b);
        if (fx.DynEqMix > 0) DynEqFx(fx, a, b);
        if (fx.PultecLow > 0 || fx.PultecHigh > 0 || fx.PultecAtten > 0) PultecFx(fx, a, b);
        if (fx.OverdriveMix > 0) OverdriveFx(fx, a, b);
        if (fx.MetalMix > 0) MetalDriveFx(fx, a, b);
        if (fx.CabMix > 0) CabSimFx(fx, a, b);
        if (fx.MBDistMix > 0) MultibandDriveFx(fx, a, b);
        if (fx.FlutterMix > 0) TapeFlutterFx(fx, a, b);
        if (fx.DimMix > 0) DimensionFx(fx, a, b);
        if (fx.EnvPhaserMix > 0) EnvPhaserFx(fx, a, b);
        if (fx.EnvFlangeMix > 0) EnvFlangerFx(fx, a, b);
        if (fx.ChopMix > 0) ChopperFx(fx, a, b);
        if (fx.BarberMix > 0) BarberPoleFx(fx, a, b);
        if (fx.HalfSpeedMix > 0) HalfSpeedFx(fx, a, b);
        if (fx.PitchEchoMix > 0) PitchEchoFx(fx, a, b);
        if (fx.FreezeMix > 0) SpectralFreezeFx(fx, a, b);
        if (fx.RevDelayMix > 0) ReverseDelayFx(fx, a, b);
        if (fx.EchoVerbMix > 0) EchoVerbFx(fx, a, b);
        if (fx.BloomMix > 0) BloomVerbFx(fx, a, b);
        if (fx.NonLinVerbMix > 0) NonLinVerbFx(fx, a, b);
        if (fx.VocoderMix > 0) VocoderFx(fx, a, b);
        if (fx.SpecGateMix > 0) SpectralGateFx(fx, a, b);
        if (fx.SpecBlurMix > 0) SpectralBlurFx(fx, a, b);
        if (fx.SpecDelayMix > 0) SpectralDelayFx(fx, a, b);
        if (fx.ConvMix > 0) ConvReverbFx(fx, a, b);
        if (fx.TankMix > 0) TankReverbFx(fx, a, b);
        if (fx.TuneMix > 0) AutoTuneFx(fx, a, b);
        if (fx.OctUpMix > 0) OctaveUpFx(fx, a, b);
        if (fx.ChordMix > 0) ChordGenFx(fx, a, b);
        if (fx.UpCompMix > 0) UpwardCompFx(fx, a, b);
        if (fx.TranGateMix > 0) TransientGateFx(fx, a, b);
        if (fx.ShuffleMix > 0) StereoShuffleFx(fx, a, b);
        if (fx.MonoMix > 0) MonoMakerFx(fx, a, b);
        if (fx.PhaseMix > 0) PhaseAlignFx(fx, a, b);
        if (fx.LoopMix > 0) TapeLoopFx(fx, a, b);
        if (fx.StutterGateMix > 0) StutterGateFx(fx, a, b);
        if (fx.PitchShift != 0) PitchShift(fx, a, b);
        if (fx.TapeWow > 0) Wow(fx, a, b);
        if (fx.TapeStop > 0) TapeStopFx(fx, a, b, spb);
        if (fx.StutterBeats > 0.001 && fx.StutterChance > 0.001) Stutter(fx, a, b, spb);
        if (fx.Filter != FilterMode.off) { Svf(a, fx); Svf(b, fx); }
        if (fx.Fold > 0) {
            double f = fx.Fold;
            for (int i = 0; i < n; i++) {
                a[i] = (float)(a[i] * (1 - f) + f * Math.Sin(a[i] * (1 + f * 8) * 1.5707963));
                b[i] = (float)(b[i] * (1 - f) + f * Math.Sin(b[i] * (1 + f * 8) * 1.5707963));
            }
        }
        if (fx.Drive > 0) {
            double g = 1 + fx.Drive * 12;
            for (int i = 0; i < n; i++) { a[i] = (float)(Math.Tanh(a[i] * g) * 0.5); b[i] = (float)(Math.Tanh(b[i] * g) * 0.5); }
        }
        if (fx.Bits < 16 || fx.Downsample > 1) {
            double steps = Math.Pow(2, Math.Max(2, fx.Bits) - 1); int ds = Math.Max(1, (int)Math.Round(fx.Downsample));
            float ha = 0, hb = 0;
            for (int i = 0; i < n; i++) {
                if (i % ds == 0) { ha = a[i]; hb = b[i]; }
                a[i] = (float)(fx.Bits < 16 ? Math.Round(ha * steps) / steps : ha); b[i] = (float)(fx.Bits < 16 ? Math.Round(hb * steps) / steps : hb);
            }
        }
        if (fx.RingMix > 0 && fx.RingFreq > 0) {
            double m = fx.RingMix, w = 2 * Math.PI * fx.RingFreq / SR;
            for (int i = 0; i < n; i++) { double s = Math.Sin(w * i); a[i] = (float)(a[i] * (1 - m) + a[i] * s * m); b[i] = (float)(b[i] * (1 - m) + b[i] * s * m); }
        }
        if (fx.Chorus > 0) Chorus(fx, a, b);
        if (fx.FlangerMix > 0) Flanger(fx, a, b);
        if (fx.PhaserMix > 0) Phaser(fx, a, b);
        if (fx.DelayMix > 0) Delay(fx, a, b, spb);
        if (fx.TapeDelayMix > 0) TapeDelay(fx, a, b, spb);
        if (fx.ReverbMix > 0) Reverb(fx, a, b);
        if (fx.PlateMix > 0) Plate(fx, a, b);
        if (fx.VinylCrackle > 0 || fx.TapeHiss > 0) VinylTape(fx, a, b);
        if (fx.CompRatio > 1) Comp(fx, a, b);
        if (fx.LowShelf != 0 || fx.HighShelf != 0 || fx.MidPeak != 0) EqFx(fx, a, b);
        if (Math.Abs(fx.Width - 1) > 0.001) WidthFx(fx, a, b);
        if (fx.LimiterOn) LimiterFx(fx, a, b);
        if (Math.Abs(fx.Gain - 1) > 0.001) { float g = (float)fx.Gain; for (int i = 0; i < n; i++) { a[i] *= g; b[i] *= g; } }
    }

    static void Stutter(Fx fx, float[] a, float[] b, double spb) {
        int blk = (int)(fx.StutterBeats * spb * SR), div = Math.Max(2, (int)Math.Round(fx.StutterDiv));
        int seg = blk / div; if (blk < 64 || seg < 32) return;
        Random r = new Random(99);
        for (int s = 0; s + blk <= a.Length; s += blk) {
            if (r.NextDouble() >= fx.StutterChance) continue;
            for (int i = seg; i < blk; i++) { a[s + i] = a[s + (i % seg)]; b[s + i] = b[s + (i % seg)]; }
        }
    }

    static void Phaser(Fx fx, float[] a, float[] b) {
        int n = a.Length, stages = Math.Max(1, (int)Math.Round(fx.PhaserStages)); double mix = fx.PhaserMix, w = 2 * Math.PI * fx.PhaserRate / SR;
        double[][] z1a = new double[12][], z1b = new double[12][];
        for (int s = 0; s < stages; s++) { z1a[s] = new double[1]; z1b[s] = new double[1]; }
        for (int i = 0; i < n; i++) {
            double lfo = Math.Sin(w * i) * fx.PhaserDepth;
            double x = a[i], y = b[i];
            for (int s = 0; s < stages; s++) {
                double fc = 500 + lfo * 2500 + s * 180; double t = Math.Tan(Math.PI * Math.Min(fc, 16000) / SR); double c = (t - 1) / (t + 1);
                double xa = x, xb = y;
                x = c * xa + z1a[s][0]; z1a[s][0] = xa - c * x;
                y = c * xb + z1b[s][0]; z1b[s][0] = xb - c * y;
            }
            a[i] = (float)(x * (1 - mix) + (a[i] + x) * mix * 0.5); b[i] = (float)(y * (1 - mix) + (b[i] + y) * mix * 0.5);
        }
    }
    static void Flanger(Fx fx, float[] a, float[] b) {
        float[] sa = (float[])a.Clone(), sb = (float[])b.Clone(); int n = a.Length; double mix = fx.FlangerMix, w = 2 * Math.PI * fx.FlangerRate / SR, fb = fx.FlangerFb;
        float[] dl = new float[(int)(SR * 0.02) + 10], dr = new float[(int)(SR * 0.02) + 10]; int pos = 0;
        for (int i = 0; i < n; i++) {
            double dt = (0.004 + 0.003 * fx.FlangerDepth * Math.Sin(w * i)) * SR; int di = Math.Max(1, (int)dt);
            double yl = dl[(pos - di + dl.Length) % dl.Length], yr = dr[(pos - di + dr.Length) % dr.Length];
            dl[pos] = (float)(sa[i] + yr * fb); dr[pos] = (float)(sb[i] + yl * fb); pos = (pos + 1) % dl.Length;
            a[i] = (float)(sa[i] * (1 - mix) + yl * mix); b[i] = (float)(sb[i] * (1 - mix) + yr * mix);
        }
    }
    static void Wow(Fx fx, float[] a, float[] b) {
        float[] sa = (float[])a.Clone(), sb = (float[])b.Clone(); int n = a.Length; double amt = fx.TapeWow * 0.02;
        for (int i = 0; i < n; i++) { double mod = Math.Sin(2 * Math.PI * 2.5 * i / SR) * amt; double src = i * (1 + mod); if (src < 0) continue; int j = (int)src; double fr = src - j; if (j + 1 < n) { a[i] = (float)(sa[j] * (1 - fr) + sa[j + 1] * fr); b[i] = (float)(sb[j] * (1 - fr) + sb[j + 1] * fr); } }
    }
    static void TapeStopFx(Fx fx, float[] a, float[] b, double spb) {
        int n = a.Length; double tstop = Math.Max(0.05, fx.TapeStopRate * 3.0); int stopN = (int)Math.Min(n, tstop * SR); double amt = fx.TapeStop;
        float[] oa = (float[])a.Clone(), ob = (float[])b.Clone();
        for (int i = stopN; i < n; i++) {
            double u = (i - stopN) / (double)Math.Max(1, n - stopN); double speed = 1 - amt * (1 - u * u); double src = i * speed + (n - stopN) * 0; if (src < 0) src = 0; if (src >= n - 1) continue; int j = (int)src; double fr = src - j; oa[i] = (float)(a[j] * (1 - fr) + a[j + 1] * fr); ob[i] = (float)(b[j] * (1 - fr) + b[j + 1] * fr);
        }
        Array.Copy(oa, a, n); Array.Copy(ob, b, n);
    }
    static void PitchShift(Fx fx, float[] a, float[] b) {
        double ratio = Math.Pow(2, fx.PitchShift / 12.0); if (Math.Abs(ratio - 1) < 0.001) return;
        float[] sa = (float[])a.Clone(), sb = (float[])b.Clone(); int n = a.Length;
        for (int i = 0; i < n; i++) { double src = i * ratio; if (src + 1 >= n) continue; int j = (int)src; double fr = src - j; a[i] = (float)(sa[j] * (1 - fr) + sa[j + 1] * fr); b[i] = (float)(sb[j] * (1 - fr) + sb[j + 1] * fr); }
    }
    static void TapeDelay(Fx fx, float[] a, float[] b, double spb) {
        int d = (int)Math.Round(fx.TapeDelayTime / 1000.0 * SR), n = a.Length; if (d < 1 || d >= n) return;
        float[] da = (float[])a.Clone(), db = (float[])b.Clone(); float fb = (float)fx.TapeDelayFb; float m = (float)fx.TapeDelayMix; double wow = 2 * Math.PI * 2.2 / SR;
        for (int i = d; i < n; i++) { double mod = fx.TapeDelayWow * 0.5 * Math.Sin(wow * i); int di = (int)Math.Round(d * (1 + mod)); if (di < 1) di = 1; if (di >= i) continue; a[i] = da[i] + m * db[i - di] + fb * a[i - di] * 0.5f; b[i] = db[i] + m * da[i - di] + fb * b[i - di] * 0.5f; }
    }
    static void Plate(Fx fx, float[] a, float[] b) {
        int n = a.Length; float[] mono = new float[n];
        for (int i = 0; i < n; i++) mono[i] = (a[i] + b[i]) * 0.5f * 0.25f;
        double fb = 0.6 + 0.35 * fx.PlateSize, damp = 0.1 + 0.5 * fx.PlateDamping;
        for (int ch = 0; ch < 2; ch++) {
            float[] wet = new float[n]; float[] dst = ch == 0 ? a : b;
            int[] combs = new int[] { 1557 + ch * 7, 1617 + ch * 13, 1491 + ch * 5, 1422 + ch * 11, 1277 + ch * 17, 1356 + ch * 19 };
            for (int c = 0; c < combs.Length; c++) {
                float[] buf = new float[combs[c]]; int idx = 0; double store = 0;
                for (int i = 0; i < n; i++) { float outv = buf[idx]; store = outv * (1 - damp) + store * damp; buf[idx] = (float)(mono[i] + store * fb); if (++idx >= buf.Length) idx = 0; wet[i] += outv; }
            }
            for (int k = 0; k < ApLen.Length; k++) { float[] buf = new float[ApLen[k] + ch * 11]; int idx = 0; for (int i = 0; i < n; i++) { float bo = buf[idx]; float w = wet[i]; wet[i] = -w + bo; buf[idx] = w + bo * 0.5f; if (++idx >= buf.Length) idx = 0; } }
            for (int i = 0; i < n; i++) dst[i] += (float)(wet[i] * fx.PlateMix);
        }
    }
    static void VinylTape(Fx fx, float[] a, float[] b) {
        Random r = new Random(1234);
        for (int i = 0; i < a.Length; i++) {
            double nz = r.NextDouble() * 2 - 1;
            if (fx.TapeHiss > 0) { a[i] += (float)(nz * fx.TapeHiss * 0.03); b[i] += (float)(nz * fx.TapeHiss * 0.03); }
            if (fx.VinylCrackle > 0 && r.NextDouble() < fx.VinylCrackle * 0.02) { float pop = (float)((r.NextDouble() * 2 - 1) * fx.VinylCrackle * 0.6); a[i] += pop; b[i] += pop; }
        }
    }
    static void EnsembleFx(Fx fx, float[] a, float[] b) {
        int len = (int)(SR * 0.03); float[] dl = new float[len], dr = new float[len]; int pos = 0;
        double rate = Math.Max(0.05, fx.EnsembleRate), depth = Math.Max(0, Math.Min(1, fx.EnsembleDepth)), mix = Math.Max(0, Math.Min(1, fx.EnsembleMix));
        for (int i = 0; i < a.Length; i++) {
            int d1 = (int)((0.006 + 0.006 * depth * (0.5 + 0.5 * Math.Sin(2 * Math.PI * rate * i / SR))) * SR);
            int d2 = (int)((0.009 + 0.008 * depth * (0.5 + 0.5 * Math.Sin(2 * Math.PI * rate * i / SR + 2.1))) * SR);
            int r1 = pos - d1; if (r1 < 0) r1 += len; int r2 = pos - d2; if (r2 < 0) r2 += len;
            double y1 = dl[r1], y2 = dr[r2]; dl[pos] = a[i]; dr[pos] = b[i]; pos++; if (pos >= len) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + (y1 + y2) * 0.5 * mix); b[i] = (float)(b[i] * (1 - mix) + (y2 + y1) * 0.5 * mix);
        }
    }
    static void RotaryFx(Fx fx, float[] a, float[] b) {
        double rate = Math.Max(0.1, fx.RotaryRate), depth = Math.Max(0, Math.Min(1, fx.RotaryDepth)), mix = Math.Max(0, Math.Min(1, fx.RotaryMix));
        for (int i = 0; i < a.Length; i++) {
            double lfo = Math.Sin(2 * Math.PI * rate * i / SR);
            double gl = 1 - depth * (0.5 + 0.5 * lfo), gr = 1 - depth * (0.5 - 0.5 * lfo);
            a[i] = (float)(a[i] * (1 - mix + mix * gl)); b[i] = (float)(b[i] * (1 - mix + mix * gr));
        }
    }
    static void OctaverFx(Fx fx, float[] a, float[] b) {
        int len = (int)(SR * 2); float[] dl = new float[len], dr = new float[len]; int w = 0; double rp = 0;
        double mix = Math.Max(0, Math.Min(1, fx.OctaverMix));
        for (int i = 0; i < a.Length; i++) {
            int ri = (int)rp; if (ri >= len) ri %= len; double fr = rp - Math.Floor(rp); int ri2 = (ri + 1) % len;
            double ya = dl[ri] * (1 - fr) + dl[ri2] * fr, yb = dr[ri] * (1 - fr) + dr[ri2] * fr;
            dl[w] = a[i]; dr[w] = b[i]; w++; if (w >= len) w = 0;
            rp += 0.5; if (rp >= len) rp -= len;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void HarmonizerFx(Fx fx, float[] a, float[] b) {
        int len = (int)(SR * 2); float[] dl = new float[len], dr = new float[len]; int w = 0; double rp = 0;
        double ratio = Math.Pow(2, fx.HarmonizerSemi / 12.0), mix = Math.Max(0, Math.Min(1, fx.HarmonizerMix));
        for (int i = 0; i < a.Length; i++) {
            int ri = (int)rp; if (ri >= len) ri %= len; double fr = rp - Math.Floor(rp); int ri2 = (ri + 1) % len;
            double ya = dl[ri] * (1 - fr) + dl[ri2] * fr, yb = dr[ri] * (1 - fr) + dr[ri2] * fr;
            dl[w] = a[i]; dr[w] = b[i]; w++; if (w >= len) w = 0;
            rp += ratio; while (rp >= len) rp -= len;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void ResonatorFx(Fx fx, float[] a, float[] b) {
        int len = Math.Max(8, (int)(SR / Math.Max(40, fx.ResonatorFreq)));
        float[] b1 = new float[len], b2 = new float[len * 2 + 1], b3 = new float[len * 3 + 2]; int p1 = 0, p2 = 0, p3 = 0;
        double fb = Math.Max(0, Math.Min(0.95, fx.ResonatorFeedback)), mix = Math.Max(0, Math.Min(1, fx.ResonatorMix));
        for (int i = 0; i < a.Length; i++) {
            double y1 = b1[p1], y2 = b2[p2], y3 = b3[p3];
            b1[p1] = (float)(a[i] + y1 * fb); b2[p2] = (float)(a[i] + y2 * fb); b3[p3] = (float)(a[i] + y3 * fb);
            p1++; if (p1 >= b1.Length) p1 = 0; p2++; if (p2 >= b2.Length) p2 = 0; p3++; if (p3 >= b3.Length) p3 = 0;
            double wet = (y1 + y2 * 0.6 + y3 * 0.35) / 2.0;
            a[i] = (float)(a[i] * (1 - mix) + wet * mix); b[i] = (float)(b[i] * (1 - mix) + wet * mix);
        }
    }
    static void TiltEqFx(Fx fx, float[] a, float[] b) {
        double gLow = Math.Pow(10, -fx.TiltGain / 20.0), gHigh = Math.Pow(10, fx.TiltGain / 20.0), lp = 0, rp = 0, coef = Math.Exp(-2 * Math.PI * 700 / SR);
        for (int i = 0; i < a.Length; i++) { lp = (1 - coef) * a[i] + coef * lp; rp = (1 - coef) * b[i] + coef * rp; a[i] = (float)(lp * gLow + (a[i] - lp) * gHigh); b[i] = (float)(rp * gLow + (b[i] - rp) * gHigh); }
    }
    static void MidSideEqFx(Fx fx, float[] a, float[] b) {
        double gm = Math.Pow(10, fx.MidGain / 20.0), gs = Math.Pow(10, fx.SideGain / 20.0);
        for (int i = 0; i < a.Length; i++) { double m = (a[i] + b[i]) * 0.5 * gm, s = (a[i] - b[i]) * 0.5 * gs; a[i] = (float)(m + s); b[i] = (float)(m - s); }
    }
    static void DeEsserFx(Fx fx, float[] a, float[] b) {
        double fc = Math.Max(2000, Math.Min(12000, fx.DeEsserFreq)), coef = Math.Exp(-2 * Math.PI * fc / SR), amt = Math.Max(0, Math.Min(1, fx.DeEsserAmount));
        double la = 0, lb = 0, env = 0, atk = 1 - Math.Exp(-1.0 / (0.001 * SR)), rel = 1 - Math.Exp(-1.0 / (0.08 * SR));
        for (int i = 0; i < a.Length; i++) {
            la = (1 - coef) * a[i] + coef * la; lb = (1 - coef) * b[i] + coef * lb;
            double ha = a[i] - la, hb = b[i] - lb, x = Math.Max(Math.Abs(ha), Math.Abs(hb)); env += (x > env ? atk : rel) * (x - env);
            double g = env > 0.12 ? 0.12 / env : 1; g = 1 - amt * (1 - g);
            a[i] = (float)(la + ha * g); b[i] = (float)(lb + hb * g);
        }
    }
    static void MultibandCompFx(Fx fx, float[] a, float[] b) {
        double c1 = Math.Exp(-2 * Math.PI * 200 / SR), c2 = Math.Exp(-2 * Math.PI * 2000 / SR);
        double l1a = 0, l1b = 0, l2a = 0, l2b = 0, e1 = 0, e2 = 0, e3 = 0;
        double makeup = Math.Pow(10, fx.MBMakeup / 20.0), ratio = Math.Max(1, fx.MBRatio), atk = 1 - Math.Exp(-1.0 / (0.01 * SR)), rel = 1 - Math.Exp(-1.0 / (0.12 * SR));
        for (int i = 0; i < a.Length; i++) {
            l1a = (1 - c1) * a[i] + c1 * l1a; l1b = (1 - c1) * b[i] + c1 * l1b;
            l2a = (1 - c2) * a[i] + c2 * l2a; l2b = (1 - c2) * b[i] + c2 * l2b;
            double lo = (l1a + l1b) * 0.5, mid = ((l2a + l2b) * 0.5) - lo, hi = ((a[i] + b[i]) * 0.5) - lo - mid;
            e1 += (Math.Abs(lo) > e1 ? atk : rel) * (Math.Abs(lo) - e1); e2 += (Math.Abs(mid) > e2 ? atk : rel) * (Math.Abs(mid) - e2); e3 += (Math.Abs(hi) > e3 ? atk : rel) * (Math.Abs(hi) - e3);
            double g1 = e1 > Math.Pow(10, fx.MBLowThresh / 20.0) ? Math.Pow(10, fx.MBLowThresh / 20.0) / e1 : 1;
            double g2 = e2 > Math.Pow(10, fx.MBMidThresh / 20.0) ? Math.Pow(10, fx.MBMidThresh / 20.0) / e2 : 1;
            double g3 = e3 > Math.Pow(10, fx.MBHighThresh / 20.0) ? Math.Pow(10, fx.MBHighThresh / 20.0) / e3 : 1;
            g1 = Math.Pow(g1, 1 - 1 / ratio); g2 = Math.Pow(g2, 1 - 1 / ratio); g3 = Math.Pow(g3, 1 - 1 / ratio);
            double y = (lo * g1 + mid * g2 + hi * g3) * makeup;
            a[i] = (float)y; b[i] = (float)y;
        }
    }
    static void ClipperFx(Fx fx, float[] a, float[] b) {
        double g = 1 + fx.ClipperDrive * 10, mix = Math.Max(0, Math.Min(1, fx.ClipperAmount));
        for (int i = 0; i < a.Length; i++) { double xa = Math.Tanh(a[i] * g), xb = Math.Tanh(b[i] * g); a[i] = (float)(a[i] * (1 - mix) + xa * mix); b[i] = (float)(b[i] * (1 - mix) + xb * mix); }
    }
    static void AutoFilterFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.AutoFilterMix)), sens = Math.Max(0, Math.Min(1, fx.AutoFilterSensitivity)), q = Math.Max(0.05, 1 - Math.Min(0.95, fx.AutoFilterResonance));
        double env = 0, loA = 0, bdA = 0, loB = 0, bdB = 0, atk = 1 - Math.Exp(-1.0 / (0.005 * SR)), rel = 1 - Math.Exp(-1.0 / (0.12 * SR));
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double fc = 150 + sens * 7000 * Math.Min(1, env * 3); fc = Math.Max(60, Math.Min(SR / 5.0, fc)); double ff = 2 * Math.Sin(Math.PI * fc / SR);
            double hiA = a[i] - loA - q * bdA; bdA += ff * hiA; loA += ff * bdA;
            double hiB = b[i] - loB - q * bdB; bdB += ff * hiB; loB += ff * bdB;
            a[i] = (float)(a[i] * (1 - mix) + loA * mix); b[i] = (float)(b[i] * (1 - mix) + loB * mix);
        }
    }
    static void GateVerbFx(Fx fx, float[] a, float[] b) {
        int len = (int)(SR * 0.06); float[] dl = new float[len], dr = new float[len]; int pos = 0; double mix = Math.Max(0, Math.Min(1, fx.GateVerbMix)), gate = 1;
        int hold = Math.Max(1, (int)(fx.GateVerbHold * SR)); double dec = Math.Exp(-1.0 / Math.Max(0.01, fx.GateVerbDecay) / SR);
        for (int i = 0; i < a.Length; i++) {
            double wetL = dl[pos], wetR = dr[pos]; dl[pos] = (float)(a[i] + wetR * 0.6); dr[pos] = (float)(b[i] + wetL * 0.6); pos++; if (pos >= len) pos = 0;
            if (i % hold == 0) gate = 1; else gate *= dec;
            a[i] = (float)(a[i] * (1 - mix) + wetL * mix * gate); b[i] = (float)(b[i] * (1 - mix) + wetR * mix * gate);
        }
    }
    static void ReverseVerbFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; float[] ra = new float[n], rb = new float[n]; for (int i = 0; i < n; i++) { ra[i] = a[n - 1 - i]; rb[i] = b[n - 1 - i]; }
        Fx rev = new Fx(); rev.SpringMix = 1; rev.SpringDecay = Math.Max(0.1, fx.ReverseVerbDecay); rev.SpringTone = 0.5; SpringFx(rev, ra, rb);
        double mix = Math.Max(0, Math.Min(1, fx.ReverseVerbMix));
        for (int i = 0; i < n; i++) { a[i] = (float)(a[i] * (1 - mix) + ra[n - 1 - i] * mix); b[i] = (float)(b[i] * (1 - mix) + rb[n - 1 - i] * mix); }
    }
    static void MultiTapFx(Fx fx, float[] a, float[] b) {
        int d = Math.Max(2, (int)(fx.MultiTapTime * 0.001 * SR)); float[] dl = new float[d], dr = new float[d]; int pos = 0; double fb = Math.Max(0, Math.Min(0.95, fx.MultiTapFb)), mix = Math.Max(0, Math.Min(1, fx.MultiTapMix));
        for (int i = 0; i < a.Length; i++) {
            int p2 = pos - d / 2; if (p2 < 0) p2 += d; int p3 = pos - d / 3; if (p3 < 0) p3 += d;
            double wl = dl[pos] * 0.6 + dl[p2] * 0.3 + dl[p3] * 0.15, wr = dr[pos] * 0.6 + dr[p2] * 0.3 + dr[p3] * 0.15;
            dl[pos] = (float)(a[i] + wl * fb); dr[pos] = (float)(b[i] + wr * fb); pos++; if (pos >= d) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + wl * mix); b[i] = (float)(b[i] * (1 - mix) + wr * mix);
        }
    }
    static void DoublerFx(Fx fx, float[] a, float[] b) {
        int d = Math.Max(1, (int)(fx.DoublerDelay * 0.001 * SR)); float[] dl = new float[d], dr = new float[d]; int pos = 0; double mix = Math.Max(0, Math.Min(1, fx.DoublerMix));
        for (int i = 0; i < a.Length; i++) { double yl = dl[pos], yr = dr[pos]; dl[pos] = a[i]; dr[pos] = b[i]; pos++; if (pos >= d) pos = 0; a[i] = (float)(a[i] * (1 - mix) + yl * mix); b[i] = (float)(b[i] * (1 - mix) + yr * mix); }
    }
    static void FormantFx(Fx fx, float[] a, float[] b) {
        double[] fs = { 800, 1200, 2500 }; int v = Math.Max(0, Math.Min(4, (int)Math.Round(fx.FormantVowel)));
        double[][] vowel = { new double[] { 800, 1200, 2500 }, new double[] { 400, 2000, 2600 }, new double[] { 300, 2300, 3000 }, new double[] { 450, 800, 2830 }, new double[] { 325, 700, 2530 } };
        fs = vowel[v]; double mix = Math.Max(0, Math.Min(1, fx.FormantMix)), shift = Math.Max(0.5, Math.Min(2, fx.FormantShift));
        for (int band = 0; band < 3; band++) {
            double fc = Math.Max(80, Math.Min(SR / 5.0, fs[band] * shift)), ff = 2 * Math.Sin(Math.PI * fc / SR), q = 0.35; double loA = 0, bdA = 0, loB = 0, bdB = 0;
            for (int i = 0; i < a.Length; i++) {
                double hiA = a[i] - loA - q * bdA; bdA += ff * hiA; loA += ff * bdA;
                double hiB = b[i] - loB - q * bdB; bdB += ff * hiB; loB += ff * bdB;
                double wa = bdA / 3.0, wb = bdB / 3.0;
                a[i] = (float)(a[i] * (1 - mix) + wa * mix); b[i] = (float)(b[i] * (1 - mix) + wb * mix);
            }
        }
    }
    static void LadderFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, fx.LadderMix));
        double fc = Math.Min(0.99, 2 * Math.Sin(Math.PI * Math.Max(40, Math.Min(16000, fx.LadderCutoff)) / SR));
        double k = 4 * Math.Max(0, Math.Min(0.95, fx.LadderReso)), drive = 1 + fx.LadderDrive * 8;
        double y1 = 0, y2 = 0, y3 = 0, y4 = 0, z1 = 0, z2 = 0, z3 = 0, z4 = 0;
        for (int i = 0; i < n; i++) {
            double xa = Math.Tanh(a[i] * drive) - k * Math.Tanh(y4), xb = Math.Tanh(b[i] * drive) - k * Math.Tanh(z4);
            y1 += fc * (xa - y1); y2 += fc * (y1 - y2); y3 += fc * (y2 - y3); y4 += fc * (y3 - y4);
            z1 += fc * (xb - z1); z2 += fc * (z1 - z2); z3 += fc * (z2 - z3); z4 += fc * (z3 - z4);
            y4 = Math.Max(-4, Math.Min(4, y4)); z4 = Math.Max(-4, Math.Min(4, z4));
            a[i] = (float)(a[i] * (1 - mix) + y4 * mix); b[i] = (float)(b[i] * (1 - mix) + z4 * mix);
        }
    }
    static void SubBassFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.SubBassMix)), w = 2 * Math.PI * Math.Max(20, Math.Min(120, fx.SubBassFreq)) / SR;
        double drive = 1 + fx.SubBassDrive * 4, c = Math.Exp(-2 * Math.PI * 140 / SR), lpA = 0, lpB = 0, ph = 0, env = 0;
        double atk = 1 - Math.Exp(-1.0 / (0.005 * SR)), rel = 1 - Math.Exp(-1.0 / (0.08 * SR));
        for (int i = 0; i < a.Length; i++) {
            lpA = (1 - c) * a[i] + c * lpA; lpB = (1 - c) * b[i] + c * lpB;
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            ph += w; if (ph > 2 * Math.PI) ph -= 2 * Math.PI;
            double s = Math.Sin(ph) * env * drive;
            a[i] = (float)(a[i] * (1 - mix) + (lpA * 0.5 + s * 0.5) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (lpB * 0.5 + s * 0.5) * mix);
        }
    }
    static void BassEnhFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.BassEnhMix)), amt = Math.Max(0, Math.Min(1, fx.BassEnhAmount));
        double c = Math.Exp(-2 * Math.PI * Math.Max(40, Math.Min(300, fx.BassEnhFreq)) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < a.Length; i++) {
            lpA = (1 - c) * a[i] + c * lpA; lpB = (1 - c) * b[i] + c * lpB;
            double ha = Math.Tanh(lpA * 4) - lpA, hb = Math.Tanh(lpB * 4) - lpB;
            a[i] = (float)(a[i] * (1 - mix) + (a[i] + ha * amt * 2) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (b[i] + hb * amt * 2) * mix);
        }
    }
    static void AirBandFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.AirMix)), g = Math.Pow(10, fx.AirGain / 20.0) - 1;
        double c = Math.Exp(-2 * Math.PI * Math.Max(2000, Math.Min(16000, fx.AirFreq)) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < a.Length; i++) {
            lpA = (1 - c) * a[i] + c * lpA; lpB = (1 - c) * b[i] + c * lpB;
            a[i] = (float)(a[i] * (1 - mix) + (a[i] + (a[i] - lpA) * g) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (b[i] + (b[i] - lpB) * g) * mix);
        }
    }
    static void TubeFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.TubeMix)), g = 1 + fx.TubeDrive * 10, bias = fx.TubeBias * 0.5;
        double c = Math.Exp(-2 * Math.PI * 18 / SR), dcA = 0, dcB = 0;
        for (int i = 0; i < a.Length; i++) {
            double ya = Math.Tanh(a[i] * g + bias) - Math.Tanh(bias), yb = Math.Tanh(b[i] * g + bias) - Math.Tanh(bias);
            dcA = (1 - c) * ya + c * dcA; dcB = (1 - c) * yb + c * dcB;
            a[i] = (float)(a[i] * (1 - mix) + (ya - dcA) * mix * 0.7);
            b[i] = (float)(b[i] * (1 - mix) + (yb - dcB) * mix * 0.7);
        }
    }
    static void TapeSatFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.TapeSatMix)), g = 1 + fx.TapeSatDrive * 8, tone = Math.Max(0, Math.Min(1, fx.TapeSatTone));
        double c = Math.Exp(-2 * Math.PI * (700 + tone * 11000) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < a.Length; i++) {
            double sa = Math.Tanh(a[i] * g) / Math.Tanh(g), sb = Math.Tanh(b[i] * g) / Math.Tanh(g);
            lpA = (1 - c) * sa + c * lpA; lpB = (1 - c) * sb + c * lpB;
            a[i] = (float)(a[i] * (1 - mix) + (sa + (sa - lpA) * tone) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (sb + (sb - lpB) * tone) * mix);
        }
    }
    static void DiodeFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.DiodeMix)), g = 1 + fx.DiodeDrive * 12, tone = Math.Max(0, Math.Min(1, fx.DiodeTone));
        double c = Math.Exp(-2 * Math.PI * (900 + tone * 9000) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < a.Length; i++) {
            double xa = a[i], xb = b[i];
            double ya = (1 - Math.Exp(-Math.Abs(xa) * g)) * Math.Sign(xa), yb = (1 - Math.Exp(-Math.Abs(xb) * g)) * Math.Sign(xb);
            lpA = (1 - c) * ya + c * lpA; lpB = (1 - c) * yb + c * lpB;
            a[i] = (float)(xa * (1 - mix) + (ya * (1 - tone) + lpA * tone) * mix * 0.8);
            b[i] = (float)(xb * (1 - mix) + (yb * (1 - tone) + lpB * tone) * mix * 0.8);
        }
    }
    static void FuzzFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.FuzzMix)), g = 1 + fx.FuzzAmount * 30, gate = fx.FuzzGate * 0.05;
        for (int i = 0; i < a.Length; i++) {
            double xa = a[i], xb = b[i];
            double ya = Math.Abs(xa) < gate ? 0 : Math.Tanh(xa * g), yb = Math.Abs(xb) < gate ? 0 : Math.Tanh(xb * g);
            a[i] = (float)(xa * (1 - mix) + ya * mix * 0.6); b[i] = (float)(xb * (1 - mix) + yb * mix * 0.6);
        }
    }
    static void ChipTuneFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.ChipMix));
        double steps = Math.Pow(2, Math.Max(2, Math.Min(12, Math.Round(fx.ChipBits))) - 1);
        int hold = Math.Max(1, (int)(SR / Math.Max(1000, fx.ChipRate * 1000)));
        float ha = 0, hb = 0;
        for (int i = 0; i < a.Length; i++) {
            if (i % hold == 0) { ha = (float)(Math.Round(a[i] * steps) / steps); hb = (float)(Math.Round(b[i] * steps) / steps); }
            a[i] = (float)(a[i] * (1 - mix) + ha * mix); b[i] = (float)(b[i] * (1 - mix) + hb * mix);
        }
    }
    static void FreqShiftFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.FreqShiftMix)), w = 2 * Math.PI * fx.FreqShiftHz / SR;
        double t = Math.Tan(Math.PI * 500 / SR), ap = (t - 1) / (t + 1), za = 0, zb = 0, ph = 0;
        for (int i = 0; i < a.Length; i++) {
            double xa = a[i], xb = b[i];
            double qa = ap * xa + za; za = xa - ap * qa;
            double qb = ap * xb + zb; zb = xb - ap * qb;
            ph += w; if (ph > Math.PI) ph -= 2 * Math.PI; if (ph < -Math.PI) ph += 2 * Math.PI;
            double co = Math.Cos(ph), si = Math.Sin(ph);
            a[i] = (float)(xa * (1 - mix) + (xa * co - qa * si) * mix);
            b[i] = (float)(xb * (1 - mix) + (xb * co - qb * si) * mix);
        }
    }
    static void BeatRepeatFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, div = Math.Max(2, Math.Min(16, (int)Math.Round(fx.BeatRepeatDiv)));
        int chunk = Math.Max(256, (int)(SR * 0.5 / div));
        double mix = Math.Max(0, Math.Min(1, fx.BeatRepeatMix)), chance = Math.Max(0, Math.Min(1, fx.BeatRepeatChance));
        float[] ba = new float[chunk], bb = new float[chunk]; Random r = new Random(777);
        int pos = 0, reps = 0, totalReps = 0; bool repeating = false;
        for (int i = 0; i < n; i++) {
            if (!repeating) {
                ba[pos] = a[i]; bb[pos] = b[i];
                a[i] = (float)(a[i] * (1 - mix) + ba[pos] * mix); b[i] = (float)(b[i] * (1 - mix) + bb[pos] * mix);
                pos++; if (pos >= chunk) { pos = 0; repeating = true; reps = 0; totalReps = r.NextDouble() < chance ? div : 1; }
            } else {
                a[i] = (float)(a[i] * (1 - mix) + ba[pos] * mix); b[i] = (float)(b[i] * (1 - mix) + bb[pos] * mix);
                pos++; if (pos >= chunk) { pos = 0; reps++; if (reps >= totalReps) repeating = false; }
            }
        }
    }
    static void GrainFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, bufLen = Math.Max(4096, (int)(SR * 1.0)), gmax = 8;
        float[] ba = new float[bufLen], bb = new float[bufLen];
        int[] gpos = new int[gmax], glen = new int[gmax], gcur = new int[gmax];
        for (int k = 0; k < gmax; k++) { glen[k] = 0; gcur[k] = 1; }
        int hop = Math.Max(1, (int)(SR / Math.Max(1, fx.GrainDensity))), since = hop, w = 0;
        double mix = Math.Max(0, Math.Min(1, fx.GrainMix));
        Random r = new Random(9001);
        for (int i = 0; i < n; i++) {
            ba[w] = a[i]; bb[w] = b[i]; w++; if (w >= bufLen) w = 0;
            since++;
            if (since >= hop) {
                since = 0;
                for (int k = 0; k < gmax; k++) {
                    if (gcur[k] >= glen[k]) {
                        int gl = Math.Max(64, (int)(Math.Max(10, fx.GrainSize) * 0.001 * SR * (0.75 + 0.5 * r.NextDouble())));
                        int maxBack = Math.Min(w, bufLen / 2);
                        if (maxBack > gl + 1) { gpos[k] = (w - gl - r.Next(maxBack - gl)) % bufLen; glen[k] = gl; gcur[k] = 0; }
                        break;
                    }
                }
            }
            double oa = 0, ob = 0; int active = 0;
            for (int k = 0; k < gmax; k++) {
                if (gcur[k] < glen[k]) {
                    int idx = (gpos[k] + gcur[k]) % bufLen;
                    double win = 0.5 - 0.5 * Math.Cos(2 * Math.PI * gcur[k] / Math.Max(1, glen[k]));
                    oa += ba[idx] * win; ob += bb[idx] * win; gcur[k]++; active++;
                }
            }
            if (active > 0) { double norm = 0.7 / Math.Sqrt(active); a[i] = (float)(a[i] * (1 - mix) + oa * norm * mix); b[i] = (float)(b[i] * (1 - mix) + ob * norm * mix); }
        }
    }
    static void PingPongFx(Fx fx, float[] a, float[] b) {
        int d = Math.Max(2, (int)(fx.PingPongTime * 0.001 * SR)); float[] dl = new float[d], dr = new float[d]; int pos = 0;
        double fb = Math.Max(0, Math.Min(0.95, fx.PingPongFb)), mix = Math.Max(0, Math.Min(1, fx.PingPongMix));
        for (int i = 0; i < a.Length; i++) {
            double yl = dl[pos], yr = dr[pos];
            dl[pos] = (float)(a[i] + yr * fb); dr[pos] = (float)(b[i] + yl * fb);
            pos++; if (pos >= d) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + yl * mix); b[i] = (float)(b[i] * (1 - mix) + yr * mix);
        }
    }
    static void DubDelayFx(Fx fx, float[] a, float[] b) {
        int d = Math.Max(2, (int)(fx.DubTime * 0.001 * SR)); float[] dl = new float[d], dr = new float[d]; int pos = 0;
        double fb = Math.Max(0, Math.Min(0.95, fx.DubFb)), mix = Math.Max(0, Math.Min(1, fx.DubMix));
        double c = Math.Exp(-2 * Math.PI * (400 + fx.DubTone * 6000) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < a.Length; i++) {
            double yl = dl[pos], yr = dr[pos];
            lpA = (1 - c) * yl + c * lpA; lpB = (1 - c) * yr + c * lpB;
            dl[pos] = (float)(a[i] + lpA * fb); dr[pos] = (float)(b[i] + lpB * fb);
            pos++; if (pos >= d) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + yl * mix); b[i] = (float)(b[i] * (1 - mix) + yr * mix);
        }
    }
    static void VerbCore(Fx fx, float[] a, float[] b, double mixv, double size, double damp, double scale) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, mixv)), sz = Math.Max(0, Math.Min(1, size)), dp = Math.Max(0, Math.Min(1, damp));
        int baseLen = Math.Max(64, (int)(SR * 0.03 * scale)); double fb = 0.55 + 0.4 * sz;
        int[] combLens = { baseLen, (int)(baseLen * 1.13), (int)(baseLen * 0.97), (int)(baseLen * 1.27) };
        for (int ch = 0; ch < 2; ch++) {
            float[] src = ch == 0 ? a : b; float[] wet = new float[n];
            for (int c = 0; c < combLens.Length; c++) {
                int len = combLens[c] + ch * 37; float[] buf = new float[len]; int idx = 0; double store = 0;
                for (int i = 0; i < n; i++) { double outv = buf[idx]; store = outv * (1 - dp) + store * dp; buf[idx] = (float)(src[i] + store * fb); if (++idx >= len) idx = 0; wet[i] += (float)outv; }
            }
            int[] apLens = { (int)(baseLen * 0.23) + ch * 13 + 5, (int)(baseLen * 0.41) + ch * 7 + 3 };
            for (int k = 0; k < apLens.Length; k++) {
                int len = Math.Max(5, apLens[k]); float[] buf = new float[len]; int idx = 0;
                for (int i = 0; i < n; i++) { double bo = buf[idx]; double x = wet[i]; wet[i] = (float)(-x + bo); buf[idx] = (float)(x + bo * 0.5); if (++idx >= len) idx = 0; }
            }
            for (int i = 0; i < n; i++) src[i] = (float)(src[i] * (1 - mix) + wet[i] * 0.25 * mix);
        }
    }
    static void WahPedalFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.WahPedalMix)), rate = Math.Max(0.05, fx.WahPedalRate), depth = Math.Max(0, Math.Min(1, fx.WahPedalDepth));
        double damp = 1 - Math.Max(0, Math.Min(0.95, fx.WahPedalQ)) * 0.9, loA = 0, bdA = 0, loB = 0, bdB = 0;
        for (int i = 0; i < a.Length; i++) {
            double lfo = 0.5 + 0.5 * Math.Sin(2 * Math.PI * rate * i / SR);
            double fc = Math.Max(120, Math.Min(SR / 5.0, 300 + depth * lfo * 2500)), ff = 2 * Math.Sin(Math.PI * fc / SR);
            double xa = a[i], xb = b[i];
            double hiA = xa - loA - damp * bdA; bdA += ff * hiA; loA += ff * bdA;
            double hiB = xb - loB - damp * bdB; bdB += ff * hiB; loB += ff * bdB;
            a[i] = (float)(xa * (1 - mix) + bdA * mix * 1.6); b[i] = (float)(xb * (1 - mix) + bdB * mix * 1.6);
        }
    }
    static void TalkBoxFx(Fx fx, float[] a, float[] b) {
        double[][] vowel = { new double[] { 800, 1200, 2500 }, new double[] { 400, 2000, 2600 }, new double[] { 300, 2300, 3000 }, new double[] { 450, 800, 2830 }, new double[] { 325, 700, 2530 } };
        double mix = Math.Max(0, Math.Min(1, fx.TalkMix)), rate = Math.Max(0.05, fx.TalkRate), off = Math.Max(0, Math.Min(4, fx.TalkVowel));
        double[] loA = new double[3], bdA = new double[3], loB = new double[3], bdB = new double[3];
        for (int i = 0; i < a.Length; i++) {
            int v = (int)((i * rate / SR + off)) % 5; if (v < 0) v += 5;
            double[] fs = vowel[v]; double xa = a[i], xb = b[i], oa = 0, ob = 0;
            for (int band = 0; band < 3; band++) {
                double fc = Math.Max(80, Math.Min(SR / 5.0, fs[band])), ff = 2 * Math.Sin(Math.PI * fc / SR), damp = 0.4;
                double hiA = xa - loA[band] - damp * bdA[band]; bdA[band] += ff * hiA; loA[band] += ff * bdA[band];
                double hiB = xb - loB[band] - damp * bdB[band]; bdB[band] += ff * hiB; loB[band] += ff * bdB[band];
                oa += bdA[band]; ob += bdB[band];
            }
            a[i] = (float)(xa * (1 - mix) + oa * mix * 0.8); b[i] = (float)(xb * (1 - mix) + ob * mix * 0.8);
        }
    }
    static void DuckerFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.DuckerMix)), thr = Math.Pow(10, fx.DuckerThreshold / 20.0), depth = Math.Max(0, Math.Min(1, fx.DuckerDepth));
        double atk = 1 - Math.Exp(-1.0 / (0.003 * SR)), rel = 1 - Math.Exp(-1.0 / (Math.Max(10, fx.DuckerRelease) * 0.001 * SR));
        double env = 0, gain = 1;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double target = env > thr ? 1 - depth : 1; gain += (target - gain) * (target < gain ? atk : rel);
            double g = 1 - mix + mix * gain; a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void RiderFx(Fx fx, float[] a, float[] b) {
        double amount = Math.Max(0, Math.Min(1, fx.RiderAmount)), target = Math.Pow(10, fx.RiderTarget / 20.0);
        double coef = 1 - Math.Exp(-1.0 / (Math.Max(0.01, fx.RiderSpeed) * 0.5 * SR));
        double env = 0, gain = 1;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? 0.01 : coef) * (x - env);
            double desired = env > 0.0005 ? target / env : 1; desired = Math.Max(0.1, Math.Min(4, desired));
            gain += (desired - gain) * coef;
            double g = 1 - amount + amount * gain; a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void StereoImageFx(Fx fx, float[] a, float[] b) {
        double width = Math.Max(0, Math.Min(2, fx.StereoWidth)), bass = Math.Max(0, fx.BassMonoFreq);
        double c = Math.Exp(-2 * Math.PI * (bass > 0 ? bass : 120) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < a.Length; i++) {
            double mid = (a[i] + b[i]) * 0.5, side = (a[i] - b[i]) * 0.5 * width;
            double l = mid + side, r = mid - side;
            if (bass > 0) {
                lpA = (1 - c) * l + c * lpA; lpB = (1 - c) * r + c * lpB;
                double lowM = (lpA + lpB) * 0.5; l = l - lpA + lowM; r = r - lpB + lowM;
            }
            a[i] = (float)l; b[i] = (float)r;
        }
    }
    static void HarmTremFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.HarmTremMix)), rate = Math.Max(0.1, fx.HarmTremRate), depth = Math.Max(0, Math.Min(1, fx.HarmTremDepth));
        for (int i = 0; i < a.Length; i++) {
            double ph = 2 * Math.PI * rate * i / SR;
            double gl = 1 - depth * (0.5 + 0.5 * Math.Sin(ph)), gr = 1 - depth * (0.5 + 0.5 * Math.Sin(ph + Math.PI / 2));
            a[i] = (float)(a[i] * (1 - mix + mix * gl) * 1.3); b[i] = (float)(b[i] * (1 - mix + mix * gr) * 1.3);
        }
    }
    static void GlitchGateFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.GlitchMix)), dens = Math.Max(0, Math.Min(1, fx.GlitchDensity));
        Random r = new Random((int)(Math.Max(0, fx.GlitchSeed) * 977) + 13);
        bool open = true; int remain = 0;
        for (int i = 0; i < a.Length; i++) {
            if (remain <= 0) {
                open = !open;
                double avg = open ? 0.02 + 0.10 * (1 - dens) : 0.015 + 0.20 * dens;
                remain = Math.Max(32, (int)(SR * avg * (0.4 + r.NextDouble())));
            }
            remain--;
            double g = open ? 1 : 0;
            a[i] = (float)(a[i] * (1 - mix + mix * g)); b[i] = (float)(b[i] * (1 - mix + mix * g));
        }
    }
    static void SweepFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.SweepMix)), rate = Math.Max(0.05, fx.SweepRate), depth = Math.Max(0, Math.Min(1, fx.SweepDepth));
        double damp = 1 - Math.Max(0, Math.Min(0.95, fx.SweepQ)) * 0.9, center = Math.Max(80, fx.SweepCenter);
        double loA = 0, bdA = 0, loB = 0, bdB = 0;
        for (int i = 0; i < a.Length; i++) {
            double lfo = Math.Sin(2 * Math.PI * rate * i / SR);
            double fc = Math.Max(40, Math.Min(SR / 5.0, center * Math.Pow(2, depth * lfo * 2))), ff = 2 * Math.Sin(Math.PI * fc / SR);
            double xa = a[i], xb = b[i];
            double hiA = xa - loA - damp * bdA; bdA += ff * hiA; loA += ff * bdA;
            double hiB = xb - loB - damp * bdB; bdB += ff * hiB; loB += ff * bdB;
            a[i] = (float)(xa * (1 - mix) + bdA * mix * 1.4); b[i] = (float)(xb * (1 - mix) + bdB * mix * 1.4);
        }
    }
    static void OptoCompFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.OptoMix)), thr = Math.Pow(10, fx.OptoThresh / 20.0), ratio = Math.Max(1, fx.OptoRatio);
        double atk = 1 - Math.Exp(-1.0 / (0.01 * SR)), rel = 1 - Math.Exp(-1.0 / (Math.Max(20, fx.OptoRelease) * 0.001 * SR));
        double env = 0, gain = 1;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double target = env > thr ? Math.Pow(env / thr, 1.0 / ratio - 1.0) : 1;
            gain += (target - gain) * (target < gain ? atk : rel);
            double g = 1 - mix + mix * gain; a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void FetCompFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.FetMix)), thr = Math.Pow(10, fx.FetThresh / 20.0), ratio = Math.Max(1, fx.FetRatio);
        double atk = 1 - Math.Exp(-1.0 / (Math.Max(0.1, fx.FetAttack) * 0.001 * SR)), rel = 1 - Math.Exp(-1.0 / (0.08 * SR));
        double env = 0, gain = 1;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double target = env > thr ? Math.Pow(env / thr, 1.0 / ratio - 1.0) : 1;
            gain += (target - gain) * (target < gain ? atk : rel);
            double g = 1 - mix + mix * gain; a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void VcaCompFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.VcaMix)), thr = Math.Pow(10, fx.VcaThresh / 20.0), ratio = Math.Max(1, fx.VcaRatio);
        double atk = 1 - Math.Exp(-1.0 / (0.02 * SR)), rel = 1 - Math.Exp(-1.0 / (Math.Max(20, fx.VcaRelease) * 0.001 * SR));
        double env = 0, gain = 1;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double target = env > thr ? Math.Pow(env / thr, 1.0 / ratio - 1.0) : 1;
            gain += (target - gain) * (target < gain ? atk : rel);
            double g = 1 - mix + mix * gain; a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void ExpanderFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.ExpMix)), thr = Math.Pow(10, fx.ExpThresh / 20.0), ratio = Math.Max(1, fx.ExpRatio);
        double floorGain = Math.Pow(10, -Math.Max(3, fx.ExpRange) / 20.0), atk = 1 - Math.Exp(-1.0 / (0.005 * SR)), rel = 1 - Math.Exp(-1.0 / (0.15 * SR));
        double env = 0, gain = 1;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double target = env < thr ? Math.Max(floorGain, Math.Pow(Math.Max(1e-6, env) / thr, ratio - 1)) : 1;
            gain += (target - gain) * (target < gain ? atk : rel);
            double g = 1 - mix + mix * gain; a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void MSCompFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.MSCompMix)), thr = Math.Pow(10, fx.MSCompThresh / 20.0), ratio = Math.Max(1, fx.MSCompRatio);
        double atk = 1 - Math.Exp(-1.0 / (0.01 * SR)), rel = 1 - Math.Exp(-1.0 / (Math.Max(20, fx.MSCompRelease) * 0.001 * SR));
        double em = 0, es = 0, gm = 1, gs = 1;
        for (int i = 0; i < a.Length; i++) {
            double xa = a[i], xb = b[i], mid = (xa + xb) * 0.5, side = (xa - xb) * 0.5;
            em += (Math.Abs(mid) > em ? atk : rel) * (Math.Abs(mid) - em);
            es += (Math.Abs(side) > es ? atk : rel) * (Math.Abs(side) - es);
            double tm = em > thr ? Math.Pow(em / thr, 1.0 / ratio - 1.0) : 1;
            double ts = es > thr ? Math.Pow(es / thr, 1.0 / ratio - 1.0) : 1;
            gm += (tm - gm) * (tm < gm ? atk : rel); gs += (ts - gs) * (ts < gs ? atk : rel);
            double om = mid * (1 - mix + mix * gm), os = side * (1 - mix + mix * gs);
            a[i] = (float)(om + os); b[i] = (float)(om - os);
        }
    }
    static void DynEqFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.DynEqMix)), fc = Math.Max(80, Math.Min(12000, fx.DynEqFreq));
        double thr = Math.Pow(10, fx.DynEqThresh / 20.0), amt = Math.Max(0, Math.Min(1, fx.DynEqAmount)), ff = 2 * Math.Sin(Math.PI * fc / SR), damp = 0.4;
        double loA = 0, bdA = 0, loB = 0, bdB = 0, env = 0, atk = 1 - Math.Exp(-1.0 / (0.005 * SR)), rel = 1 - Math.Exp(-1.0 / (0.1 * SR));
        for (int i = 0; i < a.Length; i++) {
            double hiA = a[i] - loA - damp * bdA; bdA += ff * hiA; loA += ff * bdA;
            double hiB = b[i] - loB - damp * bdB; bdB += ff * hiB; loB += ff * bdB;
            double bandA = bdA, bandB = bdB, x = Math.Max(Math.Abs(bandA), Math.Abs(bandB));
            env += (x > env ? atk : rel) * (x - env);
            double g = env > thr ? Math.Pow(env / thr, -amt * 2) : 1;
            a[i] = (float)(a[i] * (1 - mix) + (a[i] - bandA + bandA * g) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (b[i] - bandB + bandB * g) * mix);
        }
    }
    static void PultecFx(Fx fx, float[] a, float[] b) {
        double lowGain = Math.Pow(10, fx.PultecLow / 20.0) - 1, highGain = Math.Pow(10, fx.PultecHigh / 20.0) - 1, atten = 1 - Math.Pow(10, -fx.PultecAtten / 20.0);
        double cl = Math.Exp(-2 * Math.PI * 100 / SR), ch = Math.Exp(-2 * Math.PI * 10000 / SR);
        double lA = 0, lB = 0, hA = 0, hB = 0;
        for (int i = 0; i < a.Length; i++) {
            lA = (1 - cl) * a[i] + cl * lA; lB = (1 - cl) * b[i] + cl * lB;
            hA = (1 - ch) * a[i] + ch * hA; hB = (1 - ch) * b[i] + ch * hB;
            double hiA = a[i] - hA, hiB = b[i] - hB;
            a[i] = (float)(a[i] + lA * lowGain + hiA * highGain - hiA * atten);
            b[i] = (float)(b[i] + lB * lowGain + hiB * highGain - hiB * atten);
        }
    }
    static void OverdriveFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.OverdriveMix)), g = 1 + fx.OverdriveDrive * 20, tone = Math.Max(0, Math.Min(1, fx.OverdriveTone));
        double c = Math.Exp(-2 * Math.PI * (800 + tone * 8000) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < a.Length; i++) {
            double xa = a[i], xb = b[i], ya = Math.Tanh(xa * g + 0.15) - Math.Tanh(0.15), yb = Math.Tanh(xb * g + 0.15) - Math.Tanh(0.15);
            lpA = (1 - c) * ya + c * lpA; lpB = (1 - c) * yb + c * lpB;
            a[i] = (float)(xa * (1 - mix) + (ya * (1 - 0.5 * tone) + lpA * 0.5 * tone) * mix * 0.8);
            b[i] = (float)(xb * (1 - mix) + (yb * (1 - 0.5 * tone) + lpB * 0.5 * tone) * mix * 0.8);
        }
    }
    static void MetalDriveFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.MetalMix)), g = 1 + fx.MetalDrive * 40, scoop = Math.Max(0, Math.Min(1, fx.MetalMid));
        double chp = Math.Exp(-2 * Math.PI * 120 / SR), cmid = Math.Exp(-2 * Math.PI * 850 / SR), ct = Math.Exp(-2 * Math.PI * 6500 / SR);
        double hpA = 0, hpB = 0, mA = 0, mB = 0, tA = 0, tB = 0;
        for (int i = 0; i < a.Length; i++) {
            hpA = (1 - chp) * a[i] + chp * hpA; hpB = (1 - chp) * b[i] + chp * hpB;
            double yA = Math.Tanh((a[i] - hpA) * g) * 0.55, yB = Math.Tanh((b[i] - hpB) * g) * 0.55;
            mA = (1 - cmid) * yA + cmid * mA; mB = (1 - cmid) * yB + cmid * mB;
            yA -= mA * scoop * 1.5; yB -= mB * scoop * 1.5;
            tA = (1 - ct) * yA + ct * tA; tB = (1 - ct) * yB + ct * tB;
            a[i] = (float)(a[i] * (1 - mix) + tA * mix); b[i] = (float)(b[i] * (1 - mix) + tB * mix);
        }
    }
    static void CabSimFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.CabMix)), g = 1 + fx.CabDrive * 10, tone = Math.Max(0, Math.Min(1, fx.CabTone));
        double c = Math.Exp(-2 * Math.PI * (1500 + tone * 4500) / SR), cb = Math.Exp(-2 * Math.PI * 110 / SR);
        double lpA = 0, lpB = 0, bA = 0, bB = 0;
        for (int i = 0; i < a.Length; i++) {
            double ya = Math.Tanh(a[i] * g) * 0.6, yb = Math.Tanh(b[i] * g) * 0.6;
            lpA = (1 - c) * ya + c * lpA; lpB = (1 - c) * yb + c * lpB;
            bA = (1 - cb) * ya + cb * bA; bB = (1 - cb) * yb + cb * bB;
            a[i] = (float)(a[i] * (1 - mix) + (lpA + bA * 0.3) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (lpB + bB * 0.3) * mix);
        }
    }
    static void MultibandDriveFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.MBDistMix)), dl = Math.Max(0, Math.Min(1, fx.MBDistLow)), dm = Math.Max(0, Math.Min(1, fx.MBDistMid)), dh = Math.Max(0, Math.Min(1, fx.MBDistHigh));
        double c1 = Math.Exp(-2 * Math.PI * 220 / SR), c2 = Math.Exp(-2 * Math.PI * 2200 / SR);
        double l1a = 0, l1b = 0, l2a = 0, l2b = 0;
        for (int i = 0; i < a.Length; i++) {
            l1a = (1 - c1) * a[i] + c1 * l1a; l1b = (1 - c1) * b[i] + c1 * l1b;
            l2a = (1 - c2) * a[i] + c2 * l2a; l2b = (1 - c2) * b[i] + c2 * l2b;
            double loA = l1a, loB = l1b, midA = l2a - l1a, midB = l2b - l1b, hiA = a[i] - l2a, hiB = b[i] - l2b;
            double yA = Math.Tanh(loA * (1 + dl * 6)) + Math.Tanh(midA * (1 + dm * 10)) + Math.Tanh(hiA * (1 + dh * 14));
            double yB = Math.Tanh(loB * (1 + dl * 6)) + Math.Tanh(midB * (1 + dm * 10)) + Math.Tanh(hiB * (1 + dh * 14));
            a[i] = (float)(a[i] * (1 - mix) + yA * 0.5 * mix); b[i] = (float)(b[i] * (1 - mix) + yB * 0.5 * mix);
        }
    }
    static void TapeFlutterFx(Fx fx, float[] a, float[] b) {
        int len = Math.Max(64, (int)(SR * 0.05)); float[] dl = new float[len], dr = new float[len]; int pos = 0;
        double mix = Math.Max(0, Math.Min(1, fx.FlutterMix)), rate = Math.Max(0.05, fx.FlutterRate), depth = Math.Max(0, Math.Min(1, fx.FlutterDepth));
        for (int i = 0; i < a.Length; i++) {
            double lfo = 0.5 + 0.5 * Math.Sin(2 * Math.PI * rate * i / SR) + 0.25 * Math.Sin(2 * Math.PI * rate * 3.1 * i / SR);
            double dt = (0.003 + 0.006 * depth * lfo) * SR; int di = Math.Max(1, Math.Min(len - 1, (int)dt));
            int rp = pos - di; if (rp < 0) rp += len;
            double ya = dl[rp], yb = dr[rp];
            dl[pos] = a[i]; dr[pos] = b[i]; pos++; if (pos >= len) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void DimensionFx(Fx fx, float[] a, float[] b) {
        int len = Math.Max(64, (int)(SR * 0.04)); float[] dl = new float[len], dr = new float[len]; int pos = 0;
        double mix = Math.Max(0, Math.Min(1, fx.DimMix)), rate = Math.Max(0.05, fx.DimRate), depth = Math.Max(0, Math.Min(1, fx.DimDepth));
        for (int i = 0; i < a.Length; i++) {
            double ph = 2 * Math.PI * rate * i / SR;
            int d1 = Math.Max(1, (int)((0.008 + 0.006 * depth * (0.5 + 0.5 * Math.Sin(ph))) * SR));
            int d2 = Math.Max(1, (int)((0.008 + 0.006 * depth * (0.5 + 0.5 * Math.Sin(ph + Math.PI)))) % len);
            int r1 = pos - d1; if (r1 < 0) r1 += len; int r2 = pos - d2; if (r2 < 0) r2 += len;
            double ya = dl[r1], yb = dr[r2]; dl[pos] = a[i]; dr[pos] = b[i]; pos++; if (pos >= len) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + (a[i] * 0.5 + ya * 0.5) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (b[i] * 0.5 + yb * 0.5) * mix);
        }
    }
    static void EnvPhaserFx(Fx fx, float[] a, float[] b) {
        int stages = 4; double mix = Math.Max(0, Math.Min(1, fx.EnvPhaserMix)), thr = Math.Pow(10, fx.EnvPhaserThresh / 20.0);
        double depth = Math.Max(0, Math.Min(1, fx.EnvPhaserDepth)), rate = Math.Max(0, fx.EnvPhaserRate);
        double[] za = new double[stages], zb = new double[stages]; double env = 0, atk = 1 - Math.Exp(-1.0 / (0.003 * SR)), rel = 1 - Math.Exp(-1.0 / (0.1 * SR));
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double drive = env > thr ? Math.Min(1, (env - thr) / Math.Max(1e-6, thr * 4)) : 0;
            double lfo = rate > 0 ? Math.Sin(2 * Math.PI * rate * i / SR) : 0;
            double fc = Math.Max(120, Math.Min(12000, 300 + depth * (drive * 2800 + lfo * 700)));
            double t = Math.Tan(Math.PI * fc / SR), c = (t - 1) / (t + 1), xa = a[i], xb = b[i];
            for (int s = 0; s < stages; s++) {
                double xa2 = xa, xb2 = xb; xa = c * xa2 + za[s]; za[s] = xa2 - c * xa; xb = c * xb2 + zb[s]; zb[s] = xb2 - c * xb;
            }
            a[i] = (float)(a[i] * (1 - mix) + (a[i] + xa) * 0.5 * mix); b[i] = (float)(b[i] * (1 - mix) + (b[i] + xb) * 0.5 * mix);
        }
    }
    static void EnvFlangerFx(Fx fx, float[] a, float[] b) {
        int len = Math.Max(64, (int)(SR * 0.03)); float[] dl = new float[len], dr = new float[len]; int pos = 0;
        double mix = Math.Max(0, Math.Min(1, fx.EnvFlangeMix)), thr = Math.Pow(10, fx.EnvFlangeThresh / 20.0), depth = Math.Max(0, Math.Min(1, fx.EnvFlangeDepth)), fb = Math.Max(0, Math.Min(0.95, fx.EnvFlangeFb));
        double env = 0, atk = 1 - Math.Exp(-1.0 / (0.003 * SR)), rel = 1 - Math.Exp(-1.0 / (0.12 * SR));
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double drive = env > thr ? Math.Min(1, (env - thr) / Math.Max(1e-6, thr * 4)) : 0;
            int di = Math.Max(1, Math.Min(len - 1, (int)((0.001 + depth * drive * 0.012) * SR)));
            int r1 = pos - di; if (r1 < 0) r1 += len; int r2 = pos - di; if (r2 < 0) r2 += len;
            double ya = dl[r1], yb = dr[r2];
            dl[pos] = (float)(a[i] + yb * fb); dr[pos] = (float)(b[i] + ya * fb); pos++; if (pos >= len) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void ChopperFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.ChopMix)), rate = Math.Max(0.5, fx.ChopRate), depth = Math.Max(0, Math.Min(1, fx.ChopDepth)), shape = Math.Max(0, Math.Min(1, fx.ChopShape));
        for (int i = 0; i < a.Length; i++) {
            double s = 0.5 + 0.5 * Math.Sin(2 * Math.PI * rate * i / SR);
            double gate = (1 - shape) * s + shape * (s > 0.5 ? 1 : 0);
            double g = 1 - depth * (1 - gate);
            a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void BarberPoleFx(Fx fx, float[] a, float[] b) {
        int stages = Math.Max(2, Math.Min(8, (int)Math.Round(fx.BarberStages)));
        double mix = Math.Max(0, Math.Min(1, fx.BarberMix)), rate = Math.Max(0.05, fx.BarberRate), depth = Math.Max(0, Math.Min(1, fx.BarberDepth));
        double[] za = new double[stages], zb = new double[stages];
        for (int i = 0; i < a.Length; i++) {
            double xa = a[i], xb = b[i];
            for (int s = 0; s < stages; s++) {
                double ph = (i * rate / SR + (double)s / stages) % 1.0;
                double fc = Math.Max(120, Math.Min(14000, 200 * Math.Pow(2, ph * 5 * depth)));
                double t = Math.Tan(Math.PI * fc / SR), c = (t - 1) / (t + 1);
                double xa2 = xa, xb2 = xb; xa = c * xa2 + za[s]; za[s] = xa2 - c * xa; xb = c * xb2 + zb[s]; zb[s] = xb2 - c * xb;
            }
            a[i] = (float)(a[i] * (1 - mix) + (a[i] + xa) * 0.5 * mix); b[i] = (float)(b[i] * (1 - mix) + (b[i] + xb) * 0.5 * mix);
        }
    }
    static void HalfSpeedFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.HalfSpeedMix)), amt = Math.Max(0, Math.Min(1, fx.HalfSpeedAmount));
        double ratio = 1.0 / (1.0 + amt); float[] sa = (float[])a.Clone(), sb = (float[])b.Clone(); int n = a.Length;
        for (int i = 0; i < n; i++) {
            double src = i * ratio; int j = (int)src; double fr = src - j;
            if (j + 1 < n) {
                double ya = sa[j] * (1 - fr) + sa[j + 1] * fr, yb = sb[j] * (1 - fr) + sb[j + 1] * fr;
                a[i] = (float)(sa[i] * (1 - mix) + ya * mix); b[i] = (float)(sb[i] * (1 - mix) + yb * mix);
            }
        }
    }
    static void PitchEchoFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, d = Math.Max(2, (int)(fx.PitchEchoTime * 0.001 * SR)); if (d >= n) return;
        float[] dl = new float[d], dr = new float[d], wa = new float[n], wb = new float[n]; int pos = 0;
        double fb = Math.Max(0, Math.Min(0.9, fx.PitchEchoFb)), mix = Math.Max(0, Math.Min(1, fx.PitchEchoMix));
        for (int i = 0; i < n; i++) {
            double yl = dl[pos], yr = dr[pos]; wa[i] = (float)yl; wb[i] = (float)yr;
            dl[pos] = (float)(a[i] + yl * fb); dr[pos] = (float)(b[i] + yr * fb); pos++; if (pos >= d) pos = 0;
        }
        double ratio = Math.Pow(2, Math.Max(-12, Math.Min(12, fx.PitchEchoSemi)) / 12.0);
        for (int i = 0; i < n; i++) {
            double src = i * ratio; int j = (int)src; double fr = src - j;
            double ya = (j + 1 < n) ? wa[j] * (1 - fr) + wa[j + 1] * fr : 0, yb = (j + 1 < n) ? wb[j] * (1 - fr) + wb[j + 1] * fr : 0;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void SpectralFreezeFx(Fx fx, float[] a, float[] b) {
        int len = Math.Max(1024, (int)(SR * 0.5)); float[] fa = new float[len], fb2 = new float[len]; int w = 0, rp = 0;
        double mix = Math.Max(0, Math.Min(1, fx.FreezeMix)), thr = Math.Pow(10, fx.FreezeThreshold / 20.0);
        double dec = Math.Exp(-1.0 / (Math.Max(0.05, fx.FreezeDecay * 2.0) * SR)), gain = 1, env = 0;
        double atk = 1 - Math.Exp(-1.0 / (0.002 * SR)), rel = 1 - Math.Exp(-1.0 / (0.05 * SR)); bool frozen = false;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            if (env >= thr) {
                frozen = false; gain = 1; fa[w] = a[i]; fb2[w] = b[i]; w++; if (w >= len) w = 0;
                a[i] = (float)(a[i]); b[i] = (float)(b[i]);
            } else {
                if (!frozen) { frozen = true; rp = w; }
                double ya = fa[rp], yb = fb2[rp]; rp++; if (rp >= len) rp = 0; gain *= dec;
                a[i] = (float)(a[i] * (1 - mix) + ya * gain * mix); b[i] = (float)(b[i] * (1 - mix) + yb * gain * mix);
            }
        }
    }
    static void ReverseDelayFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, d = Math.Max(64, (int)(fx.RevDelayTime * 0.001 * SR)); if (d >= n) return;
        float[] sa = (float[])a.Clone(), sb = (float[])b.Clone();
        float[] wa = new float[n], wb = new float[n];
        for (int s = 0; s + d <= n; s += d) {
            for (int j = 0; j < d && s + d + j < n; j++) { wa[s + d + j] = sa[s + d - 1 - j]; wb[s + d + j] = sb[s + d - 1 - j]; }
        }
        double fb = Math.Max(0, Math.Min(0.9, fx.RevDelayFb));
        for (int i = d; i < n; i++) { wa[i] += (float)(wa[i - d] * fb * 0.5); wb[i] += (float)(wb[i - d] * fb * 0.5); }
        double mix = Math.Max(0, Math.Min(1, fx.RevDelayMix));
        for (int i = 0; i < n; i++) { a[i] = (float)(sa[i] * (1 - mix) + wa[i] * mix); b[i] = (float)(sb[i] * (1 - mix) + wb[i] * mix); }
    }
    static void EchoVerbFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, d = Math.Max(2, (int)(fx.EchoVerbTime * 0.001 * SR)); if (d >= n) return;
        float[] dl = new float[d], dr = new float[d], wa = (float[])a.Clone(), wb = (float[])b.Clone(); int pos = 0;
        double fb = Math.Max(0, Math.Min(0.9, fx.EchoVerbFb));
        for (int i = 0; i < n; i++) {
            double yl = dl[pos], yr = dr[pos];
            dl[pos] = (float)(wa[i] + yl * fb); dr[pos] = (float)(wb[i] + yr * fb); pos++; if (pos >= d) pos = 0;
            wa[i] = (float)yl; wb[i] = (float)yr;
        }
        VerbCore(fx, wa, wb, 1.0, fx.EchoVerbSize, 0.5, 1.0);
        double mix = Math.Max(0, Math.Min(1, fx.EchoVerbMix));
        for (int i = 0; i < n; i++) { a[i] = (float)(a[i] * (1 - mix) + wa[i] * mix); b[i] = (float)(b[i] * (1 - mix) + wb[i] * mix); }
    }
    static void BloomVerbFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; float[] wa = (float[])a.Clone(), wb = (float[])b.Clone();
        VerbCore(fx, wa, wb, 1.0, fx.BloomSize, 0.5, 1.3);
        double mix = Math.Max(0, Math.Min(1, fx.BloomMix)), thr = Math.Pow(10, fx.BloomThresh / 20.0);
        double atk = 1 - Math.Exp(-1.0 / (Math.Max(0.01, fx.BloomAttack) * SR)), rel = 1 - Math.Exp(-1.0 / (0.3 * SR));
        double env = 0, gate = 0, lvl = 0, lvlAtk = 1 - Math.Exp(-1.0 / (0.002 * SR));
        for (int i = 0; i < n; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); lvl += (x - lvl) * lvlAtk;
            double target = lvl > thr ? 1 : 0; env += (target - env) * (target > env ? atk : rel); gate = env;
            a[i] = (float)(a[i] * (1 - mix) + wa[i] * gate * mix); b[i] = (float)(b[i] * (1 - mix) + wb[i] * gate * mix);
        }
    }
    static void NonLinVerbFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; float[] wa = (float[])a.Clone(), wb = (float[])b.Clone();
        VerbCore(fx, wa, wb, 1.0, fx.NonLinVerbSize, 0.4, 1.0);
        double mix = Math.Max(0, Math.Min(1, fx.NonLinVerbMix)), drive = 1 + fx.NonLinVerbDrive * 8, gate = Math.Max(0, Math.Min(1, fx.NonLinVerbGate));
        for (int i = 0; i < n; i++) {
            double ya = Math.Tanh(wa[i] * drive) * (1 - gate) + wa[i] * gate, yb = Math.Tanh(wb[i] * drive) * (1 - gate) + wb[i] * gate;
            a[i] = (float)(a[i] * (1 - mix) + ya * 0.7 * mix); b[i] = (float)(b[i] * (1 - mix) + yb * 0.7 * mix);
        }
    }
    static void VocoderFx(Fx fx, float[] a, float[] b) {
        int bands = Math.Max(4, Math.Min(12, (int)Math.Round(fx.VocoderBands)));
        double mix = Math.Max(0, Math.Min(1, fx.VocoderMix)), rel = 1 - Math.Exp(-1.0 / (Math.Max(10, fx.VocoderRelease) * 0.001 * SR));
        double[] la = new double[bands], ba = new double[bands], lc = new double[bands], bc = new double[bands], env = new double[bands];
        Random rnd = new Random(5150); double phase = 0;
        for (int i = 0; i < a.Length; i++) {
            double mono = (a[i] + b[i]) * 0.5, carrier;
            if (fx.VocoderCarrier >= 0.5) { phase += 110.0 / SR; if (phase >= 1) phase -= 1; carrier = (phase * 2 - 1) * 0.6; }
            else carrier = rnd.NextDouble() * 2 - 1;
            double wet = 0, damp = 0.35;
            for (int k = 0; k < bands; k++) {
                double t = bands > 1 ? (double)k / (bands - 1) : 0.5;
                double fc = Math.Max(120, Math.Min(SR / 5.0, 200 * Math.Pow(40, t))), ff = 2 * Math.Sin(Math.PI * fc / SR);
                double hiA = mono - la[k] - damp * ba[k]; ba[k] += ff * hiA; la[k] += ff * ba[k];
                double hiC = carrier - lc[k] - damp * bc[k]; bc[k] += ff * hiC; lc[k] += ff * bc[k];
                env[k] += rel * (Math.Abs(ba[k]) - env[k]);
                wet += bc[k] * env[k];
            }
            wet *= 1.5;
            a[i] = (float)(a[i] * (1 - mix) + wet * mix); b[i] = (float)(b[i] * (1 - mix) + wet * mix);
        }
    }
    static void SpectralGateFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, bands = Math.Max(2, Math.Min(8, (int)Math.Round(fx.SpecGateBands)));
        double mix = Math.Max(0, Math.Min(1, fx.SpecGateMix)), thr = Math.Max(0.001, fx.SpecGateThresh);
        double[] wetA = new double[n], wetB = new double[n];
        for (int k = 0; k < bands; k++) {
            double t = bands > 1 ? (double)k / (bands - 1) : 0.5;
            double fc = Math.Max(80, Math.Min(SR / 5.0, 120 * Math.Pow(2, t * 6))), ff = 2 * Math.Sin(Math.PI * fc / SR), damp = 0.4;
            double loA = 0, bdA = 0, loB = 0, bdB = 0, env = 0, atk = 1 - Math.Exp(-1.0 / (0.003 * SR)), rel = 1 - Math.Exp(-1.0 / (0.08 * SR));
            for (int i = 0; i < n; i++) {
                double hiA = a[i] - loA - damp * bdA; bdA += ff * hiA; loA += ff * bdA;
                double hiB = b[i] - loB - damp * bdB; bdB += ff * hiB; loB += ff * bdB;
                double x = Math.Max(Math.Abs(bdA), Math.Abs(bdB)); env += (x > env ? atk : rel) * (x - env);
                double g = env > thr ? 1 : env / thr;
                wetA[i] += bdA * g; wetB[i] += bdB * g;
            }
        }
        double scale = 1.5 / bands;
        for (int i = 0; i < n; i++) { a[i] = (float)(a[i] * (1 - mix) + wetA[i] * scale * mix); b[i] = (float)(b[i] * (1 - mix) + wetB[i] * scale * mix); }
    }
    static void SpectralBlurFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, len = Math.Max(256, (int)(SR * 0.08)); float[] da = new float[len], db = new float[len]; int pos = 0;
        double mix = Math.Max(0, Math.Min(1, fx.SpecBlurMix)), amt = Math.Max(0, Math.Min(1, fx.SpecBlurAmount)), spread = Math.Max(0, Math.Min(1, fx.SpecBlurSpread));
        double lpA = 0, lpB = 0, c = Math.Exp(-2 * Math.PI * (2200 + 6000 * amt) / SR);
        for (int i = 0; i < n; i++) {
            double lfo = 0.5 + 0.5 * Math.Sin(2 * Math.PI * (0.15 + 0.9 * amt) * i / SR);
            int d1 = Math.Max(1, (int)((0.004 + 0.025 * amt * lfo) * SR));
            int d2 = Math.Max(1, (int)((0.007 + 0.03 * amt * (1 - lfo)) * SR));
            int r1 = pos - d1; if (r1 < 0) r1 += len; int r2 = pos - d2; if (r2 < 0) r2 += len;
            double yA = da[r1] * 0.6 + da[r2] * 0.4, yB = db[r2] * 0.6 + db[r1] * 0.4;
            da[pos] = (float)(a[i] + yB * spread * 0.25); db[pos] = (float)(b[i] + yA * spread * 0.25); pos++; if (pos >= len) pos = 0;
            lpA = (1 - c) * yA + c * lpA; lpB = (1 - c) * yB + c * lpB;
            a[i] = (float)(a[i] * (1 - mix) + (lpA * 0.7 + a[i] * 0.3) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (lpB * 0.7 + b[i] * 0.3) * mix);
        }
    }
    static void SpectralDelayFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, d = Math.Max(16, (int)(fx.SpecDelayTime * 0.001 * SR));
        int[] taps = { Math.Max(2, d / 2), d, Math.Min(n - 1, (int)(d * 1.5)) };
        float[][] da = new float[taps.Length][], db = new float[taps.Length][];
        for (int k = 0; k < taps.Length; k++) { da[k] = new float[Math.Max(4, taps[k])]; db[k] = new float[Math.Max(4, taps[k])]; }
        double mix = Math.Max(0, Math.Min(1, fx.SpecDelayMix)), fb = Math.Max(0, Math.Min(0.9, fx.SpecDelayFb)), spread = Math.Max(0, Math.Min(1, fx.SpecDelaySpread));
        double lpA = 0, lpB = 0, c = Math.Exp(-2 * Math.PI * 2400 / SR);
        for (int i = 0; i < n; i++) {
            double wa = 0, wb = 0;
            for (int k = 0; k < da.Length; k++) {
                int idx = i % da[k].Length;
                double xa = da[k][idx], xb = db[k][idx];
                da[k][idx] = (float)(a[i] + xa * fb); db[k][idx] = (float)(b[i] + xb * fb);
                double g = 1.0 / (k + 1);
                wa += xa * g; wb += xb * g;
            }
            lpA = (1 - c) * wa + c * lpA; lpB = (1 - c) * wb + c * lpB;
            a[i] = (float)(a[i] * (1 - mix) + (wa * (1 - spread) + lpA * spread) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (wb * (1 - spread) + lpB * spread) * mix);
        }
    }
    static void ConvReverbFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, taps = 24;
        double mix = Math.Max(0, Math.Min(1, fx.ConvMix)), size = Math.Max(0, Math.Min(1, fx.ConvSize)), damp = Math.Max(0, Math.Min(1, fx.ConvDamp));
        int pre = Math.Max(0, (int)(fx.ConvPre * 0.001 * SR));
        int maxDelay = Math.Max(64, pre + (int)((0.08 + 0.55 * size) * SR));
        float[] da = new float[maxDelay], db = new float[maxDelay]; int pos = 0;
        Random r = new Random(1234 + (int)(size * 1000));
        int[] tapDelay = new int[taps]; double[] gainA = new double[taps], gainB = new double[taps];
        for (int k = 0; k < taps; k++) {
            tapDelay[k] = Math.Max(1, (int)(pre + (0.02 + 0.9 * r.NextDouble()) * (0.08 + 0.55 * size) * SR));
            double dec = Math.Exp(-(tapDelay[k] / (double)SR) / (0.05 + 0.55 * size));
            gainA[k] = dec * (0.6 + 0.8 * r.NextDouble()) * (1 - damp * 0.5);
            gainB[k] = dec * (0.6 + 0.8 * r.NextDouble()) * (1 - damp * 0.5);
        }
        double c = Math.Exp(-2 * Math.PI * (600 + 5000 * (1 - damp)) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < n; i++) {
            double wa = 0, wb = 0;
            for (int k = 0; k < taps; k++) {
                int idx = pos - tapDelay[k]; if (idx < 0) idx += maxDelay;
                wa += da[idx] * gainA[k]; wb += db[idx] * gainB[k];
            }
            lpA = (1 - c) * wa + c * lpA; lpB = (1 - c) * wb + c * lpB;
            da[pos] = a[i]; db[pos] = b[i]; pos++; if (pos >= maxDelay) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + lpA * 0.35 * mix);
            b[i] = (float)(b[i] * (1 - mix) + lpB * 0.35 * mix);
        }
    }
    static void TankReverbFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, fx.TankMix)), decay = Math.Max(0, Math.Min(1, fx.TankDecay)), tone = Math.Max(0, Math.Min(1, fx.TankTone)), drip = Math.Max(0, Math.Min(1, fx.TankDrip));
        int[] lens = { Math.Max(64, (int)(SR * (0.021 + 0.008 * decay))), Math.Max(64, (int)(SR * (0.033 + 0.012 * decay))), Math.Max(64, (int)(SR * (0.047 + 0.015 * decay))) };
        float[][] da = new float[lens.Length][], db = new float[lens.Length][];
        for (int k = 0; k < lens.Length; k++) { da[k] = new float[lens[k]]; db[k] = new float[lens[k]]; }
        double fb = 0.55 + 0.4 * decay, c = Math.Exp(-2 * Math.PI * (900 + 6000 * tone) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < n; i++) {
            double wa = 0, wb = 0;
            for (int k = 0; k < lens.Length; k++) {
                int idx = i % lens[k]; double xa = da[k][idx], xb = db[k][idx];
                da[k][idx] = (float)(a[i] + xb * fb * 0.6); db[k][idx] = (float)(b[i] + xa * fb * 0.6);
                wa += xa; wb += xb;
            }
            double dripA = drip * 0.02 * Math.Sin(2 * Math.PI * 2400 * i / SR) * Math.Exp(-(i % 4096) / 800.0);
            lpA = (1 - c) * wa + c * lpA; lpB = (1 - c) * wb + c * lpB;
            a[i] = (float)(a[i] * (1 - mix) + (lpA / lens.Length + dripA) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (lpB / lens.Length + dripA) * mix);
        }
    }
    static void AutoTuneFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, len = Math.Max(1024, SR * 2); float[] da = new float[len], db = new float[len]; int w = 0; double rp = 0, ratio = 1, prevRatio = 1;
        double mix = Math.Max(0, Math.Min(1, fx.TuneMix)), speed = Math.Max(0, Math.Min(1, fx.TuneSpeed)), minHz = Math.Max(40, fx.TuneMinHz);
        int block = 2048;
        for (int start = 0; start < n; start += block) {
            int end = Math.Min(n, start + block), count = end - start;
            double best = 0; int bestLag = 0, maxLag = Math.Min(count - 1, (int)(SR / minHz)), minLag = Math.Max(2, (int)(SR / 1200));
            for (int lag = minLag; lag <= maxLag; lag++) {
                double s = 0;
                for (int i = start; i < end - lag; i++) s += a[i] * a[i + lag];
                if (s > best) { best = s; bestLag = lag; }
            }
            if (bestLag > 0 && best > 1e-6) {
                double hz = SR / (double)bestLag, midi = 69 + 12 * Math.Log(hz / 440.0, 2), target;
                int sc = (int)Math.Round(fx.TuneScale);
                if (sc == 0) target = Math.Round(midi);
                else {
                    int[] major = { 0, 2, 4, 5, 7, 9, 11 }, minor = { 0, 2, 3, 5, 7, 8, 10 };
                    int[] scale = sc == 2 ? minor : major; int pc = ((int)Math.Round(midi) % 12 + 12) % 12; int bestPc = scale[0], bd = 99;
                    foreach (int s in scale) { int d = Math.Abs(s - pc); if (d < bd) { bd = d; bestPc = s; } }
                    int baseNote = (int)Math.Floor((midi - pc) / 12.0) * 12 + bestPc; target = baseNote;
                }
                ratio = Math.Pow(2, (target - midi) / 12.0);
            }
            double sm = 0.02 + 0.9 * speed;
            for (int i = start; i < end; i++) {
                prevRatio += (ratio - prevRatio) * sm;
                int ri = (int)rp; if (ri >= len) ri %= len; double fr = rp - Math.Floor(rp); int ri2 = (ri + 1) % len;
                double ya = da[ri] * (1 - fr) + da[ri2] * fr, yb = db[ri] * (1 - fr) + db[ri2] * fr;
                da[w] = a[i]; db[w] = b[i]; w++; if (w >= len) w = 0;
                rp += prevRatio; while (rp >= len) rp -= len;
                a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
            }
        }
    }
    static void OctaveUpFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, len = Math.Max(1024, SR * 2); float[] da = new float[len], db = new float[len]; int w = 0; double rp = 0;
        double mix = Math.Max(0, Math.Min(1, fx.OctUpMix)), tone = Math.Max(0, Math.Min(1, fx.OctUpTone));
        double c = Math.Exp(-2 * Math.PI * (1200 + 9000 * tone) / SR), lpA = 0, lpB = 0;
        for (int i = 0; i < n; i++) {
            int ri = (int)rp; if (ri >= len) ri %= len; double fr = rp - Math.Floor(rp); int ri2 = (ri + 1) % len;
            double ya = da[ri] * (1 - fr) + da[ri2] * fr, yb = db[ri] * (1 - fr) + db[ri2] * fr;
            da[w] = a[i]; db[w] = b[i]; w++; if (w >= len) w = 0;
            rp += 2.0; while (rp >= len) rp -= len;
            lpA = (1 - c) * ya + c * lpA; lpB = (1 - c) * yb + c * lpB;
            a[i] = (float)(a[i] * (1 - mix) + lpA * mix); b[i] = (float)(b[i] * (1 - mix) + lpB * mix);
        }
    }
    static void ChordGenFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, len = Math.Max(1024, SR * 2);
        int ct = Math.Max(0, Math.Min(3, (int)Math.Round(fx.ChordType)));
        int[] intervals = ct == 0 ? new[] { 4, 7 } : ct == 1 ? new[] { 3, 7 } : ct == 2 ? new[] { 7, 7 } : new[] { 5, 7 };
        float[][] da = new float[2][], db = new float[2][]; int[] w = new int[2]; double[] rp = new double[2];
        for (int k = 0; k < 2; k++) { da[k] = new float[len]; db[k] = new float[len]; }
        double mix = Math.Max(0, Math.Min(1, fx.ChordMix)), spread = Math.Max(0, Math.Min(1, fx.ChordSpread));
        for (int i = 0; i < n; i++) {
            double wa = 0, wb = 0;
            for (int k = 0; k < 2; k++) {
                int ri = (int)rp[k]; if (ri >= len) ri %= len; double fr = rp[k] - Math.Floor(rp[k]); int ri2 = (ri + 1) % len;
                double ya = da[k][ri] * (1 - fr) + da[k][ri2] * fr, yb = db[k][ri] * (1 - fr) + db[k][ri2] * fr;
                da[k][w[k]] = a[i]; db[k][w[k]] = b[i]; w[k]++; if (w[k] >= len) w[k] = 0;
                double ratio = Math.Pow(2, intervals[k] / 12.0);
                rp[k] += ratio; while (rp[k] >= len) rp[k] -= len;
                wa += (k == 0 ? ya * (1 - spread * 0.5) : ya * spread); wb += (k == 0 ? yb * (1 - spread * 0.5) : yb * spread);
            }
            a[i] = (float)(a[i] * (1 - mix) + wa * 0.55 * mix); b[i] = (float)(b[i] * (1 - mix) + wb * 0.55 * mix);
        }
    }
    static void UpwardCompFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, fx.UpCompMix)), thr = Math.Pow(10, fx.UpCompThresh / 20.0), ratio = Math.Max(1, fx.UpCompRatio), maxDb = Math.Max(0, fx.UpCompRange);
        double atk = 1 - Math.Exp(-1.0 / (0.005 * SR)), rel = 1 - Math.Exp(-1.0 / (0.1 * SR)), env = 0, gain = 1;
        for (int i = 0; i < n; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            double target = env > 1e-6 && env < thr ? Math.Min(Math.Pow(10, maxDb / 20.0), Math.Pow(thr / env, 1 - 1 / ratio)) : 1;
            gain += (target - gain) * (target > gain ? atk : rel);
            double g = 1 - mix + mix * gain; a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void TransientGateFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, fx.TranGateMix)), thr = Math.Pow(10, fx.TranGateThresh / 20.0);
        int hold = Math.Max(1, (int)(fx.TranGateHold * 0.001 * SR)); double rel = 1 - Math.Exp(-1.0 / (Math.Max(10, fx.TranGateRelease) * 0.001 * SR));
        double atk = 1 - Math.Exp(-1.0 / (0.001 * SR)), env = 0, gate = 0; int held = 0;
        for (int i = 0; i < n; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env += (x > env ? atk : rel) * (x - env);
            if (env > thr) { gate = 1; held = hold; } else if (held > 0) held--; else gate += (0 - gate) * rel;
            double g = 1 - mix + mix * gate; a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void StereoShuffleFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, fx.ShuffleMix)), loW = Math.Max(0, Math.Min(2, fx.ShuffleLow)), hiW = Math.Max(0, Math.Min(2, fx.ShuffleHigh));
        double c = Math.Exp(-2 * Math.PI * Math.Max(40, fx.ShuffleFreq) / SR), lpA = 0;
        for (int i = 0; i < n; i++) {
            double mid = (a[i] + b[i]) * 0.5, side = (a[i] - b[i]) * 0.5;
            lpA = (1 - c) * side + c * lpA;
            double lowSide = lpA, highSide = side - lowSide;
            double outSide = lowSide * loW + highSide * hiW;
            a[i] = (float)(a[i] * (1 - mix) + (mid + outSide) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (mid - outSide) * mix);
        }
    }
    static void MonoMakerFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, fx.MonoMix)), width = Math.Max(0, Math.Min(2, fx.MonoWidth));
        double c = Math.Exp(-2 * Math.PI * Math.Max(20, fx.MonoFreq) / SR), lpA = 0;
        for (int i = 0; i < n; i++) {
            double mid = (a[i] + b[i]) * 0.5, side = (a[i] - b[i]) * 0.5;
            lpA = (1 - c) * side + c * lpA;
            double lowSide = lpA, highSide = side - lowSide;
            double outSide = highSide * width;
            a[i] = (float)(a[i] * (1 - mix) + (mid + outSide) * mix);
            b[i] = (float)(b[i] * (1 - mix) + (mid - outSide) * mix);
        }
    }
    static void PhaseAlignFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, fx.PhaseMix)), amount = Math.Max(0, Math.Min(1, fx.PhaseAmount));
        double t = Math.Tan(Math.PI * Math.Max(80, Math.Min(8000, fx.PhaseFreq)) / SR), c = (t - 1) / (t + 1), za1 = 0, zb1 = 0, za2 = 0, zb2 = 0;
        for (int i = 0; i < n; i++) {
            double xa = a[i], xb = b[i];
            double ya1 = c * xa + za1; za1 = xa - c * ya1; double ya2 = c * ya1 + za2; za2 = ya1 - c * ya2;
            double yb1 = c * xb + zb1; zb1 = xb - c * yb1; double yb2 = c * yb1 + zb2; zb2 = yb1 - c * yb2;
            double oa = xa * (1 - amount) + ya2 * amount, ob = xb * (1 - amount) + yb2 * amount;
            a[i] = (float)(xa * (1 - mix) + oa * mix); b[i] = (float)(xb * (1 - mix) + ob * mix);
        }
    }
    static void TapeLoopFx(Fx fx, float[] a, float[] b) {
        int n = a.Length, len = Math.Max(64, (int)(fx.LoopLength * 0.001 * SR)); float[] da = new float[len], db = new float[len]; int w = 0; double rp = 0;
        double mix = Math.Max(0, Math.Min(1, fx.LoopMix)), speed = Math.Max(0.25, Math.Min(2, fx.LoopSpeed)), fb = Math.Max(0, Math.Min(0.95, fx.LoopFb));
        for (int i = 0; i < n; i++) {
            int ri = (int)rp; if (ri >= len) ri %= len; double fr = rp - Math.Floor(rp); int ri2 = (ri + 1) % len;
            double ya = da[ri] * (1 - fr) + da[ri2] * fr, yb = db[ri] * (1 - fr) + db[ri2] * fr;
            da[w] = (float)(a[i] + ya * fb); db[w] = (float)(b[i] + yb * fb); w++; if (w >= len) w = 0;
            rp += speed; while (rp >= len) rp -= len;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void StutterGateFx(Fx fx, float[] a, float[] b) {
        int n = a.Length; double mix = Math.Max(0, Math.Min(1, fx.StutterGateMix)), chance = Math.Max(0, Math.Min(1, fx.StutterGateChance));
        int div = Math.Max(2, Math.Min(16, (int)Math.Round(fx.StutterGateDiv)));
        int hold = Math.Max(64, (int)(SR / Math.Max(0.5, fx.StutterGateRate) / div));
        Random r = new Random(901); bool open = true; int remain = hold;
        for (int i = 0; i < n; i++) {
            if (remain <= 0) { open = r.NextDouble() < chance; remain = hold; }
            remain--; double g = 1 - mix + mix * (open ? 1 : 0);
            a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void GateFx(Fx fx, float[] a, float[] b) {
        double thr = Math.Pow(10, fx.GateThreshold / 20.0), amt = Math.Max(0, Math.Min(1, fx.GateAmount));
        double atk = 1 - Math.Exp(-1.0 / (Math.Max(0.1, fx.GateAttack) * 0.001 * SR));
        double rel = 1 - Math.Exp(-1.0 / (Math.Max(10, fx.GateRelease) * 0.001 * SR));
        double env = 0;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i]));
            env += (x > env ? atk : rel) * (x - env);
            double g = env >= thr ? 1 : 1 - amt;
            a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void SaturatorFx(Fx fx, float[] a, float[] b) {
        double drive = 1 + fx.Saturator * 12, mix = Math.Max(0, Math.Min(1, fx.SatMix)), tone = Math.Max(0, Math.Min(1, fx.SatTone));
        double lp = 0;
        for (int i = 0; i < a.Length; i++) {
            double xa = Math.Tanh((a[i] + fx.SatBias) * drive) * 0.75;
            double xb = Math.Tanh((b[i] + fx.SatBias) * drive) * 0.75;
            lp += (xa - lp) * (0.02 + 0.6 * (1 - tone));
            a[i] = (float)(a[i] * (1 - mix) + lp * mix);
            b[i] = (float)(b[i] * (1 - mix) + xb * mix);
        }
    }
    static void ExciterFx(Fx fx, float[] a, float[] b) {
        double fc = Math.Max(500, Math.Min(SR / 4.0, fx.ExciterFreq)), coef = Math.Exp(-2 * Math.PI * fc / SR), mix = Math.Max(0, Math.Min(1, fx.ExciterMix));
        double la = 0, lb = 0;
        for (int i = 0; i < a.Length; i++) {
            la = (1 - coef) * a[i] + coef * la; lb = (1 - coef) * b[i] + coef * lb;
            double ha = Math.Tanh((a[i] - la) * 4) * fx.Exciter, hb = Math.Tanh((b[i] - lb) * 4) * fx.Exciter;
            a[i] = (float)(a[i] + ha * mix); b[i] = (float)(b[i] + hb * mix);
        }
    }
    static void RectifyFx(Fx fx, float[] a, float[] b) {
        double mix = Math.Max(0, Math.Min(1, fx.RectifyMix)), drive = 1 + fx.RectifyDrive * 8;
        for (int i = 0; i < a.Length; i++) {
            double xa = a[i] * drive, xb = b[i] * drive;
            double ya = fx.RectifyHalf ? Math.Max(0, xa) : Math.Abs(xa);
            double yb = fx.RectifyHalf ? Math.Max(0, xb) : Math.Abs(xb);
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void TransientFx(Fx fx, float[] a, float[] b) {
        double af = 1 - Math.Exp(-1.0 / (0.0015 * SR)), aslow = 1 - Math.Exp(-1.0 / (0.08 * SR));
        double atk = fx.TransientAttack, sus = fx.TransientSustain, sens = Math.Max(0, Math.Min(1, fx.TransientSensitivity));
        double fast = 0, slow = 0;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i]));
            fast += (x - fast) * (x > fast ? af : aslow); slow += (x - slow) * aslow;
            double g = 1 + atk * Math.Max(0, fast - slow) * (1 + sens * 8) - sus * slow * (1 + sens * 4);
            g = Math.Max(0, Math.Min(4, g));
            a[i] = (float)(a[i] * g); b[i] = (float)(b[i] * g);
        }
    }
    static void TremoloFx(Fx fx, float[] a, float[] b) {
        double rate = Math.Max(0.05, fx.TremoloRate), depth = Math.Max(0, Math.Min(1, fx.TremoloDepth)), mix = Math.Max(0, Math.Min(1, fx.TremoloMix));
        for (int i = 0; i < a.Length; i++) { double lfo = 0.5 + 0.5 * Math.Sin(2 * Math.PI * rate * i / SR); double g = 1 - depth * (1 - lfo); a[i] = (float)(a[i] * (1 - mix + mix * g)); b[i] = (float)(b[i] * (1 - mix + mix * g)); }
    }
    static void AutoPanFx(Fx fx, float[] a, float[] b) {
        double rate = Math.Max(0.02, fx.AutoPanRate), depth = Math.Max(0, Math.Min(1, fx.AutoPanDepth)), mix = Math.Max(0, Math.Min(1, fx.AutoPanMix));
        for (int i = 0; i < a.Length; i++) {
            double p = depth * Math.Sin(2 * Math.PI * rate * i / SR);
            double gl = Math.Min(1, 1 - p), gr = Math.Min(1, 1 + p);
            a[i] = (float)(a[i] * (1 - mix + mix * gl)); b[i] = (float)(b[i] * (1 - mix + mix * gr));
        }
    }
    static void VibratoFx(Fx fx, float[] a, float[] b) {
        int len = (int)(SR * 0.03) + 2; float[] dl = new float[len], dr = new float[len]; int pos = 0;
        double rate = Math.Max(0.05, fx.VibratoRate), depth = Math.Max(0, Math.Min(1, fx.VibratoDepth)), mix = Math.Max(0, Math.Min(1, fx.VibratoMix));
        for (int i = 0; i < a.Length; i++) {
            double d = (0.004 + 0.006 * depth * (0.5 + 0.5 * Math.Sin(2 * Math.PI * rate * i / SR))) * SR;
            int di = Math.Max(1, Math.Min(len - 1, (int)d)); int rp = pos - di; if (rp < 0) rp += len;
            double ya = dl[rp], yb = dr[rp]; dl[pos] = a[i]; dr[pos] = b[i]; pos++; if (pos >= len) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void AutoWahFx(Fx fx, float[] a, float[] b) {
        double rate = Math.Max(0.05, fx.AutoWahRate), depth = Math.Max(0, Math.Min(1, fx.AutoWahDepth)), q = Math.Max(0.05, 1 - Math.Min(0.95, fx.AutoWahResonance)), mix = Math.Max(0, Math.Min(1, fx.AutoWahMix));
        double loA = 0, bdA = 0, loB = 0, bdB = 0;
        for (int i = 0; i < a.Length; i++) {
            double fc = 250 + depth * 3500 * (0.5 + 0.5 * Math.Sin(2 * Math.PI * rate * i / SR)); fc = Math.Max(80, Math.Min(SR / 5.0, fc));
            double ff = 2 * Math.Sin(Math.PI * fc / SR);
            double hiA = a[i] - loA - q * bdA; bdA += ff * hiA; loA += ff * bdA;
            double hiB = b[i] - loB - q * bdB; bdB += ff * hiB; loB += ff * bdB;
            a[i] = (float)(a[i] * (1 - mix) + bdA * mix); b[i] = (float)(b[i] * (1 - mix) + bdB * mix);
        }
    }
    static void CombFx(Fx fx, float[] a, float[] b) {
        int d = Math.Max(2, (int)(SR / Math.Max(20, Math.Min(2000, fx.CombFreq))));
        float[] dl = new float[d], dr = new float[d]; int pos = 0;
        double fb = Math.Max(0, Math.Min(0.95, fx.CombFeedback)), mix = Math.Max(0, Math.Min(1, fx.CombMix));
        for (int i = 0; i < a.Length; i++) {
            double ya = dl[pos], yb = dr[pos];
            dl[pos] = (float)(a[i] + ya * fb); dr[pos] = (float)(b[i] + yb * fb); pos++; if (pos >= d) pos = 0;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void SpringFx(Fx fx, float[] a, float[] b) {
        int d1 = (int)(SR * 0.031), d2 = (int)(SR * 0.047), d3 = (int)(SR * 0.071);
        float[] b1 = new float[d1], b2 = new float[d2], b3 = new float[d3]; int p1 = 0, p2 = 0, p3 = 0;
        double decay = 0.3 + 0.6 * Math.Max(0, Math.Min(1, fx.SpringDecay)), tone = Math.Max(0, Math.Min(1, fx.SpringTone)), mix = Math.Max(0, Math.Min(1, fx.SpringMix)), lp = 0;
        for (int i = 0; i < a.Length; i++) {
            double inx = (a[i] + b[i]) * 0.5, y1 = b1[p1], y2 = b2[p2], y3 = b3[p3];
            b1[p1] = (float)(inx + (y2 + y3) * 0.5 * decay); b2[p2] = (float)(inx + (y1 + y3) * 0.5 * decay); b3[p3] = (float)(inx + (y1 + y2) * 0.5 * decay);
            p1++; if (p1 >= d1) p1 = 0; p2++; if (p2 >= d2) p2 = 0; p3++; if (p3 >= d3) p3 = 0;
            double wet = (y1 + y2 + y3) / 3.0; lp += (wet - lp) * (0.05 + 0.75 * tone);
            a[i] = (float)(a[i] * (1 - mix) + lp * mix); b[i] = (float)(b[i] * (1 - mix) + lp * mix);
        }
    }
    static void ShimmerFx(Fx fx, float[] a, float[] b) {
        int d = (int)(SR * 0.12); float[] dl = new float[d], dr = new float[d]; int pos = 0;
        double ratio = Math.Pow(2, fx.ShimmerPitch / 12.0), decay = 0.3 + 0.6 * Math.Max(0, Math.Min(1, fx.ShimmerDecay)), mix = Math.Max(0, Math.Min(1, fx.ShimmerMix));
        double rp = 0;
        for (int i = 0; i < a.Length; i++) {
            int ri = (int)rp; if (ri >= d) ri %= d; double fr = rp - Math.Floor(rp);
            int ri2 = (ri + 1) % d;
            double ya = dl[ri] * (1 - fr) + dl[ri2] * fr, yb = dr[ri] * (1 - fr) + dr[ri2] * fr;
            dl[pos] = (float)(a[i] + ya * decay); dr[pos] = (float)(b[i] + yb * decay); pos++; if (pos >= d) pos = 0;
            rp += ratio; if (rp >= d) rp -= d;
            a[i] = (float)(a[i] * (1 - mix) + ya * mix); b[i] = (float)(b[i] * (1 - mix) + yb * mix);
        }
    }
    static void HaasFx(Fx fx, float[] a, float[] b) {
        int d = Math.Max(1, (int)(Math.Max(1, fx.HaasDelay) * 0.001 * SR)); float[] db = new float[d]; int pos = 0; double mix = Math.Max(0, Math.Min(1, fx.HaasMix));
        for (int i = 0; i < a.Length; i++) { double y = db[pos]; db[pos] = b[i]; pos++; if (pos >= d) pos = 0; b[i] = (float)(b[i] * (1 - mix) + y * mix); }
    }
    static void LimiterFx(Fx fx, float[] a, float[] b) {
        double thr = Math.Pow(10, fx.LimiterThreshold / 20.0), ceil = Math.Pow(10, fx.LimiterCeiling / 20.0);
        double rel = 1 - Math.Exp(-1.0 / (Math.Max(10, fx.LimiterRelease) * 0.001 * SR)); double env = 0;
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env = Math.Max(x, env * (1 - rel) + x * rel);
            double g = env > thr ? thr / env : 1; g = Math.Min(1, g);
            double ya = a[i] * g, yb = b[i] * g;
            a[i] = (float)Math.Max(-ceil, Math.Min(ceil, ya)); b[i] = (float)Math.Max(-ceil, Math.Min(ceil, yb));
        }
    }
    static void Comp(Fx fx, float[] a, float[] b) {
        double env = 0, atk = Math.Exp(-1 / (fx.CompAttack / 1000.0 * SR)), rel = Math.Exp(-1 / (fx.CompRelease / 1000.0 * SR)); double thr = Math.Pow(10, fx.CompThresh / 20.0), ratio = fx.CompRatio;
        double makeup = Math.Pow(10, fx.CompMakeup / 20.0);
        for (int i = 0; i < a.Length; i++) {
            double x = Math.Max(Math.Abs(a[i]), Math.Abs(b[i])); env = Math.Max(x, env * rel + (x - env) * (x > env ? 1 - atk : 1 - rel));
            double gr = makeup; if (env > thr) { double db = 20 * Math.Log10(env / thr); gr = Math.Pow(10, (-db * (1 - 1 / ratio)) / 20.0); gr = Math.Min(1, gr) * makeup; }
            a[i] = (float)(a[i] * gr); b[i] = (float)(b[i] * gr);
        }
    }
    static void EqFx(Fx fx, float[] a, float[] b) {
        Biquad lsa = Biquad.LowShelf(SR, 200, fx.LowShelf, 0.7071), hsa = Biquad.HighShelf(SR, 8000, fx.HighShelf, 0.7071), mpa = Biquad.Peak(SR, 1200, fx.MidPeak, 1.0);
        Biquad lsb = Biquad.LowShelf(SR, 200, fx.LowShelf, 0.7071), hsb = Biquad.HighShelf(SR, 8000, fx.HighShelf, 0.7071), mpb = Biquad.Peak(SR, 1200, fx.MidPeak, 1.0);
        for (int i = 0; i < a.Length; i++) { a[i] = (float)mpa.Tick(hsa.Tick(lsa.Tick(a[i]))); b[i] = (float)mpb.Tick(hsb.Tick(lsb.Tick(b[i]))); }
    }
    static void WidthFx(Fx fx, float[] a, float[] b) {
        for (int i = 0; i < a.Length; i++) { double mid = (a[i] + b[i]) * 0.5, side = (a[i] - b[i]) * 0.5 * fx.Width; a[i] = (float)(mid + side); b[i] = (float)(mid - side); }
    }

    static void Svf(float[] x, Fx fx) {
        double lo = 0, bd = 0, q = Math.Max(0.05, 1 - Math.Min(0.95, fx.Resonance));
        for (int i = 0; i < x.Length; i++) {
            double fc = fx.Cutoff;
            if (fx.FilterLfoRate > 0) fc *= Math.Pow(2, fx.FilterLfoDepth * 3 * Math.Sin(2 * Math.PI * fx.FilterLfoRate * i / SR));
            fc = Math.Max(30, Math.Min(fc, SR / 6.0));
            double ff = 2 * Math.Sin(Math.PI * fc / SR);
            lo += ff * bd; double hi = x[i] - lo - q * bd; bd += ff * hi;
            x[i] = (float)(fx.Filter == FilterMode.lowpass ? lo : (fx.Filter == FilterMode.highpass ? hi : bd));
        }
    }

    static void Chorus(Fx fx, float[] a, float[] b) {
        float[] sa = (float[])a.Clone(), sb = (float[])b.Clone();
        double mix = fx.Chorus, w = 2 * Math.PI * fx.ChorusRate / SR;
        for (int i = 0; i < a.Length; i++) {
            double da = (0.010 + 0.007 * fx.ChorusDepth * Math.Sin(w * i)) * SR, db = (0.010 + 0.007 * fx.ChorusDepth * Math.Sin(w * i + 1.5708)) * SR;
            a[i] = (float)(sa[i] + mix * Tap(sa, i - da)); b[i] = (float)(sb[i] + mix * Tap(sb, i - db));
        }
    }
    static double Tap(float[] x, double pos) {
        if (pos < 0) return 0;
        int j = (int)pos; double fr = pos - j;
        return j + 1 < x.Length ? x[j] * (1 - fr) + x[j + 1] * fr : 0;
    }

    static void Delay(Fx fx, float[] a, float[] b, double spb) {
        int d = (int)Math.Round(fx.DelayBeats * spb * SR), n = a.Length;
        if (d < 1 || d >= n) return;
        float[] da = (float[])a.Clone(), db = (float[])b.Clone(), wl = new float[n], wr = new float[n];
        float fb = (float)fx.DelayFeedback;
        for (int i = d; i < n; i++) { wl[i] = db[i - d] + fb * wr[i - d]; wr[i] = da[i - d] + fb * wl[i - d]; }
        float m = (float)fx.DelayMix;
        for (int i = 0; i < n; i++) { a[i] = da[i] + m * wl[i]; b[i] = db[i] + m * wr[i]; }
    }

    static readonly int[] CombLen = new int[] { 1116, 1188, 1277, 1356, 1422, 1491 };
    static readonly int[] ApLen = new int[] { 556, 441, 341, 225 };
    static void Reverb(Fx fx, float[] a, float[] b) {
        int n = a.Length; float[] mono = new float[n];
        for (int i = 0; i < n; i++) mono[i] = (a[i] + b[i]) * 0.5f * 0.2f;
        double fb = 0.72 + 0.26 * fx.ReverbSize, damp = 0.25, mix = fx.ReverbMix;
        for (int ch = 0; ch < 2; ch++) {
            float[] wet = new float[n]; float[] dst = ch == 0 ? a : b;
            for (int c = 0; c < CombLen.Length; c++) {
                float[] buf = new float[CombLen[c] + ch * 23]; int idx = 0; double store = 0;
                for (int i = 0; i < n; i++) {
                    float outv = buf[idx]; store = outv * (1 - damp) + store * damp;
                    buf[idx] = (float)(mono[i] + store * fb); if (++idx >= buf.Length) idx = 0;
                    wet[i] += outv;
                }
            }
            for (int k = 0; k < ApLen.Length; k++) {
                float[] buf = new float[ApLen[k] + ch * 11]; int idx = 0;
                for (int i = 0; i < n; i++) { float bo = buf[idx]; float w = wet[i]; wet[i] = -w + bo; buf[idx] = w + bo * 0.5f; if (++idx >= buf.Length) idx = 0; }
            }
            for (int i = 0; i < n; i++) dst[i] += (float)(wet[i] * mix);
        }
    }
}
