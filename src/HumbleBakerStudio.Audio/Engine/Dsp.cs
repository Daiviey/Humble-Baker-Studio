namespace HumbleBakerStudio.Audio;


public static class Dsp {
    public const int SR = 44100;

    public static short[] Render(Project p, bool loop) { return Render(p, loop, true); }

    // Mixes the current pattern (loop) or the whole arrangement (song) to float stereo, before mastering.
    public static void Mix(Project p, bool song, bool loop, out float[] L, out float[] R) {
        if (song && p.Song.Count > 0) RenderStereo(Arranger.Flatten(p), false, out L, out R);
        else RenderStereo(p, loop, out L, out R);
    }

    public static short[] ToPcm(float[] L, float[] R, bool mastered) {
        int N = L.Length; short[] pcm = new short[N * 2];
        for (int i = 0; i < N; i++) {
            double a = mastered ? L[i] : Math.Tanh(L[i]), b = mastered ? R[i] : Math.Tanh(R[i]), sc = mastered ? 32767 : 30000;
            pcm[2 * i] = (short)Math.Max(-32767, Math.Min(32767, Math.Round(a * sc)));
            pcm[2 * i + 1] = (short)Math.Max(-32767, Math.Min(32767, Math.Round(b * sc)));
        }
        return pcm;
    }

    public static short[] Render(Project p, bool loop, bool master) {
        float[] L, R; RenderStereo(p, loop, out L, out R);
        bool m = master && p.Master != null && p.Master.Enabled;
        if (m) Mastering.Process(L, R, p.Master);
        return ToPcm(L, R, m);
    }

    // Per-block render: synthesizes the loop (or song) and returns only the frames in [fromFrame, toFrame) as interleaved stereo PCM.
    // When loop is true the range wraps (a looping buffer is periodic), so callers can ask for [F, F+N) to get the seamless window starting at F.
    public static short[] Render(Project p, bool loop, bool master, int fromFrame, int toFrame) {
        short[] full = Render(p, loop, master);
        int n = full.Length / 2;
        int f = Math.Max(0, Math.Min(fromFrame, n));
        int span = Math.Max(0, toFrame - fromFrame);
        short[] outPcm = new short[Math.Max(0, span * 2)];
        for (int i = 0; i < span; i++) {
            int src = f + i; if (loop) src %= n;
            if (src >= 0 && src * 2 + 2 <= full.Length) { Array.Copy(full, src * 2, outPcm, i * 2, 2); }
        }
        return outPcm;
    }

    public static short[] RenderSong(Project p, bool master) {
        float[] L, R; Mix(p, true, false, out L, out R);
        bool m = master && p.Master != null && p.Master.Enabled;
        if (m) Mastering.Process(L, R, p.Master);
        return ToPcm(L, R, m);
    }

