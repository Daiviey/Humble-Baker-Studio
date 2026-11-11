namespace HumbleBakerStudio.Audio;


public static class PatchRender {
    public static bool Active(Project p) { return p != null && p.Graph != null && p.Graph.Initialized && p.Graph.Nodes.Count > 0; }
    // True when the graph contains a module that owns this FX instance. RenderStereo uses this to
    // decide whether the track's FX is processed by a graph module (once) or by the legacy mixer path.
    public static bool HasFxNode(Project p, Fx fx) {
        if (p == null || p.Graph == null || fx == null) return false;
        foreach (PatchNode n in p.Graph.Nodes) if (n.Kind == "fx" && ReferenceEquals(n.Fx, fx)) return true;
        return false;
    }
    // Track/bus FX objects are occasionally replaced (AI design, preset clear). Point the graph's
    // trackfx/busfx modules at the live objects again so the graph always processes the current FX.
    public static void BindFxNodes(Project p) {
        if (p == null || p.Graph == null) return;
        PatchGraph g = p.Graph;
        foreach (PatchNode n in g.Nodes) {
            if (n.Kind != "fx" || n.Type == null) continue;
            if (string.Equals(n.Type, "trackfx", StringComparison.OrdinalIgnoreCase)) {
                foreach (PatchLink l in g.Links) {
                    if (l.To != n.Id) continue;
                    PatchNode up = g.Find(l.From);
                    if (up == null || up.Kind != "track") continue;
                    Track t = null; foreach (Track x in p.Tracks) if (x.Uid == up.TrackUid) { t = x; break; }
                    if (t != null) { n.Fx = t.Fx; n.Auto = t.Auto; }
                    break;
                }
            } else if (string.Equals(n.Type, "busfx", StringComparison.OrdinalIgnoreCase)) {
                string lab = n.Label ?? "";
                if (lab.StartsWith("BUS ") && lab.Length >= 5) {
                    int bi = lab[4] - 'A' + 1;
                    if (bi >= 1 && bi <= p.Buses.Count) { n.Fx = p.Buses[bi - 1].Fx; n.Auto = p.Buses[bi - 1].Auto; }
                }
            }
        }
    }

    // Resolves the track index for a track node (by uid), -1 when the track was deleted.
    static int TrackIndex(List<Track> ts, string uid) {
        if (string.IsNullOrEmpty(uid)) return -1;
        for (int i = 0; i < ts.Count; i++) if (ts[i].Uid == uid) return i;
        return -1;
    }