    public static void RenderStereo(Project p, bool loop, out float[] outL, out float[] outR) {
        double spb = 60.0 / p.Bpm;
        int loopN = (int)Math.Round(p.LoopBeats * spb * SR);
        int N = loop ? loopN : loopN + SR * 2;
        float[] L = new float[N], R = new float[N];
        Random rnd = new Random(1234);
        List<float[]> allL = new List<float[]>(), allR = new List<float[]>(); List<Track> allT = new List<Track>(); List<bool> ghost = new List<bool>();
        bool anySolo = false; foreach (Track solot in p.Tracks) if (solot.Solo) { anySolo = true; break; }
        // When the patch graph drives the mix, the graph's own FX modules process the audio.
        // Applying the track FX here as well would run each effect twice (once dry, once in the graph).
        bool patchOn = PatchRender.Active(p);
        if (patchOn) PatchRender.BindFxNodes(p);
        foreach (Track t in p.Tracks) {
            bool isSrc = MixEngine.IsSource(p, t);
            bool offBySolo = anySolo && !t.Solo && !isSrc;
            if (t.Mute && !isSrc) continue;
            if (offBySolo) continue;
            double pl = Math.Cos((t.Pan + 1) * Math.PI / 4), pr = Math.Sin((t.Pan + 1) * Math.PI / 4);
            float[] TL = new float[N], TR = new float[N];
            Dictionary<string, float[]> cache = new Dictionary<string, float[]>();
            bool isPatch = (t.Synth ?? "").ToLowerInvariant() == "patch" && t.Patch != null;
            bool isDrums = (t.Synth ?? "").ToLowerInvariant() == "drums";
            bool pctx = isPatch && t.Patch.NeedsContext();
            List<AutoLane> planes = null;
            if (isPatch && t.Auto != null) foreach (AutoLane al in t.Auto) if (al.Param.StartsWith("p:") && al.Points.Count > 0) { if (planes == null) planes = new List<AutoLane>(); planes.Add(al); }
            int evi = 0;
            foreach (Ev e in Engine.Events(p, t)) {
                evi++;
                int s0 = (int)Math.Round(e.Start * spb * SR);
                float amp = (float)(t.Vol * e.Vel / 127.0 * 0.5);
                string key = e.Pitch + "|" + Math.Round(e.Len * spb, 3).ToString(CultureInfo.InvariantCulture);
                if (isDrums && DrumSamples.Enabled) key += "|v" + DrumSamples.LayerFor(e.Vel);
                float[] buf;
                if (isPatch && (pctx || planes != null)) {
                    int variant = t.Patch.Variation > 0 ? evi % 4 : 0, velB = pctx ? Math.Min(127, (e.Vel / 8) * 8 + 4) : 100; string psig = "";
                    Patch pv = planes != null ? PatchAuto.Resolve(t.Patch, planes, e.Start, out psig) : null;
                    key += "|v" + velB + "|r" + variant + "|" + psig;
                    if (!cache.TryGetValue(key, out buf)) { buf = PatchSynth.Render(pv ?? t.Patch, e.Pitch, e.Len * spb, velB, variant); cache[key] = buf; }
                } else if (!cache.TryGetValue(key, out buf)) {
                    buf = NoteBuffer(t, e.Pitch, e.Len * spb, rnd, e.Vel);
                    cache[key] = buf;
                }
                for (int i = 0; i < buf.Length; i++) {
                    int idx = s0 + i;
                    if (idx >= N) { if (loop) idx %= N; else break; }
                    float v = buf[i] * amp;
                    TL[idx] += (float)(v * pl); TR[idx] += (float)(v * pr);
                }
            }
            if (t.Fx != null && t.Fx.Active && (!patchOn || !PatchRender.HasFxNode(p, t.Fx))) FxProc.Process(t.Fx, TL, TR, loop, spb);
            if (t.Auto != null && t.Auto.Count > 0 && (!patchOn || !PatchRender.HasFxNode(p, t.Fx))) AutoFx.Apply(t, TL, TR, spb);
            allT.Add(t); allL.Add(TL); allR.Add(TR); ghost.Add(t.Mute || offBySolo);
        }
        // Per-track compressor sidechain + makeup gain (duck from a named source track)
        for (int k = 0; k < allT.Count; k++) {
            Track t = allT[k]; if (t == null || t.Fx == null) continue;
            Fx fx = t.Fx;
            string srcName = fx.SidechainSource;
            if (!String.IsNullOrEmpty(srcName) && fx.SidechainMix > 0) {
                int si = -1;
                for (int q = 0; q < allT.Count; q++) if (allT[q] != null && allT[q].Name == srcName) { si = q; break; }
                if (si >= 0 && si != k) {
                    float[] sl = allL[si], sr = allR[si]; float[] oL = allL[k], oR = allR[k];
                    double aCoef = Math.Exp(-1 / Math.Max(1, fx.SidechainAttack / 1000.0 * SR)), rCoef = Math.Exp(-1 / Math.Max(1, fx.SidechainRelease / 1000.0 * SR));
                    double env = 0;
                    for (int i = 0; i < N; i++) {
                        double x = Math.Max(Math.Abs(sl[i]), Math.Abs(sr[i])); env = Math.Max(x, env * rCoef + (x - env) * (x > env ? 1 - aCoef : 1 - rCoef));
                        double duck = 1 - Math.Min(1, Math.Max(0, env * 2)) * fx.SidechainMix;
                        oL[i] = (float)(oL[i] * duck); oR[i] = (float)(oR[i] * duck);
                    }
                }
            }
            if (fx.CompMakeup != 0) { float g = (float)Math.Pow(10, fx.CompMakeup / 20.0); float[] oL2 = allL[k], oR2 = allR[k]; for (int i = 0; i < N; i++) { oL2[i] *= g; oR2[i] *= g; } }
        }
        MixEngine.Apply(p, allT, allL, allR, ghost, loop, spb);
        if (patchOn) { PatchRender.Run(p, allT, allL, allR, ghost, loop, spb, L, R, N); }
        else {
        float[][] busL = new float[5][], busR = new float[5][];
        for (int b = 1; b <= 4; b++) { busL[b] = new float[N]; busR[b] = new float[N]; }
        for (int k = 0; k < allT.Count; k++) {
            if (ghost[k]) continue;
            float[] TL = allL[k], TR = allR[k];
            int b = allT[k].Bus;
            float[] dL = (b >= 1 && b <= 4) ? busL[b] : L, dR = (b >= 1 && b <= 4) ? busR[b] : R;
            for (int i = 0; i < N; i++) { dL[i] += TL[i]; dR[i] += TR[i]; }
        }
        for (int b = 1; b <= 4; b++) {
            Bus bus = (p.Buses != null && b <= p.Buses.Count) ? p.Buses[b - 1] : null;
            if (bus != null && bus.Enabled && bus.Fx != null) { if (bus.Auto != null && bus.Auto.Count > 0) AutoFx.ApplyLanes(bus.Auto, bus.Fx, busL[b], busR[b], spb); if (bus.Fx.Active) FxProc.Process(bus.Fx, busL[b], busR[b], loop, spb); }
            for (int i = 0; i < N; i++) { L[i] += busL[b][i]; R[i] += busR[b][i]; }
        }
        }
        int d = (int)Math.Round(p.DelayBeats * spb * SR);
        if (p.DelayMix > 0.001 && d > 0 && d < N) {
            float[] wl = new float[N], wr = new float[N];
            int passes = loop ? 4 : 1;
            for (int pass = 0; pass < passes; pass++) {
                for (int i = 0; i < N; i++) {
                    int j = i - d;
                    if (j < 0) { if (loop) j += N; else continue; }
                    // ping-pong: left feeds right and back
                    wr[i] = L[j] + (float)p.DelayFb * wl[j];
                    wl[i] = R[j] + (float)p.DelayFb * wr[j];
                }
            }
            for (int i = 0; i < N; i++) { L[i] += (float)p.DelayMix * wl[i]; R[i] += (float)p.DelayMix * wr[i]; }
        }
        if (p.Click) {
            double spbB = 60.0 / p.Bpm;
            double step = 4.0 / Math.Max(1, p.BeatUnit);
            int idx = 0;
            for (double bPos = 0; bPos < p.LoopBeats + 1e-9; bPos += step, idx++) {
                int s0 = (int)Math.Round(bPos * spbB * SR);
                if (s0 >= N) break;
                bool downbeat = (idx % Math.Max(1, p.BeatsPerBar)) == 0;
                double freq = downbeat ? 1520.0 : 960.0;
                int len = Math.Min((int)(0.035 * SR), N - s0);
                for (int i = 0; i < len; i++) {
                    double env = Math.Exp(-i / (0.0075 * SR));
                    float v = (float)(Math.Sin(2 * Math.PI * freq * i / SR) * env * 0.28);
                    L[s0 + i] += v; R[s0 + i] += v;
                }
            }
        }
        outL = L; outR = R;
    }