    public static void Run(Project p, List<Track> allT, List<float[]> allL, List<float[]> allR, List<bool> ghost, bool loop, double spb, float[] sinkL, float[] sinkR, int N) {
        PatchGraph g = p.Graph;
        Dictionary<string, PatchNode> byId = new Dictionary<string, PatchNode>();
        foreach (PatchNode n in g.Nodes) byId[n.Id] = n;
        Dictionary<string, float[]> memoL = new Dictionary<string, float[]>(), memoR = new Dictionary<string, float[]>();
        Dictionary<string, bool> gotInput = new Dictionary<string, bool>();
        Dictionary<string, int> mark = new Dictionary<string, int>();

        Func<PatchNode, float[][]> eval = null;
        eval = delegate(PatchNode node) {
            if (memoL.ContainsKey(node.Id)) return new float[][] { memoL[node.Id], memoR[node.Id] };
            int m; mark.TryGetValue(node.Id, out m);
            if (m == 1) return null;                 // dependency cycle: this branch is muted
            mark[node.Id] = 1;
            float[] sL, sR;
            if (node.Kind == "track") {
                int ti = TrackIndex(allT, node.TrackUid);
                if (ti >= 0 && !ghost[ti]) { sL = allL[ti]; sR = allR[ti]; }
                else { sL = new float[N]; sR = new float[N]; }
            } else {
                sL = new float[N]; sR = new float[N]; bool any = false;
                List<PatchNode> srcNodes = new List<PatchNode>();
                List<float[]> inL = new List<float[]>(), inR = new List<float[]>();
                foreach (PatchLink link in g.LinksTo(node.Id)) {
                    PatchNode src; byId.TryGetValue(link.From, out src);
                    if (src == null) continue;
                    float[][] s = eval(src);
                    if (s == null) continue;
                    srcNodes.Add(src); inL.Add(s[0]); inR.Add(s[1]);
                    any = true;
                }
                // Placement-based sidechain: at a bus/group, the bottom-most (largest Y)
                // connected source is the trigger and ducks the other inputs. Opt-in via
                // the shared node's FX Sidechain Mix.
                int duckTrigger = -1; float[] duckGain = null;
                bool isMixBus = node.Kind == "bus" || node.Kind == "group" || (node.Kind == "fx" && string.Equals(node.Type, "busfx", StringComparison.OrdinalIgnoreCase));
                if (any && isMixBus && !node.Bypass && node.Fx != null && node.Fx.SidechainMix > 0 && srcNodes.Count >= 2) {
                    int ti = 0;
                    for (int k = 1; k < srcNodes.Count; k++) if (srcNodes[k].Y > srcNodes[ti].Y) ti = k;
                    float[] tL = inL[ti], tR = inR[ti];
                    double mix = Math.Max(0, Math.Min(1, node.Fx.SidechainMix));
                    double atk = 1 - Math.Exp(-1.0 / (Math.Max(0.5, node.Fx.SidechainAttack) * 0.001 * Dsp.SR));
                    double rel = 1 - Math.Exp(-1.0 / (Math.Max(10, node.Fx.SidechainRelease) * 0.001 * Dsp.SR));
                    double env = 0; float[] duck = new float[N];
                    for (int i = 0; i < N; i++) {
                        double x = Math.Max(Math.Abs(tL[i]), Math.Abs(tR[i]));
                        env += (x > env ? atk : rel) * (x - env);
                        duck[i] = (float)(1 - mix * Math.Min(1, env * 2));
                    }
                    duckTrigger = ti; duckGain = duck;
                }
                for (int k = 0; k < inL.Count; k++) {
                    float[] dl = inL[k], dr = inR[k];
                    if (duckGain != null && k != duckTrigger) {
                        for (int i = 0; i < N; i++) { sL[i] += dl[i] * duckGain[i]; sR[i] += dr[i] * duckGain[i]; }
                    } else {
                        for (int i = 0; i < N; i++) { sL[i] += dl[i]; sR[i] += dr[i]; }
                    }
                }
                gotInput[node.Id] = any;
                if (node.Kind == "fx") {
                    if (!node.Bypass && node.Fx != null) { float[] cL = (float[])sL.Clone(), cR = (float[])sR.Clone(); if (node.Auto != null && node.Auto.Count > 0) AutoFx.ApplyLanes(node.Auto, node.Fx, cL, cR, spb); if (node.Fx.Active) FxProc.Process(node.Fx, cL, cR, loop, spb); sL = cL; sR = cR; }
                } else if (node.Kind == "bus" || node.Kind == "group") {
                    if (!node.Bypass) {
                        // Group masters are buses too: they can process their own FX and trim/pan the group sum.
                        if (node.Kind == "group" && node.Fx != null) { float[] cL = (float[])sL.Clone(), cR = (float[])sR.Clone(); if (node.Auto != null && node.Auto.Count > 0) AutoFx.ApplyLanes(node.Auto, node.Fx, cL, cR, spb); if (node.Fx.Active) FxProc.Process(node.Fx, cL, cR, loop, spb); sL = cL; sR = cR; }
                        double pan = Math.Max(-1, Math.Min(1, node.Pan));
                        float pl = (float)Math.Cos((pan + 1) * Math.PI / 4), pr = (float)Math.Sin((pan + 1) * Math.PI / 4), gGain = (float)node.Gain;
                        float[] tL = new float[N], tR = new float[N];
                        for (int i = 0; i < N; i++) { tL[i] = (float)(sL[i] * pl * gGain); tR[i] = (float)(sR[i] * pr * gGain); }
                        sL = tL; sR = tR;
                    }
                }
            }
            memoL[node.Id] = sL; memoR[node.Id] = sR; mark[node.Id] = 2;
            return new float[][] { sL, sR };
        };

        PatchNode outNode = g.OutNode();
        if (outNode != null) { float[][] s = eval(outNode); if (s != null) for (int i = 0; i < N; i++) { sinkL[i] += s[0][i]; sinkR[i] += s[1][i]; } }

        // Auto-route: a track that has no outgoing wires still sounds, so an unpatched session is
        // audible. FX and bus modules do NOT auto-route: they are heard only when actually wired
        // to an output. Otherwise a module dropped on the canvas but never connected would bleed
        // into the master and appear to affect every track.
        foreach (PatchNode node in g.Nodes) {
            if (node.Kind != "track") continue;
            if (g.LinksFrom(node.Id).Count != 0) continue;
            float[][] s = eval(node); if (s == null) continue;
            for (int i = 0; i < N; i++) { sinkL[i] += s[0][i]; sinkR[i] += s[1][i]; }
        }
        // Out nodes that are not the primary out still count (multiple outputs fan out fine).
        foreach (PatchNode node in g.Nodes) {
            if (node.Kind != "out" || ReferenceEquals(node, outNode)) continue;
            float[][] s = eval(node); if (s == null) continue;
            for (int i = 0; i < N; i++) { sinkL[i] += s[0][i]; sinkR[i] += s[1][i]; }
        }
    }
}