    static float[] NoteBuffer(Track t, int pitch, double dur, Random rnd, int vel) {
        string syn = (t.Synth ?? "").ToLowerInvariant();
        if (syn == "drums") return Drum(pitch, rnd, t.DrumKit, vel);
        if (syn == "patch") return t.Patch == null ? new float[1] : PatchSynth.Render(t.Patch, pitch, dur);
        if (syn == "sample") return SampleVoice(t, pitch, dur);
        return Voice(t, pitch, dur, rnd);
    }

    static float[] SampleVoice(Track t, int pitch, double dur) {
        if (t.Sample == null || t.Sample.Length < 2) return new float[1];
        double ratio = t.SampleMode == 1 ? 1.0 : Math.Pow(2, (pitch - t.SampleRoot) / 12.0);
        int n;
        if (t.SampleMode == 2) n = Math.Max(1, (int)((dur + Math.Max(0.005, t.Rel)) * SR));
        else { n = (int)(t.Sample.Length / ratio); if (t.SampleGate) n = Math.Min(n, (int)((dur + Math.Max(0.005, t.Rel)) * SR)); }
        n = Math.Max(1, Math.Min(n, SR * 30));
        float[] o = new float[n];
        for (int i = 0; i < n; i++) {
            double pos = i * ratio;
            if (t.SampleMode == 2) pos = pos % Math.Max(1, t.Sample.Length - 1);
            else if (pos >= t.Sample.Length - 1) break;
            int a = (int)pos;
            double fr = pos - a;
            double x = t.Sample[a] * (1 - fr) + t.Sample[Math.Min(a + 1, t.Sample.Length - 1)] * fr;
            if (t.SampleGate) { double tm = (double)i / SR; if (tm > dur) x *= Math.Max(0, 1 - (tm - dur) / Math.Max(0.005, t.Rel)); }
            if (i < 40) x *= i / 40.0;
            o[i] = (float)x;
        }
        return o;
    }

    // Render one note of a track (synth, patch, sample or drum, with its effect chain) to a mono, trimmed, normalized sample.
    public static float[] RenderSample(Track t, int pitch, double seconds, bool normalize) {
        Project p = new Project(); p.Bpm = 60; p.BeatsPerBar = 1; p.Bars = Math.Max(1, (int)Math.Ceiling(seconds)); p.DelayMix = 0;
        Track c = new Track();
        c.Name = t.Name; c.Synth = t.Synth; c.Vol = 1; c.Pan = 0; c.Cutoff = t.Cutoff; c.Res = t.Res; c.EnvAmt = t.EnvAmt; c.Atk = t.Atk; c.Rel = t.Rel; c.Crush = t.Crush;
        c.Patch = t.Patch; c.Fx = t.Fx; c.Sample = t.Sample; c.SampleRoot = t.SampleRoot; c.SampleGate = t.SampleGate; c.DrumKit = t.DrumKit; c.InstrumentName = t.InstrumentName; c.SampleMode = t.SampleMode;
        c.Notes.Add(new Note(pitch, 0, seconds, 110)); p.Tracks.Add(c);
        short[] pcm = Render(p, false);
        float[] m = new float[pcm.Length / 2];
        int last = 0; float peak = 0;
        for (int i = 0; i < m.Length; i++) {
            m[i] = (pcm[2 * i] + pcm[2 * i + 1]) / 65536f;
            float a = Math.Abs(m[i]); if (a > peak) peak = a;
            if (a > 0.0015f) last = i;
        }
        int len = Math.Min(m.Length, last + SR / 50);
        float[] o = new float[Math.Max(2, len)];
        Array.Copy(m, o, Math.Min(len, m.Length));
        int fade = Math.Min(o.Length / 2, SR / 100);
        for (int i = 0; i < fade; i++) o[o.Length - 1 - i] *= (float)i / fade;
        if (normalize) Normalize(o, 0.89f);
        return o;
    }

    public static void Normalize(float[] s, float target) {
        float peak = 0; for (int i = 0; i < s.Length; i++) { float a = Math.Abs(s[i]); if (a > peak) peak = a; }
        if (peak < 1e-6f) return;
        float g = target / peak; for (int i = 0; i < s.Length; i++) s[i] *= g;
    }

    public static void WriteWavMono(string path, float[] s) {
        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter w = new BinaryWriter(fs)) {
            int bytes = s.Length * 2;
            w.Write(new char[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new char[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
            for (int i = 0; i < s.Length; i++) w.Write((short)Math.Max(-32767, Math.Min(32767, (int)Math.Round(s[i] * 32767.0))));
        }
    }

    // Reads 16/24-bit PCM or 32-bit float WAV, mixes to mono and resamples to 44.1 kHz.
    public static float[] ReadWav(string path) {
        byte[] b = File.ReadAllBytes(path);
        if (b.Length < 44 || b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F') throw new InvalidDataException("Not a WAV file.");
        int pos = 12, fmt = 1, ch = 1, rate = SR, bits = 16, dataPos = -1, dataLen = 0;
        while (pos + 8 <= b.Length) {
            string id = System.Text.Encoding.ASCII.GetString(b, pos, 4); int len = BitConverter.ToInt32(b, pos + 4);
            if (id == "fmt ") { fmt = BitConverter.ToInt16(b, pos + 8); ch = BitConverter.ToInt16(b, pos + 10); rate = BitConverter.ToInt32(b, pos + 12); bits = BitConverter.ToInt16(b, pos + 22); if (fmt == 65534 && len >= 26) fmt = BitConverter.ToInt16(b, pos + 32); }
            else if (id == "data") { dataPos = pos + 8; dataLen = Math.Min(len, b.Length - dataPos); break; }
            pos += 8 + len + (len & 1);
        }
        if (dataPos < 0) throw new InvalidDataException("No audio data in the WAV file.");
        int bps = bits / 8; if (ch < 1 || bps < 2) throw new InvalidDataException("Unsupported WAV format.");
        int frames = dataLen / (bps * ch);
        float[] mono = new float[frames];
        for (int i = 0; i < frames; i++) {
            double sum = 0;
            for (int c = 0; c < ch; c++) {
                int o = dataPos + (i * ch + c) * bps; double v;
                if (fmt == 3 && bps == 4) v = BitConverter.ToSingle(b, o);
                else if (bps == 2) v = BitConverter.ToInt16(b, o) / 32768.0;
                else if (bps == 3) v = (((b[o + 2] << 24) | (b[o + 1] << 16) | (b[o] << 8)) >> 8) / 8388608.0;
                else if (bps == 4) v = BitConverter.ToInt32(b, o) / 2147483648.0;
                else throw new InvalidDataException("Unsupported WAV bit depth.");
                sum += v;
            }
            mono[i] = (float)(sum / ch);
        }
        if (rate == SR || rate < 1000) return mono;
        double ratio = (double)rate / SR; int n = (int)(frames / ratio);
        float[] o2 = new float[n];
        for (int i = 0; i < n; i++) { double ps = i * ratio; int a = (int)ps; if (a >= frames - 1) break; double fr = ps - a; o2[i] = (float)(mono[a] * (1 - fr) + mono[a + 1] * fr); }
        return o2;
    }

    public static string SampleToBase64(float[] s) {
        byte[] bytes = new byte[s.Length * 2];
        for (int i = 0; i < s.Length; i++) { short v = (short)Math.Max(-32767, Math.Min(32767, (int)Math.Round(s[i] * 32767.0))); bytes[2 * i] = (byte)(v & 0xFF); bytes[2 * i + 1] = (byte)((v >> 8) & 0xFF); }
        return Convert.ToBase64String(bytes);
    }
    public static float[] SampleFromBase64(string b64) {
        byte[] bytes = Convert.FromBase64String(b64);
        float[] s = new float[bytes.Length / 2];
        for (int i = 0; i < s.Length; i++) s[i] = BitConverter.ToInt16(bytes, 2 * i) / 32768f;
        return s;
    }

    static double Osc(string shape, double ph) {
        switch (shape) {
            case "square": return ph < 0.5 ? 1 : -1;
            case "sine": return Math.Sin(2 * Math.PI * ph);
            case "tri": return 4 * Math.Abs(ph - 0.5) - 1;
            default: return 2 * ph - 1;
        }
    }

    static float[] Voice(Track t, int pitch, double dur, Random rnd) {
        string syn = (t.Synth ?? "saw").ToLowerInvariant();
        double f = 440 * Math.Pow(2, (pitch - 69) / 12.0);
        double atk = Math.Max(0.001, t.Atk), rel = Math.Max(0.005, t.Rel);
        if (syn == "pad") { atk = Math.Max(atk, 0.15); rel = Math.Max(rel, 0.4); }
        int n = (int)((dur + rel) * SR) + 1; if (n > SR * 12) n = SR * 12;
        float[] o = new float[n];
        double ph1 = 0, ph2 = rnd.NextDouble(), lo = 0, bd = 0;
        double inc = f / SR;
        float[] ks = null; int kn = 0, kp = 0;
        if (syn == "pluck") { kn = Math.Max(2, (int)(SR / f)); ks = new float[kn]; for (int i = 0; i < kn; i++) ks[i] = (float)(rnd.NextDouble() * 2 - 1); }
        string shape = (syn == "square" || syn == "sine" || syn == "tri") ? syn : "saw";
        double q = 1 - Math.Min(0.95, Math.Max(0, t.Res));
        double bits = Math.Pow(2, Math.Max(2, Math.Min(16, t.Crush)) - 1);
        for (int i = 0; i < n; i++) {
            double tm = (double)i / SR;
            double x;
            if (syn == "noise") x = rnd.NextDouble() * 2 - 1;
            else if (syn == "pluck") {
                int nx = (kp + 1) % kn;
                float y = ks[kp]; ks[kp] = 0.4975f * (ks[kp] + ks[nx]); kp = nx; x = y;
            }
            else if (syn == "fm") x = Math.Sin(2 * Math.PI * ph1 + (1 + 4 * Math.Exp(-tm * 5)) * Math.Sin(2 * Math.PI * ph1 * 2));
            else if (syn == "pad") x = 0.5 * (Osc("saw", ph1) + Osc("saw", ph2));
            else x = Osc(shape, ph1);
            ph1 += inc; if (ph1 >= 1) ph1 -= 1;
            ph2 += inc * (syn == "pad" ? 1.006 : 1); if (ph2 >= 1) ph2 -= 1;
            if (syn != "sine" && syn != "pluck") {
                double fc = t.Cutoff * (1 + t.EnvAmt * 4 * Math.Exp(-tm / 0.15));
                fc = Math.Max(40, Math.Min(fc, SR / 6.0));
                double ff = 2 * Math.Sin(Math.PI * fc / SR);
                lo += ff * bd;
                double hi = x - lo - q * bd;
                bd += ff * hi;
                x = lo;
            }
            double env = tm < atk ? tm / atk : 1.0;
            if (tm > dur) env *= Math.Max(0, 1 - (tm - dur) / rel);
            if (syn == "acid") env *= 0.35 + 0.65 * Math.Exp(-tm * 6);
            if (syn == "pluck") env *= 1.0;
            x *= env;
            if (t.Crush < 16) x = Math.Round(x * bits) / bits;
            o[i] = (float)x;
        }
        return o;
    }

    static float[] Drum(int pitch, Random rnd, string kitName, int vel) {
        float[] hit = DrumSamples.Get(kitName, pitch, vel);
        // The generated layers are all peak-normalised by the renderer, so the soft / mid / loud files
        // differ in timbre but not in level. Apply the velocity as gain here so ghost notes stay quiet.
        if (hit != null && hit.Length > 4) {
            float vg = (float)(0.42 + 0.58 * (vel / 127.0));
            float[] vhit = new float[hit.Length];
            for (int i = 0; i < hit.Length; i++) vhit[i] = hit[i] * vg;
            return vhit;
        }
        DrumKitParams kp = DrumKits.For(kitName);
        double len; int kind;
        switch (pitch) {
            case 35: case 36: len = kp.KickLen; kind = 0; break;
            case 38: case 40: len = 0.25; kind = 1; break;
            case 39: len = 0.25; kind = 2; break;
            case 37: len = 0.06; kind = 3; break;
            case 42: case 44: len = 0.09; kind = 4; break;
            case 46: len = 0.4 * (kp.OpenDecay / 9.0); kind = 5; break;
            case 49: case 55: case 57: len = kp.CrashLen; kind = 6; break;
            case 51: case 53: case 59: len = 0.6; kind = 7; break;
            case 41: case 43: case 45: case 47: case 48: case 50: len = 0.35; kind = 8; break;
            case 54: case 56: case 58: len = 0.12; kind = 11; break;
            default:
                if (pitch >= 72) { len = 0.11; kind = 13; }
                else if (pitch >= 60) { len = 0.3; kind = 12; }
                else { len = 0.35; kind = 8; }
                break;
        }
        int n = (int)(len * SR);
        float[] o = new float[n];
        double osc1 = rnd.NextDouble() * 6.2832, osc2 = rnd.NextDouble() * 6.2832, osc3 = rnd.NextDouble() * 6.2832;
        double hzKick = 55 + rnd.NextDouble() * 14 * (1 - kp.Humanize);
        for (int i = 0; i < n; i++) {
            double t = (double)i / SR, x = 0;
            switch (kind) {
                case 0: { // kick: pitch-swept body + sub sine + snap transient
                    double pv = (hzKick + kp.KickPitch * Math.Exp(-t * 30)) * (1 + (rnd.NextDouble() - 0.5) * kp.Humanize * 0.06);
                    osc1 += 2 * Math.PI * pv / SR; double body = Math.Sin(osc1) * Math.Exp(-t * kp.KickDecay) * 1.9;
                    osc2 += 2 * Math.PI * hzKick / SR; double sub = Math.Sin(osc2) * Math.Exp(-t * (kp.KickDecay * 0.55)) * kp.KickSub * 1.2;
                    double snap = Math.Exp(-t * 140) * (rnd.NextDouble() * 2 - 1) * kp.KickSnap * 1.5 + Math.Exp(-t * 300) * (rnd.NextDouble() * 2 - 1) * kp.KickClick;
                    x = body + sub + snap;
                    x = Math.Tanh(x * (1 + kp.KickDrive * 8)) * 0.8 * Math.Min(1, x == 0 ? 1 : 1.6 / Math.Max(0.5, Math.Abs(x)));
                } break;
                case 1: { // snare: snap burst + band noise + body tone + tightness
                    double burst = (rnd.NextDouble() * 2 - 1) * Math.Exp(-t * (120 - kp.SnareTight * 60)) * kp.SnareSnap * 1.4;
                    double noise = (rnd.NextDouble() * 2 - 1) * Math.Exp(-t * kp.SnareDecay) * kp.SnareNoise;
                    osc1 += 2 * Math.PI * kp.SnareTone / SR; double tone = Math.Sin(osc1) * Math.Exp(-t * 25) * kp.SnareBody * 0.8;
                    osc2 += 2 * Math.PI * kp.SnareTone * 1.5 / SR; double ring = Math.Sin(osc2) * Math.Exp(-t * 30) * kp.SnareBody * 0.25;
                    x = burst + noise + tone + ring;
                } break;
                case 2: { double e = Math.Exp(-t * kp.ClapDecay) * (1 + 0.8 * Math.Sin(t * 2 * Math.PI * 110)); x = (rnd.NextDouble() * 2 - 1) * Math.Max(0, e) * 0.7 + Math.Sin(2 * Math.PI * 1800 * t) * Math.Exp(-t * 40) * 0.25; } break;
                case 3: osc1 += 2 * Math.PI * kp.RimFreq / SR; x = Math.Sin(osc1) * Math.Exp(-t * 90) + (rnd.NextDouble() * 2 - 1) * Math.Exp(-t * 120) * 0.3; break;
                case 4: case 5: { // hats: metallic partials + filtered noise; open uses OpenDecay
                    double dec = kind == 4 ? kp.HatDecay : kp.OpenDecay; double metal = 0;
                    double[] mfr = { 616, 913, 1109, 1568, 1620, 2400 }; double ms2 = Math.Pow(2, (Math.Min(pitch, 100) - 72) / 12.0);
                    osc1 += 2 * Math.PI * mfr[0] * ms2 / SR; metal += (osc1 % 6.2832 < 3.1416 ? 1 : -1) * 0.22;
                    osc2 += 2 * Math.PI * mfr[2] * ms2 / SR; metal += (osc2 % 6.2832 < 3.1416 ? 1 : -1) * 0.14;
                    osc3 += 2 * Math.PI * mfr[4] * ms2 / SR; metal += (osc3 % 6.2832 < 3.1416 ? 1 : -1) * 0.1;
                    metal = (metal > 0 ? 1 : -1) * Math.Abs(metal);
                    double nzf = (rnd.NextDouble() * 2 - 1); double hpf = nzf - nzf * 0.15;
                    x = (hpf * 0.55 + metal * 0.35) * Math.Exp(-t * dec) * 0.8 * kp.HatBright * Math.Min(1, t * 900);
                } break;
                case 6: { // crash: bright noise burst + shimmer tail
                    x = (rnd.NextDouble() * 2 - 1) * Math.Exp(-t * 3) * 0.6 * kp.CrashBright + Math.Sin(2 * Math.PI * 6400 * t) * Math.Exp(-t * 5) * 0.2 * kp.CrashBright;
                } break;
                case 7: { // ride: ping + wash
                    osc1 += 2 * Math.PI * 5400 / SR; double ping = Math.Sin(osc1) * Math.Exp(-t * 12) * kp.RidePing * 0.8;
                    x = ((rnd.NextDouble() * 2 - 1) * 0.35 + ping * 0.5) * Math.Exp(-t * 6) * (0.5 + kp.RideCha);
                } break;
                case 8: { // tom: tuned body + partial + attack click
                    double f0 = 80 + (Math.Max(41, Math.Min(59, pitch)) - 41) * 14; double fr = f0 * (1 + 1.1 * Math.Exp(-t * 38));
                    osc1 += 2 * Math.PI * fr / SR; osc2 += 2 * Math.PI * fr * 1.59 / SR;
                    x = (Math.Sin(osc1) * 0.9 + Math.Sin(osc2) * 0.22 * Math.Exp(-t * 30)) * Math.Exp(-t * (8 + (pitch - 60) * 0.5)) + (i < 110 ? (rnd.NextDouble() * 2 - 1) * 0.25 * (1 - i / 110.0) : 0);
                } break;
                case 11: { // shaker/perc: short noise tick / shake
                    x = (rnd.NextDouble() * 2 - 1) * Math.Min(1.0, t * 250) * Math.Exp(-t * kp.PercDecay) * 0.8;
                } break;
                case 12: { // tuned tom (pitched)
                    double f0 = 98.0 * Math.Pow(2, (pitch - 60) / 12.0), fr = f0 * (1 + 1.1 * Math.Exp(-t * 38));
                    osc1 += 2 * Math.PI * fr / SR; osc2 += 2 * Math.PI * fr * 1.59 / SR;
                    x = (Math.Sin(osc1) * 0.9 + Math.Sin(osc2) * 0.22 * Math.Exp(-t * 30)) * Math.Exp(-t * (8 + (pitch - 60) * 0.5)) + (i < 110 ? (rnd.NextDouble() * 2 - 1) * 0.25 * (1 - i / 110.0) : 0);
                } break;
                case 13: { // pitched hat: metallic cluster + band noise
                    double sq = 0, mscale = Math.Pow(2, (Math.Min(pitch, 100) - 72) / 12.0);
                    double[] mfr = { 616, 913, 1109, 1568, 1620, 2400 };
                    for (int k = 0; k < 6; k++) { osc1 += 2 * Math.PI * mfr[k] * mscale / SR; sq += (osc1 % 6.2832 < 3.1416 ? 1 : -1); }
                    x = (sq * 0.03 + (rnd.NextDouble() * 2 - 1) * 0.18) * Math.Min(1.0, t * 900) * Math.Exp(-t * 50) * kp.HatBright;
                } break;
                default: osc1 += 2 * Math.PI * (200 + pitch * 5.0) / SR; x = Math.Sin(osc1) * Math.Exp(-t * 35); break;
            }
            // global kit drive + crush + room tail
            if (kp.DrumDrive > 0) x = Math.Tanh(x * (1 + kp.DrumDrive * 6)) * 0.62;
            if (kp.DrumCrush < 16) { double steps = Math.Pow(2, Math.Max(2, kp.DrumCrush) - 1); x = Math.Round(x * steps) / steps; }
            if (kp.Room > 0 && t > 0.01) x += (rnd.NextDouble() * 2 - 1) * Math.Exp(-t * 22) * kp.Room * 0.12;
            o[i] = (float)x;
        }
        return o;
    }

    public static void WriteWav(string path, short[] pcm) {
        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter w = new BinaryWriter(fs)) {
            int bytes = pcm.Length * 2;
            w.Write(new char[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)2); w.Write(SR); w.Write(SR * 4); w.Write((short)4); w.Write((short)16);
            w.Write(new char[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
            for (int i = 0; i < pcm.Length; i++) w.Write(pcm[i]);
        }
    }
}
