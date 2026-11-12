namespace HumbleBakerStudio.Audio;


public class PatchGraphControl : Control {
    Project proj;
    PatchNode dragNode;
    readonly List<PatchNode> dragNodes = new List<PatchNode>();
    readonly List<double> dragOffsX = new List<double>(), dragOffsY = new List<double>();
    readonly List<string> selIds = new List<string>();
    bool panning; int panStartX, panStartY; double panBaseX, panBaseY;
    PatchNode wireFrom; PatchNode wireHoverJack; Point wireMouse;
    PatchNode gainDrag;
    PatchNode hover;
    public event EventHandler SelectionChanged;
    public event EventHandler GraphChanged;
    public event EventHandler<PatchLink> LinkMade;   // fired whenever a new wire (or rewire) completes, so PowerShell can offer AI review
    public PatchNode Selected { get; private set; }
    public List<string> Selection { get { return selIds; } }
    const int NW = 150, TH = 66, FH = 92, BH = 96, OH = 56, GH = 108;   // track/fx/bus/out/group heights
    static readonly Font HeaderFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
    static readonly SolidBrush RedBrush = new SolidBrush(Color.FromArgb(230, 80, 90));

    public PatchGraphControl() {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(18, 20, 26); Font = new Font("Segoe UI", 8);
        TabStop = true; Cursor = Cursors.Default;
    }
    public Project Proj { get { return proj; } set { proj = value; Selected = null; selIds.Clear(); Invalidate(); } }
    PatchGraph Graph { get { return (proj != null && proj.Graph != null) ? proj.Graph : null; } }

    // ---- geometry ---------------------------------------------------------
    int NodeH(PatchNode n) { return n.Kind == "track" ? TH : n.Kind == "fx" ? FH : n.Kind == "group" ? GH : n.Kind == "bus" ? BH : OH; }
    double panX() { return Graph != null ? Graph.PanX : 0; }
    double panY() { return Graph != null ? Graph.PanY : 0; }
    Point NodePos(PatchNode n) { return new Point((int)(n.X + panX()), (int)(n.Y + panY())); }
    Rectangle NodeRect(PatchNode n) { Point p = NodePos(n); return new Rectangle(p.X, p.Y, NW, NodeH(n)); }
    Point OutJack(PatchNode n) { Rectangle r = NodeRect(n); return new Point(r.Right, r.Top + 30); }
    Point InJack(PatchNode n) { Rectangle r = NodeRect(n); return new Point(r.Left, r.Top + 30); }
    Rectangle GainRow(PatchNode n) { Rectangle r = NodeRect(n); return new Rectangle(r.Left + 12, r.Top + 54, r.Width - 24, 16); }
    PatchNode HitNode(Point p) { PatchGraph g = Graph; if (g == null) return null; for (int i = g.Nodes.Count - 1; i >= 0; i--) { PatchNode n = g.Nodes[i]; if (NodeVisible(n) && NodeRect(n).Contains(p)) return n; } return null; }
    // Jack circles half-overhang the node edge, so hit-test the jack itself, not the enclosing rect.
    PatchNode HitOutJack(Point p) { PatchGraph g = Graph; if (g == null) return null; for (int i = g.Nodes.Count - 1; i >= 0; i--) { PatchNode n = g.Nodes[i]; if (!NodeVisible(n)) continue; if (n.Kind == "out" || !KindHasOut(n)) continue; Point o = OutJack(n); if (Dist2(p, o) <= 100) return n; } return null; }
    PatchNode HitInJack(Point p) { PatchGraph g = Graph; if (g == null) return null; for (int i = g.Nodes.Count - 1; i >= 0; i--) { PatchNode n = g.Nodes[i]; if (!NodeVisible(n)) continue; if (n.Kind == "track") continue; Point o = InJack(n); if (Dist2(p, o) <= 100) return n; } return null; }
    static double Dist2(Point a, Point b) { double dx = a.X - b.X, dy = a.Y - b.Y; return dx * dx + dy * dy; }
    static bool KindHasOut(PatchNode n) { return n.Kind != "out"; }

    // ---- groups -----------------------------------------------------------
    PatchGroup GroupByNodeId(string nodeId) { PatchGraph g = Graph; if (g == null) return null; foreach (PatchGroup pg in g.Groups) if (pg.NodeId == nodeId) return pg; return null; }
    PatchGroup GroupOf(string nodeId) { PatchGraph g = Graph; if (g == null) return null; foreach (PatchGroup pg in g.Groups) if (pg.Members.Contains(nodeId)) return pg; return null; }
    bool GroupVisible(PatchGroup pg) { return GroupVisible(pg, 0); }
    bool GroupVisible(PatchGroup pg, int depth) {
        if (pg == null || depth > 32) return true;
        PatchGraph g = Graph; if (g == null) return true;
        foreach (PatchGroup parent in g.Groups) {
            if (!parent.Members.Contains(pg.NodeId)) continue;
            if (parent.Collapsed || !GroupVisible(parent, depth + 1)) return false;
        }
        return true;
    }
    bool NodeVisible(PatchNode n) {
        if (Graph == null || n == null) return false;
        if (n.Kind == "group") { PatchGroup own = GroupByNodeId(n.Id); return own == null || GroupVisible(own); }
        PatchGroup g = GroupOf(n.Id);
        return g == null || (!g.Collapsed && GroupVisible(g));
    }
    PatchNode Proxy(PatchNode n) { return Proxy(n, 0); }
    PatchNode Proxy(PatchNode n, int depth) {
        if (Graph == null || n == null || depth > 32) return n;
        if (n.Kind == "group") {
            PatchGroup own = GroupByNodeId(n.Id);
            if (own == null || GroupVisible(own)) return n;
            PatchGroup parent = GroupOf(n.Id);
            return parent == null ? n : Proxy(Graph.Find(parent.NodeId), depth + 1);
        }
        PatchGroup g = GroupOf(n.Id);
        if (g == null) return n;
        if (!g.Collapsed && GroupVisible(g)) return n;
        return Proxy(Graph.Find(g.NodeId), depth + 1);
    }
    Point OutPoint(PatchNode n) { PatchNode p = Proxy(n); return OutJack(p == null ? n : p); }
    Point InPoint(PatchNode n) { PatchNode p = Proxy(n); return InJack(p == null ? n : p); }
    bool WireDrawn(PatchLink l) { PatchGraph g = Graph; if (g == null) return false; PatchNode a = g.Find(l.From), b = g.Find(l.To); if (a == null || b == null) return false; PatchNode pa = Proxy(a), pb = Proxy(b); return pa != null && pb != null && pa.Id != pb.Id; }
    bool IsSelected(PatchNode n) { return n != null && selIds.Contains(n.Id); }
    void SetSelection(PatchNode n, bool toggle) {
        if (n == null) return;
        if (toggle) { if (selIds.Contains(n.Id)) { selIds.Remove(n.Id); if (Selected == n) Selected = selIds.Count > 0 ? Graph.Find(selIds[selIds.Count - 1]) : null; } else selIds.Add(n.Id); }
        else { selIds.Clear(); selIds.Add(n.Id); }
        Selected = n;
        Invalidate(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
    }
    int NextGroupNumber() { PatchGraph g = Graph; int n = 1; if (g != null) { foreach (PatchGroup pg in g.Groups) if (pg.Name == "Group " + n) n++; } return Math.Max(1, g == null ? 1 : g.Groups.Count + 1); }
    public PatchGroup CreateGroupFromSelection(string name) {
        PatchGraph g = Graph; if (g == null || selIds.Count == 0) return null;
        List<PatchNode> picked = new List<PatchNode>();
        foreach (string id in selIds) { PatchNode n = g.Find(id); if (n != null) picked.Add(n); }
        if (picked.Count == 0) return null;
        // one group per node: remove the picked nodes from any other group first
        foreach (PatchGroup old in g.Groups) old.Members.RemoveAll(delegate(string id) { return selIds.Contains(id); });
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue;
        foreach (PatchNode n in picked) { Rectangle r = NodeRect(n); if (r.Left < minX) minX = r.Left; if (r.Top < minY) minY = r.Top; if (r.Right > maxX) maxX = r.Right; }
        PatchGroup pg = new PatchGroup(); pg.Name = string.IsNullOrEmpty(name) ? ("Group " + NextGroupNumber()) : name; pg.ColorIndex = g.Groups.Count % 6;
        PatchNode gn = new PatchNode { Kind = "group", Label = pg.Name, X = maxX + 90 - panX(), Y = minY - panY(), Gain = 1, Pan = 0 };
        pg.NodeId = gn.Id; pg.Members = new List<string>(selIds);
        g.Nodes.Add(gn); g.Groups.Add(pg);
        selIds.Clear(); selIds.Add(gn.Id); Selected = gn;
        Changed(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
        return pg;
    }
    public void ToggleGroup(PatchGroup pg) { if (pg != null) { pg.Collapsed = !pg.Collapsed; Changed(); } }
    public void Ungroup(PatchGroup pg) {
        PatchGraph g = Graph; if (g == null || pg == null) return;
        PatchNode gn = g.Find(pg.NodeId);
        if (gn != null) { gn.Kind = "bus"; gn.Label = (pg.Name ?? "GROUP").ToUpperInvariant(); }
        g.Groups.Remove(pg);
        Changed(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
    }
    public void RenameGroup(PatchGroup pg, string name) { if (pg == null || string.IsNullOrEmpty(name)) return; pg.Name = name.Trim(); PatchGraph g = Graph; if (g != null) { PatchNode n = g.Find(pg.NodeId); if (n != null) n.Label = pg.Name; } Changed(); }
    public void RemoveGroupsForNode(PatchNode n) { PatchGraph g = Graph; if (g == null || n == null) return; if (n.Kind == "group") g.Groups.RemoveAll(delegate(PatchGroup pg) { return pg.NodeId == n.Id; }); foreach (PatchGroup pg in g.Groups) pg.Members.Remove(n.Id); }
    public void ExpandAllGroups(bool collapsed) { PatchGraph g = Graph; if (g == null) return; foreach (PatchGroup pg in g.Groups) pg.Collapsed = collapsed; Changed(); }
    public bool AddNodeToGroup(PatchNode n, PatchGroup target) {
        PatchGraph g = Graph; if (g == null || n == null || target == null) return false;
        if (n.Id == target.NodeId) return false;
        if (n.Kind == "group") {
            PatchGroup src = GroupByNodeId(n.Id);
            if (src != null) { PatchGroup walk = target; int depth = 0; while (walk != null && depth < 32) { if (walk == src) return false; walk = GroupOf(walk.NodeId); depth++; } }
        }
        foreach (PatchGroup pg in g.Groups) pg.Members.Remove(n.Id);
        if (!target.Members.Contains(n.Id)) target.Members.Add(n.Id);
        Changed(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
        return true;
    }
    // Connects only members that have no outgoing wire yet - existing cables are never touched.
    public void WireGroupMembers(PatchGroup pg) {
        PatchGraph g = Graph; if (g == null || pg == null) return;
        HashSet<string> ids = new HashSet<string>(); CollectGroupIds(pg, ids, 0);
        foreach (string id in ids) {
            PatchNode n = g.Find(id); if (n == null || id == pg.NodeId) continue;
            if (g.LinksFrom(n.Id).Count == 0 && !g.HasLink(n.Id, pg.NodeId)) g.Links.Add(new PatchLink(n.Id, pg.NodeId));
        }
        Changed();
    }
    bool InGroupTree(PatchNode n, PatchGroup target) {
        if (n == null || target == null) return false;
        string id = n.Id; int depth = 0;
        while (!string.IsNullOrEmpty(id) && depth < 32) {
            PatchGroup own = GroupOf(id); if (own == null) return false;
            if (own == target) return true;
            id = own.NodeId; depth++;
        }
        return false;
    }
    void CollectGroupIds(PatchGroup pg, HashSet<string> ids, int depth) {
        if (pg == null || depth > 32) return;
        foreach (string id in pg.Members) {
            if (!ids.Add(id)) continue;
            PatchNode n = Graph == null ? null : Graph.Find(id);
            if (n != null && n.Kind == "group") { PatchGroup child = GroupByNodeId(id); if (child != null) CollectGroupIds(child, ids, depth + 1); }
        }
    }
    int CountGroupTree(PatchGroup pg) { HashSet<string> ids = new HashSet<string>(); CollectGroupIds(pg, ids, 0); return ids.Count; }
    string PromptGroupName(string title, string def) { try { return Microsoft.VisualBasic.Interaction.InputBox("Group name:", title, def).Trim(); } catch { return def; } }
    void CreateGroupUi() {
        if (selIds.Count == 0) return;
        string def = "Group " + NextGroupNumber();
        string nm = PromptGroupName("Create group", def); if (string.IsNullOrEmpty(nm)) nm = def;
        CreateGroupFromSelection(nm);
    }
    public void GroupSelectionInteractive() { CreateGroupUi(); }
    // Distance from a point to a wire (cubic bezier), sampled coarsely - fine for interaction.
    public static double BezierDist(Point a, Point c1, Point c2, Point b, Point p) {
        double best = double.MaxValue;
        for (int i = 0; i <= 48; i++) {
            double t = i / 48.0, u = 1 - t;
            double x = u * u * u * a.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * b.X;
            double y = u * u * u * a.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * b.Y;
            double d = (x - p.X) * (x - p.X) + (y - p.Y) * (y - p.Y);
            if (d < best) best = d;
        }
        return Math.Sqrt(best);
    }
    Point WireMid(PatchNode from, PatchNode to) { Point a = OutPoint(from), b = InPoint(to); float dx = Math.Max(60, Math.Abs(b.X - a.X) * 0.5f); double t = 0.5, u = 1 - t; int x = (int)(u * u * u * a.X + 3 * u * u * t * (a.X + dx) + 3 * u * t * t * (b.X - dx) + t * t * t * b.X); int y = (int)(u * u * u * a.Y + 3 * u * u * t * a.Y + 3 * u * t * t * b.Y + t * t * t * b.Y); return new Point(x, y); }

    // ---- view helpers -----------------------------------------------------
    // Moves the pan so the whole patch sits at the top-left of the canvas with a small margin.
    public void FrameAll() {
        PatchGraph g = Graph; if (g == null) { return; }
        if (g.Nodes.Count == 0) { g.PanX = 0; g.PanY = 0; Invalidate(); return; }
        int minX = int.MaxValue, minY = int.MaxValue; bool any = false;
        foreach (PatchNode n in g.Nodes) { if (!NodeVisible(n)) continue; Rectangle r = NodeRect(n); if (r.Left < minX) minX = r.Left; if (r.Top < minY) minY = r.Top; any = true; }
        if (!any) foreach (PatchNode n in g.Nodes) { Rectangle r = NodeRect(n); if (r.Left < minX) minX = r.Left; if (r.Top < minY) minY = r.Top; }
        g.PanX += 30 - minX; g.PanY += 30 - minY;
        Invalidate();
    }
    // A saved pan (or a stray drag) can leave every module off-screen. Fit the view when that happens.
    public void EnsureVisible() {
        PatchGraph g = Graph; if (g == null || g.Nodes.Count == 0) { return; }
        Rectangle view = new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height));
        foreach (PatchNode n in g.Nodes) if (NodeVisible(n) && view.IntersectsWith(NodeRect(n))) return;
        FrameAll();
    }
    // Re-lays the modules out in signal-flow columns (tracks > FX/buses > master out) without touching the wiring.
    public void Tidy() {
        PatchGraph g = Graph; if (g == null || g.Nodes.Count == 0) { return; }
        Dictionary<string, int> lvl = new Dictionary<string, int>();
        foreach (PatchNode n in g.Nodes) lvl[n.Id] = (n.Kind == "track") ? 0 : 1;
        int guard = g.Nodes.Count + 4;
        for (int pass = 0; pass < guard; pass++) {
            bool moved = false;
            foreach (PatchLink l in g.Links) {
                PatchNode f = g.Find(l.From), t = g.Find(l.To);
                if (f == null || t == null || t.Kind == "out") continue;
                int fl, tl; lvl.TryGetValue(f.Id, out fl); lvl.TryGetValue(t.Id, out tl);
                if (tl <= fl) { lvl[t.Id] = fl + 1; moved = true; }
            }
            if (!moved) break;
        }
        int maxL = 0; foreach (KeyValuePair<string, int> kv in lvl) if (kv.Value > maxL) maxL = kv.Value;
        PatchNode outN = g.OutNode(); if (outN != null) lvl[outN.Id] = maxL + 1;
        SortedDictionary<int, List<PatchNode>> cols = new SortedDictionary<int, List<PatchNode>>();
        foreach (PatchNode n in g.Nodes) {
            int lv; lvl.TryGetValue(n.Id, out lv);
            List<PatchNode> list; if (!cols.TryGetValue(lv, out list)) { list = new List<PatchNode>(); cols[lv] = list; }
            list.Add(n);
        }
        foreach (KeyValuePair<int, List<PatchNode>> col in cols) {
            List<PatchNode> list = col.Value;
            list.Sort(delegate(PatchNode a, PatchNode b) { return a.Y.CompareTo(b.Y); });
            double y = 30;
            foreach (PatchNode n in list) { n.X = 30 + col.Key * 330; n.Y = y; y += NodeH(n) + 26; }
        }
        FrameAll();
    }
    // Loaded projects can contain track modules for tracks that no longer exist (or duplicates).
    // Drop them, plus any wires that pointed at them, so the graph matches the real track list.
    public static int PruneTrackNodes(Project p) {
        if (p == null || p.Graph == null) return 0;
        PatchGraph g = p.Graph;
        HashSet<string> uids = new HashSet<string>();
        if (p.Tracks != null) foreach (Track t in p.Tracks) if (!string.IsNullOrEmpty(t.Uid)) uids.Add(t.Uid);
        HashSet<string> seen = new HashSet<string>();
        List<string> dead = new List<string>();
        foreach (PatchNode n in g.Nodes) {
            if (n.Kind != "track") continue;
            if (string.IsNullOrEmpty(n.TrackUid) || !uids.Contains(n.TrackUid) || !seen.Add(n.TrackUid)) dead.Add(n.Id);
        }
        if (dead.Count == 0) return 0;
        HashSet<string> ids = new HashSet<string>(dead);
        g.Nodes.RemoveAll(delegate(PatchNode n) { return ids.Contains(n.Id); });
        g.Links.RemoveAll(delegate(PatchLink l) { return ids.Contains(l.From) || ids.Contains(l.To); });
        foreach (PatchGroup pg in g.Groups) pg.Members.RemoveAll(delegate(string id) { return ids.Contains(id); });
        g.Groups.RemoveAll(delegate(PatchGroup pg) { return string.IsNullOrEmpty(pg.NodeId) || g.Find(pg.NodeId) == null; });
        return dead.Count;
    }
    PatchLink HitWire(Point p) {
        PatchGraph g = Graph; if (g == null) return null;
        PatchLink best = null; double bestD = 8;
        foreach (PatchLink l in g.Links) {
            PatchNode from = g.Find(l.From), to = g.Find(l.To); if (from == null || to == null) continue;
            if (!WireDrawn(l)) continue;
            Point a = OutPoint(from), b = InPoint(to); float dx = Math.Max(60, Math.Abs(b.X - a.X) * 0.5f);
            double d = BezierDist(a, new Point((int)(a.X + dx), a.Y), new Point((int)(b.X - dx), b.Y), b, p);
            if (d < bestD) { bestD = d; best = l; }
        }
        return best;
    }
    // Inserts n into link's chain: from -> n -> to. Only modules with both an input and an output may be spliced.
    public static bool SpliceNode(PatchGraph g, PatchNode n, PatchLink link) {
        if (g == null || n == null || link == null) return false;
        if (n.Kind != "fx" && n.Kind != "bus") return false;
        if (link.From == n.Id || link.To == n.Id) return false;
        if (!g.Nodes.Contains(n) || !g.Links.Contains(link)) return false;
        string from = link.From, to = link.To;
        g.Links.Remove(link);
        if (!g.HasLink(from, n.Id)) g.Links.Add(new PatchLink(from, n.Id));
        if (!g.HasLink(n.Id, to)) g.Links.Add(new PatchLink(n.Id, to));
        return true;
    }

    // ---- painting -----------------------------------------------------------
    Color KindColor(PatchNode n) { return n.Kind == "track" ? Color.FromArgb(28, 110, 92) : n.Kind == "fx" ? Color.FromArgb(150, 95, 35) : n.Kind == "group" ? Color.FromArgb(120, 80, 170) : n.Kind == "bus" ? Color.FromArgb(55, 95, 150) : Color.FromArgb(150, 55, 70); }
    static readonly Color[] GroupColors = new Color[] { Color.FromArgb(40, 200, 170), Color.FromArgb(90, 150, 240), Color.FromArgb(230, 150, 70), Color.FromArgb(220, 90, 140), Color.FromArgb(150, 200, 80), Color.FromArgb(180, 130, 220) };
    Color GroupColor(PatchGroup pg) { int i = pg == null ? 0 : pg.ColorIndex; return GroupColors[(i % GroupColors.Length + GroupColors.Length) % GroupColors.Length]; }
    string NodeTitle(PatchNode n) {
        if (n.Kind == "track" && proj != null) { for (int i = 0; i < proj.Tracks.Count; i++) if (proj.Tracks[i].Uid == n.TrackUid) return (proj.Tracks[i].Name ?? "track").ToUpperInvariant(); return "(MISSING)"; }
        if (n.Kind == "group" || n.Kind == "bus") return (n.Label ?? (n.Kind == "group" ? "GROUP" : "BUS")).ToUpperInvariant();
        if (n.Kind == "out") return "MASTER OUT";
        return (n.Label ?? n.Type ?? "FX").ToUpperInvariant();
    }
    void DrawJack(Graphics g, Point p, bool input) {
        using (SolidBrush b = new SolidBrush(Color.FromArgb(230, 190, 90))) g.FillEllipse(b, p.X - 6, p.Y - 6, 12, 12);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(18, 20, 26))) g.FillEllipse(b, p.X - 3, p.Y - 3, 6, 6);
    }
    protected override void OnPaint(PaintEventArgs e) {
        Graphics g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        using (Pen dot = new Pen(Color.FromArgb(35, 38, 48), 1)) for (int x = 20; x < Width; x += 24) for (int y = 14; y < Height; y += 24) g.DrawRectangle(dot, x, y, 0, 0);
        PatchGraph gp = Graph;
        if (gp == null) {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(90, 96, 115))) g.DrawString("No project loaded.", Font, b, 20, 20);
            return;
        }
        // group boundaries (behind the modules)
        foreach (PatchGroup pg in gp.Groups) {
            if (pg.Collapsed || !GroupVisible(pg)) continue;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue; bool any = false;
            foreach (PatchNode n in gp.Nodes) {
                if (!NodeVisible(n) || !InGroupTree(n, pg)) continue;
                Rectangle r = NodeRect(n);
                if (r.Left < minX) minX = r.Left; if (r.Top < minY) minY = r.Top; if (r.Right > maxX) maxX = r.Right; if (r.Bottom > maxY) maxY = r.Bottom; any = true;
            }
            PatchNode gn0 = gp.Find(pg.NodeId);
            if (gn0 != null && NodeVisible(gn0)) { Rectangle r = NodeRect(gn0); if (r.Left < minX) minX = r.Left; if (r.Top < minY) minY = r.Top; if (r.Right > maxX) maxX = r.Right; if (r.Bottom > maxY) maxY = r.Bottom; any = true; }
            if (!any) continue;
            Rectangle box = Rectangle.FromLTRB(minX - 18, minY - 30, maxX + 18, maxY + 18);
            Color gc = GroupColor(pg);
            using (SolidBrush gb = new SolidBrush(Color.FromArgb(26, gc))) g.FillRectangle(gb, box);
            using (Pen gp2 = new Pen(Color.FromArgb(150, gc), 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }) g.DrawRectangle(gp2, box);
            using (SolidBrush head = new SolidBrush(Color.FromArgb(210, gc))) g.FillRectangle(head, box.Left, box.Top, Math.Min(box.Width, 190), 22);
            using (SolidBrush htxt = new SolidBrush(Color.FromArgb(20, 22, 28))) g.DrawString((pg.Name ?? "GROUP").ToUpperInvariant() + "  Ã‚Â·  " + CountGroupTree(pg) + " modules", HeaderFont, htxt, new RectangleF(box.Left + 8, box.Top + 2, 300, 18));
        }
        // wires
        foreach (PatchLink l in gp.Links) {
            PatchNode from = gp.Find(l.From), to = gp.Find(l.To); if (from == null || to == null) continue;
            if (!WireDrawn(l)) continue;
            Point a = OutPoint(from), b = InPoint(to);
            float dx = Math.Max(60, Math.Abs(b.X - a.X) * 0.5f);
            using (Pen wp = new Pen(Color.FromArgb(220, 180, 80), 3f)) g.DrawBezier(wp, a, new Point(a.X + (int)dx, a.Y), new Point(b.X - (int)dx, b.Y), b);
        }
        if (wireFrom != null) {
            Point a = OutPoint(wireFrom), b = wireMouse; float dx = Math.Max(60, Math.Abs(b.X - a.X) * 0.5f);
            using (Pen wp = new Pen(Color.FromArgb(255, 220, 120), 3f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }) g.DrawBezier(wp, a, new Point(a.X + (int)dx, a.Y), new Point(b.X - (int)dx, b.Y), b);
        }
        // nodes
        using (SolidBrush nb = new SolidBrush(Color.FromArgb(30, 33, 44)))
        using (Pen sel = new Pen(Color.FromArgb(40, 200, 170), 2))
        using (Pen norm = new Pen(Color.FromArgb(60, 66, 84), 1))
        using (SolidBrush headerTxt = new SolidBrush(Color.White))
        using (SolidBrush sub = new SolidBrush(Color.FromArgb(150, 158, 180)))
        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap }) {
            foreach (PatchNode n in gp.Nodes) {
                if (!NodeVisible(n)) continue;
                Rectangle r = NodeRect(n); bool isSel = IsSelected(n);
                using (System.Drawing.Drawing2D.GraphicsPath path = RoundRect(r, 7)) {
                    g.FillPath(nb, path);
                    using (SolidBrush hd = new SolidBrush(KindColor(n))) { using (System.Drawing.Drawing2D.GraphicsPath hp = RoundRect(new Rectangle(r.X, r.Y, r.Width, 22), 7)) { g.FillPath(hd, hp); } g.FillRectangle(hd, r.X, r.Y + 12, r.Width, 10); }
                    g.DrawPath(isSel ? sel : norm, path);
                }
                g.DrawString(NodeTitle(n), HeaderFont, headerTxt, new RectangleF(r.X + 6, r.Y + 2, r.Width - 12, 20), sf);
                if (n.Kind != "track") DrawJack(g, InJack(n), true);
                if (KindHasOut(n)) DrawJack(g, OutJack(n), false);
                if (n.Kind == "track" && proj != null) { for (int i = 0; i < proj.Tracks.Count; i++) if (proj.Tracks[i].Uid == n.TrackUid) { g.DrawString(proj.Tracks[i].Synth.ToUpperInvariant(), Font, sub, new RectangleF(r.X + 8, r.Y + 30, r.Width - 16, 24), sf); if (proj.Tracks[i].Mute) g.DrawString("MUTED", Font, RedBrush, new RectangleF(r.X + 8, r.Y + TH - 22, r.Width - 16, 16), sf); } }
                else if (n.Kind == "fx") { g.DrawString(n.Bypass ? "BYPASSED" : FxTag(n), Font, n.Bypass ? RedBrush : sub, new RectangleF(r.X + 8, r.Y + 34, r.Width - 16, 40), sf); }
                else if (n.Kind == "bus") { Rectangle gr = GainRow(n); using (Pen bg = new Pen(Color.FromArgb(60, 66, 84), 3)) g.DrawLine(bg, gr.Left, gr.Top + 8, gr.Right, gr.Top + 8); int dotw = (int)((Math.Max(0, Math.Min(2, n.Gain)) / 2.0) * (gr.Width - 12)); using (SolidBrush lb = new SolidBrush(Color.FromArgb(40, 200, 170))) g.FillEllipse(lb, gr.Left + dotw - 6, gr.Top + 2, 12, 12); using (SolidBrush lb2 = new SolidBrush(Color.FromArgb(150, 158, 180))) { g.DrawString("GAIN", Font, lb2, new RectangleF(gr.Left, gr.Top - 2, gr.Width, 12), sf); } g.DrawString("PAN " + n.Pan.ToString("0.00"), Font, sub, new RectangleF(r.X + 8, r.Y + 72, r.Width - 16, 16), sf); }
                else if (n.Kind == "group") {
                    PatchGroup pg = GroupByNodeId(n.Id);
                    Rectangle gr = GainRow(n);
                    using (Pen bg = new Pen(Color.FromArgb(60, 66, 84), 3)) g.DrawLine(bg, gr.Left, gr.Top + 8, gr.Right, gr.Top + 8);
                    int dotw = (int)((Math.Max(0, Math.Min(2, n.Gain)) / 2.0) * (gr.Width - 12)); using (SolidBrush lb = new SolidBrush(GroupColor(pg))) g.FillEllipse(lb, gr.Left + dotw - 6, gr.Top + 2, 12, 12);
                    g.DrawString("GAIN", Font, sub, new RectangleF(gr.Left, gr.Top - 2, gr.Width, 12), sf);
                    g.DrawString("PAN " + n.Pan.ToString("0.00"), Font, sub, new RectangleF(r.X + 8, r.Y + 72, r.Width - 16, 16), sf);
                    string state = (pg != null && pg.Collapsed) ? "CONDENSED COMPONENT" : "GROUP MASTER";
                    g.DrawString(state + " Ã‚Â· " + (pg == null ? 0 : CountGroupTree(pg)) + " modules", Font, sub, new RectangleF(r.X + 8, r.Y + 26, r.Width - 16, 18), sf);
                    g.DrawString((pg != null && pg.Collapsed) ? "double-click to expand" : "double-click to condense", Font, sub, new RectangleF(r.X + 8, r.Y + 88, r.Width - 16, 16), sf);
                }
                else if (n.Kind == "out") { g.DrawString("master chain", Font, sub, new RectangleF(r.X + 8, r.Y + 26, r.Width - 16, 20), sf); }
            }
        }
        if (wireHoverJack != null) { PatchNode n = wireHoverJack; Point o = InJack(n); using (Pen hi = new Pen(Color.FromArgb(80, 255, 140), 3)) g.DrawEllipse(hi, o.X - 10, o.Y - 10, 20, 20); }
        if (wireFrom != null) { using (SolidBrush b = new SolidBrush(Color.FromArgb(150, 158, 180))) g.DrawString("release on an IN jack to patch > green = good target, or drop onto a wire to splice", Font, b, 12, Height - 22); }
    }
    static string FxTag(PatchNode n) {
        if (n == null) return "(empty module)";
        if (n.HasPlugin && !n.PluginBypass) return "VST3 · " + (string.IsNullOrEmpty(n.PluginName) ? "PLUG-IN" : n.PluginName.ToUpperInvariant());
        if (n.Fx == null) return "(empty module)";
        if (!string.IsNullOrEmpty(n.Module)) return n.Module.ToUpperInvariant();
        Fx fx = n.Fx;
        List<string> on = new List<string>();
        if (fx.Filter != FilterMode.off) on.Add("FILTER"); if (fx.Drive > 0) on.Add("DRIVE"); if (fx.Bits < 16 || fx.Downsample > 1) on.Add("CRUSH"); if (fx.RingMix > 0) on.Add("RING"); if (fx.StutterBeats > 0) on.Add("STUTTER"); if (fx.Chorus > 0) on.Add("CHORUS"); if (fx.DelayMix > 0) on.Add("DELAY"); if (fx.ReverbMix > 0) on.Add("REVERB");
        return on.Count == 0 ? "FX RACK" : string.Join(" | ", on.ToArray());
    }
    static System.Drawing.Drawing2D.GraphicsPath RoundRect(Rectangle r, int rad) {
        System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath(); int d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90); p.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90); p.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90); p.CloseFigure();
        return p;
    }

    // ---- interaction --------------------------------------------------------
    void Changed() { Invalidate(); if (GraphChanged != null) GraphChanged(this, EventArgs.Empty); }
    void LinkFired(PatchNode from, PatchNode to) { if (LinkMade != null) LinkMade(this, new PatchLink(from.Id, to.Id)); }
    bool OverGainRow(Point p) { PatchNode n = HitNode(p); if (n != null && (n.Kind == "bus" || n.Kind == "group")) return GainRow(n).Contains(p); return false; }
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e); if (e.Button == MouseButtons.Right) return;
        PatchGraph g = Graph; if (g == null) return;
        Focus();
        PatchNode outJack = HitOutJack(e.Location);
        if (outJack != null) { wireFrom = outJack; wireHoverJack = null; wireMouse = e.Location; Cursor = Cursors.Cross; Capture = true; return; }
        PatchNode inJack = HitInJack(e.Location);
        if (inJack != null) {
            PatchLink existing = null; foreach (PatchLink l in g.LinksTo(inJack.Id)) { existing = l; break; }
            if (existing != null) { g.Links.Remove(existing); PatchNode src = g.Find(existing.From); Changed(); wireFrom = src; wireHoverJack = null; wireMouse = e.Location; Cursor = Cursors.Cross; Capture = true; }
            return;
        }
        PatchNode over = HitNode(e.Location);
        if (over != null && OverGainRow(e.Location)) { gainDrag = over; Capture = true; SetGainFromX(e.X); return; }
        if (over != null) {
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            if (ctrl) SetSelection(over, true);
            else if (!IsSelected(over)) SetSelection(over, false);
            else { Selected = over; Invalidate(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty); }
            dragNodes.Clear(); dragOffsX.Clear(); dragOffsY.Clear();
            if (IsSelected(over)) {
                foreach (string id in selIds) {
                    PatchNode sn = g.Find(id); if (sn == null || !NodeVisible(sn)) continue;
                    Rectangle sr = NodeRect(sn);
                    dragNodes.Add(sn); dragOffsX.Add(e.X - sr.X); dragOffsY.Add(e.Y - sr.Y);
                }
            }
            // Dragging a group component moves the whole group, including modules hidden inside it.
            if (over.Kind == "group") {
                PatchGroup dg = GroupByNodeId(over.Id);
                if (dg != null) {
                    HashSet<string> gids = new HashSet<string>(); CollectGroupIds(dg, gids, 0); gids.Add(over.Id);
                    foreach (string id in gids) {
                        PatchNode sn = g.Find(id); if (sn == null || dragNodes.Contains(sn)) continue;
                        Rectangle sr = NodeRect(sn);
                        dragNodes.Add(sn); dragOffsX.Add(e.X - sr.X); dragOffsY.Add(e.Y - sr.Y);
                    }
                }
            }
            dragNode = IsSelected(over) ? over : null; Capture = true; return;
        }
        if (!((Control.ModifierKeys & Keys.Control) == Keys.Control)) { selIds.Clear(); Selected = null; }
        panning = true; panStartX = e.X; panStartY = e.Y; panBaseX = g.PanX; panBaseY = g.PanY; Capture = true; Cursor = Cursors.SizeAll;
    }
    void SetGainFromX(int x) { if (gainDrag == null) return; Rectangle gr = GainRow(gainDrag); double t = (x - gr.Left) / (double)(gr.Width - 12); gainDrag.Gain = Math.Max(0, Math.Min(2, t * 2)); Changed(); }
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        PatchGraph g = Graph; if (g == null) return;
        if (wireFrom != null) { wireMouse = e.Location; wireHoverJack = HitInJack(e.Location); Cursor = wireHoverJack != null ? Cursors.SizeAll : Cursors.Cross; Invalidate(); return; }
        if (gainDrag != null) { SetGainFromX(e.X); return; }
        if (dragNodes.Count > 0) {
            for (int i = 0; i < dragNodes.Count; i++) {
                PatchNode dn = dragNodes[i];
                dn.X = e.X - dragOffsX[i] - panX(); dn.Y = e.Y - dragOffsY[i] - panY();
            }
            Invalidate(); return;
        }
        if (panning) { g.PanX = panBaseX + (e.X - panStartX); g.PanY = panBaseY + (e.Y - panStartY); Invalidate(); return; }
        PatchNode h = HitNode(e.Location);
        if (!ReferenceEquals(h, hover)) { hover = h; Cursor = h != null ? Cursors.Hand : Cursors.Default; }
    }
    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        PatchGraph g = Graph;
        if (wireFrom != null && g != null) {
            PatchNode target = HitInJack(e.Location);
            if (target != null && !ReferenceEquals(target, wireFrom) && target.Kind != "track" && !g.HasLink(wireFrom.Id, target.Id)) {
                g.Links.Add(new PatchLink(wireFrom.Id, target.Id)); Changed(); LinkFired(wireFrom, target);
            } else Invalidate();
            wireFrom = null; wireHoverJack = null; Cursor = Cursors.Default; Capture = false; return;
        }
        PatchLink w = (dragNode != null && g != null) ? HitWire(e.Location) : null;
        if (w != null) {
            string fromId = w.From, toId = w.To;
            if (SpliceNode(g, dragNode, w)) { Changed(); PatchNode f = g.Find(fromId), t = g.Find(toId); if (f != null && t != null) { LinkFired(f, dragNode); LinkFired(dragNode, t); } }
        }
        gainDrag = null; dragNode = null; dragNodes.Clear(); dragOffsX.Clear(); dragOffsY.Clear(); panning = false; Capture = false; Cursor = Cursors.Default;
    }
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); }
    protected override void OnMouseDoubleClick(MouseEventArgs e) {
        base.OnMouseDoubleClick(e);
        if (e.Button != MouseButtons.Left) return;
        PatchNode n = HitNode(e.Location);
        if (n == null || n.Kind != "group") return;
        PatchGroup pg = GroupByNodeId(n.Id); if (pg == null) return;
        ToggleGroup(pg);
        if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
    }
    public void DeleteSelected() {
        PatchGraph g = Graph; if (g == null) return;
        List<string> ids = new List<string>(selIds);
        if (ids.Count == 0 && Selected != null) ids.Add(Selected.Id);
        if (ids.Count == 0) return;
        foreach (string id in ids) {
            PatchNode n = g.Find(id); if (n == null) continue;
            if (n.Kind == "group") g.Groups.RemoveAll(delegate(PatchGroup pg) { return pg.NodeId == n.Id; });
            foreach (PatchGroup pg in g.Groups) pg.Members.Remove(id);
            g.Nodes.Remove(n);
            g.Links.RemoveAll(delegate(PatchLink l) { return l.From == id || l.To == id; });
        }
        selIds.Clear(); Selected = null; Changed();
        if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
    }
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e); PatchGraph g = Graph; if (g == null) return;
        if (e.KeyCode == Keys.Delete && Selected != null) { DeleteSelected(); e.Handled = true; }
        else if (e.KeyCode == Keys.Escape && wireFrom != null) { wireFrom = null; wireHoverJack = null; Invalidate(); }
    }
    protected override void OnMouseClick(MouseEventArgs e) {
        base.OnMouseClick(e); if (e.Button != MouseButtons.Right) return; PatchGraph g = Graph; if (g == null) return;
        ContextMenuStrip menu = new ContextMenuStrip(); menu.BackColor = Color.FromArgb(32, 35, 44); menu.ForeColor = Color.FromArgb(224, 228, 236);
        PatchNode over = HitNode(e.Location);
        Point world = new Point((int)(e.X - panX()), (int)(e.Y - panY()));
        if (over != null) {
            if (!IsSelected(over)) { selIds.Clear(); selIds.Add(over.Id); Selected = over; Invalidate(); }
            PatchGroup overGroup = GroupByNodeId(over.Id);
            if (overGroup != null) {
                ToolStripMenuItem tog = new ToolStripMenuItem(overGroup.Collapsed ? "Expand group" : "Condense to component node"); tog.Click += delegate { ToggleGroup(overGroup); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty); }; menu.Items.Add(tog);
                ToolStripMenuItem wireG = new ToolStripMenuItem("Wire unconnected members to this master"); wireG.Click += delegate { WireGroupMembers(overGroup); }; menu.Items.Add(wireG);
                ToolStripMenuItem ren = new ToolStripMenuItem("Rename group..."); ren.Click += delegate { string nm = PromptGroupName("Rename group", overGroup.Name); RenameGroup(overGroup, nm); }; menu.Items.Add(ren);
                ToolStripMenuItem un = new ToolStripMenuItem("Ungroup (keep the bus)"); un.Click += delegate { Ungroup(overGroup); }; menu.Items.Add(un);
                menu.Items.Add(new ToolStripSeparator());
            }
            PatchGroup memberOf = GroupOf(over.Id);
            if (memberOf != null) {
                ToolStripMenuItem rm = new ToolStripMenuItem("Remove from group '" + memberOf.Name + "'"); rm.Click += delegate { memberOf.Members.Remove(over.Id); Changed(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty); }; menu.Items.Add(rm);
            }
            if (g.Groups.Count > 0) {
                ToolStripMenuItem addTo = new ToolStripMenuItem("Add to group >");
                foreach (PatchGroup pg in g.Groups) {
                    if (pg.Members.Contains(over.Id)) continue;
                    PatchGroup target = pg;
                    ToolStripMenuItem mi = new ToolStripMenuItem(pg.Name);
                    mi.Click += delegate { if (!AddNodeToGroup(over, target)) MessageBox.Show("That would put a group inside itself.", "Patch groups", MessageBoxButtons.OK, MessageBoxIcon.Information); };
                    addTo.DropDownItems.Add(mi);
                }
                if (addTo.DropDownItems.Count > 0) menu.Items.Add(addTo);
            }
            ToolStripMenuItem mk = new ToolStripMenuItem("Group selected nodes..."); mk.Click += delegate { CreateGroupUi(); }; menu.Items.Add(mk);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem del = new ToolStripMenuItem(over.Kind == "track" ? "Delete track module (track stays)" : (over.Kind == "group" ? "Delete group (members stay)" : "Delete module")); del.Click += delegate { selIds.Clear(); selIds.Add(over.Id); Selected = over; DeleteSelected(); }; menu.Items.Add(del);
            if (over.Kind != "out") { ToolStripMenuItem bp = new ToolStripMenuItem(over.Bypass ? "Enable module" : "Bypass module"); bp.Click += delegate { over.Bypass = !over.Bypass; Changed(); }; menu.Items.Add(bp); }
            PatchNode outNode = g.OutNode();
            if (over.Kind != "out") { ToolStripMenuItem toOut = new ToolStripMenuItem("Wire to Master Out"); toOut.Click += delegate { if (outNode != null && !g.HasLink(over.Id, outNode.Id)) { g.Links.Add(new PatchLink(over.Id, outNode.Id)); Changed(); } }; menu.Items.Add(toOut); }
        } else {
            if (selIds.Count > 0) {
                ToolStripMenuItem mk = new ToolStripMenuItem("Group selected nodes (" + selIds.Count + ")..."); mk.Click += delegate { CreateGroupUi(); }; menu.Items.Add(mk);
                menu.Items.Add(new ToolStripSeparator());
            }
            ToolStripMenuItem expAll = new ToolStripMenuItem("Expand all groups"); expAll.Click += delegate { ExpandAllGroups(false); }; menu.Items.Add(expAll);
            ToolStripMenuItem colAll = new ToolStripMenuItem("Condense all groups"); colAll.Click += delegate { ExpandAllGroups(true); }; menu.Items.Add(colAll);
            menu.Items.Add(new ToolStripSeparator());
            PatchLink overWire = HitWire(e.Location);
            if (overWire != null) {
                PatchNode fromNode = g.Find(overWire.From), toNode = g.Find(overWire.To);
                Point mid = (fromNode != null && toNode != null) ? WireMid(fromNode, toNode) : new Point(world.X, world.Y);
                ToolStripMenuItem ins = new ToolStripMenuItem("Insert FX module into this wire >");
                foreach (string[] mod in FxCatalog) { string label = mod[0], type = mod[1]; ToolStripMenuItem mi = new ToolStripMenuItem(label); mi.Click += delegate { PatchNode n = NewFxNode(type, mid.X - NW / 2, mid.Y - FH / 2); g.Nodes.Add(n); if (SpliceNode(g, n, overWire)) { Changed(); PatchNode f = g.Find(overWire.From), t = g.Find(overWire.To); if (f != null && t != null) { LinkFired(f, n); LinkFired(n, t); } } }; ins.DropDownItems.Add(mi); }
                menu.Items.Add(ins);
                ToolStripMenuItem insBus = new ToolStripMenuItem("Insert Bus amp into this wire >"); insBus.Click += delegate { PatchNode n = new PatchNode { Kind = "bus", Label = "BUS " + (CountKind(g, "bus") + 1), X = mid.X - NW / 2, Y = mid.Y - BH / 2, Gain = 1, Pan = 0 }; g.Nodes.Add(n); if (SpliceNode(g, n, overWire)) { Changed(); PatchNode f = g.Find(overWire.From), t = g.Find(overWire.To); if (f != null && t != null) { LinkFired(f, n); LinkFired(n, t); } } }; menu.Items.Add(insBus);
                if (fromNode != null && toNode != null) { ToolStripMenuItem disc = new ToolStripMenuItem("Disconnect this wire (" + NodeTitle(fromNode) + " -> " + NodeTitle(toNode) + " -> "); disc.Click += delegate { g.Links.Remove(overWire); Changed(); }; menu.Items.Add(disc); }
            }
            ToolStripMenuItem addFx = new ToolStripMenuItem("Add FX module"); 
            foreach (string[] mod in FxCatalog) { string label = mod[0], type = mod[1]; ToolStripMenuItem mi = new ToolStripMenuItem(label); mi.Click += delegate { g.Nodes.Add(NewFxNode(type, world.X, world.Y)); Changed(); }; addFx.DropDownItems.Add(mi); }
            menu.Items.Add(addFx);
            ToolStripMenuItem addBus = new ToolStripMenuItem("Add Bus (gain + pan)"); addBus.Click += delegate { PatchNode n = new PatchNode { Kind = "bus", Label = "BUS " + (CountKind(g, "bus") + 1), X = world.X, Y = world.Y, Gain = 1, Pan = 0 }; g.Nodes.Add(n); Changed(); }; menu.Items.Add(addBus);
            ToolStripMenuItem addOut = new ToolStripMenuItem("Add Master Out"); addOut.Click += delegate { g.Nodes.Add(new PatchNode { Kind = "out", Label = "out", X = world.X, Y = world.Y }); Changed(); }; menu.Items.Add(addOut);
        }
        menu.Show(this, e.Location);
    }
    static int CountKind(PatchGraph g, string kind) { int c = 0; foreach (PatchNode n in g.Nodes) if (n.Kind == kind) c++; return c; }
    public static readonly string[][] FxCatalog = new string[][] {
        new[] { "Filter", "filter" }, new[] { "Drive", "drive" }, new[] { "Crush", "crush" }, new[] { "Fold", "fold" },
        new[] { "Ring mod", "ring" }, new[] { "Stutter", "stutter" }, new[] { "Chorus", "chorus" }, new[] { "Delay", "delay" },
        new[] { "Reverb", "reverb" }, new[] { "Phaser", "phaser" }, new[] { "Flanger", "flanger" }, new[] { "Tape stop", "tapestop" },
        new[] { "Tape delay", "tapedelay" }, new[] { "Plate", "plate" }, new[] { "Vinyl", "vinyl" }, new[] { "Compressor", "comp" },
        new[] { "EQ + width", "eq" }, new[] { "Width", "width" }, new[] { "Saturator", "saturator" }, new[] { "Exciter", "exciter" },
        new[] { "Rectifier", "rectify" }, new[] { "Gate", "gate" }, new[] { "Transient", "transient" }, new[] { "Tremolo", "tremolo" },
        new[] { "Auto-pan", "autopan" }, new[] { "Vibrato", "vibrato" }, new[] { "Auto-wah", "autowah" }, new[] { "Comb", "comb" },
        new[] { "Spring reverb", "spring" }, new[] { "Shimmer", "shimmer" }, new[] { "Haas", "haas" }, new[] { "Limiter", "limiter" },
        new[] { "Ensemble", "ensemble" }, new[] { "Rotary", "rotary" }, new[] { "Octaver", "octaver" }, new[] { "Harmonizer", "harmonizer" },
        new[] { "Resonator", "resonator" }, new[] { "Tilt EQ", "tilteq" }, new[] { "Mid/Side EQ", "midside" }, new[] { "De-esser", "deesser" },
        new[] { "Multiband comp", "mbcomp" }, new[] { "Clipper", "clipper" }, new[] { "Auto-filter", "autofilter" }, new[] { "Gated reverb", "gatedverb" },
        new[] { "Reverse reverb", "reverseverb" }, new[] { "Multi-tap", "multitap" }, new[] { "Doubler", "doubler" }, new[] { "Formant", "formant" },
        new[] { "Ladder filter", "ladder" }, new[] { "Tube preamp", "tube" }, new[] { "Tape saturator", "tapesat" }, new[] { "Diode drive", "diode" },
        new[] { "Fuzz", "fuzz" }, new[] { "Chip tune", "chiptune" }, new[] { "Sub bass", "subbass" }, new[] { "Bass enhancer", "bassenh" },
        new[] { "Frequency shifter", "freqshift" }, new[] { "Air band", "airband" }, new[] { "Stereo image", "stereoimage" }, new[] { "Ping-pong delay", "pingpong" },
        new[] { "Dub delay", "dubdelay" }, new[] { "Cathedral reverb", "cathedral" }, new[] { "Room reverb", "roomverb" }, new[] { "Ambience", "ambience" },
        new[] { "Wah pedal", "wahpedal" }, new[] { "Talk box", "talkbox" }, new[] { "Ducker", "ducker" }, new[] { "Level rider", "rider" },
        new[] { "Harmonic tremolo", "harmtrem" }, new[] { "Beat repeat", "beatrepeat" }, new[] { "Granular cloud", "grain" }, new[] { "Glitch gate", "glitchgate" },
        new[] { "Sweep filter", "sweep" },
        new[] { "Opto compressor", "optocomp" }, new[] { "FET compressor", "fetcomp" }, new[] { "VCA compressor", "vcacomp" }, new[] { "Expander", "expander" },
        new[] { "Mid/Side comp", "mscomp" }, new[] { "Dynamic EQ", "dyneq" }, new[] { "Pultec EQ", "pulteceq" }, new[] { "Overdrive", "overdrive" },
        new[] { "Metal drive", "metaldrive" }, new[] { "Speaker cab", "cabsim" }, new[] { "Multiband drive", "mbdist" }, new[] { "Tape flutter", "tapeflutter" },
        new[] { "Dimension", "dimension" }, new[] { "Envelope phaser", "envphaser" }, new[] { "Envelope flanger", "envflanger" }, new[] { "Chopper", "chopper" },
        new[] { "Barber-pole", "barberpole" }, new[] { "Half speed", "halfspeed" }, new[] { "Pitch echo", "pitchecho" }, new[] { "Spectral freeze", "spectralfreeze" },
        new[] { "Reverse delay", "revdelay" }, new[] { "Echo-verb", "echoverb" }, new[] { "Bloom reverb", "bloomverb" }, new[] { "Non-linear verb", "nonlinverb" },
        new[] { "Vocoder", "vocoder" },
        new[] { "Spectral gate", "specgate" }, new[] { "Spectral blur", "specblur" }, new[] { "Spectral delay", "specdelay" }, new[] { "Convolution reverb", "convreverb" },
        new[] { "Tank reverb", "tankreverb" }, new[] { "Auto-tune", "autotune" }, new[] { "Octave up", "octaveup" }, new[] { "Chord generator", "chordgen" },
        new[] { "Upward compressor", "upcomp" }, new[] { "Transient gate", "trangate" }, new[] { "Stereo shuffler", "shuffle" }, new[] { "Mono maker", "monomaker" },
        new[] { "Phase aligner", "phasealign" }, new[] { "Tape loop", "tapeloop" }, new[] { "Stutter gate", "stuttergate" }
    };
    public static PatchNode NewFxNode(string type, int x, int y) {
        PatchNode n = new PatchNode(); n.Kind = "fx"; n.Type = type; n.Module = type; n.Label = type.ToUpperInvariant(); n.X = x; n.Y = y;
        Fx fx = n.Fx;
        switch (type) {
            case "filter": fx.Filter = FilterMode.lowpass; fx.Cutoff = 1200; fx.Resonance = 0.4; break;
            case "drive": fx.Drive = 0.4; break;
            case "crush": fx.Bits = 6; fx.Downsample = 2; break;
            case "ring": fx.RingMix = 0.5; fx.RingFreq = 200; break;
            case "stutter": fx.StutterBeats = 0.25; fx.StutterDiv = 4; fx.StutterChance = 0.3; break;
            case "chorus": fx.Chorus = 0.35; fx.ChorusRate = 0.6; fx.ChorusDepth = 0.5; break;
            case "delay": fx.DelayMix = 0.3; fx.DelayBeats = 0.75; break;
            case "reverb": fx.ReverbMix = 0.35; fx.ReverbSize = 0.6; break;
            case "phaser": fx.PhaserMix = 0.4; fx.PhaserRate = 0.5; fx.PhaserDepth = 0.6; fx.PhaserStages = 4; break;
            case "flanger": fx.FlangerMix = 0.4; fx.FlangerRate = 0.8; fx.FlangerDepth = 0.6; fx.FlangerFb = 0.4; break;
            case "tapestop": fx.TapeStop = 0.8; fx.TapeStopRate = 1.2; break;
            case "tapedelay": fx.TapeDelayMix = 0.3; fx.TapeDelayTime = 280; fx.TapeDelayFb = 0.45; fx.TapeDelayWow = 0.3; break;
            case "plate": fx.PlateMix = 0.35; fx.PlateSize = 0.6; fx.PlateDamping = 0.5; break;
            case "vinyl": fx.VinylCrackle = 0.4; fx.TapeHiss = 0.2; fx.TapeWow = 0.3; break;
            case "comp": fx.CompThresh = -18; fx.CompRatio = 3; fx.CompAttack = 10; fx.CompRelease = 120; fx.CompMakeup = 3; break;
            case "eq": fx.LowShelf = 2; fx.HighShelf = 2; fx.Width = 1.05; break;
            case "fold": fx.Fold = 0.4; break;
            case "width": fx.Width = 1.3; break;
            case "saturator": fx.Saturator = 0.45; fx.SatMix = 0.5; break;
            case "exciter": fx.Exciter = 0.4; fx.ExciterFreq = 4000; fx.ExciterMix = 0.5; break;
            case "rectify": fx.RectifyMix = 0.5; fx.RectifyDrive = 0.2; break;
            case "gate": fx.GateAmount = 0.8; fx.GateThreshold = -45; break;
            case "transient": fx.TransientAttack = 0.4; fx.TransientSustain = -0.2; fx.TransientSensitivity = 0.5; break;
            case "tremolo": fx.TremoloMix = 0.6; fx.TremoloRate = 5; fx.TremoloDepth = 0.5; break;
            case "autopan": fx.AutoPanMix = 0.6; fx.AutoPanRate = 0.5; fx.AutoPanDepth = 0.7; break;
            case "vibrato": fx.VibratoMix = 0.5; fx.VibratoRate = 5; fx.VibratoDepth = 0.3; break;
            case "autowah": fx.AutoWahMix = 0.6; fx.AutoWahRate = 1.5; fx.AutoWahDepth = 0.6; fx.AutoWahResonance = 0.6; break;
            case "comb": fx.CombMix = 0.5; fx.CombFreq = 220; fx.CombFeedback = 0.5; break;
            case "spring": fx.SpringMix = 0.4; fx.SpringDecay = 0.6; break;
            case "shimmer": fx.ShimmerMix = 0.4; fx.ShimmerPitch = 12; fx.ShimmerDecay = 0.7; break;
            case "haas": fx.HaasMix = 0.5; fx.HaasDelay = 14; break;
            case "limiter": fx.LimiterOn = true; fx.LimiterThreshold = -6; fx.LimiterCeiling = -1; break;
            case "ensemble": fx.EnsembleMix = 0.5; fx.EnsembleRate = 0.6; fx.EnsembleDepth = 0.5; break;
            case "rotary": fx.RotaryMix = 0.5; fx.RotaryRate = 0.8; fx.RotaryDepth = 0.7; break;
            case "octaver": fx.OctaverMix = 0.45; break;
            case "harmonizer": fx.HarmonizerMix = 0.4; fx.HarmonizerSemi = 7; break;
            case "resonator": fx.ResonatorMix = 0.4; fx.ResonatorFreq = 220; fx.ResonatorFeedback = 0.6; break;
            case "tilteq": fx.TiltGain = 3; break;
            case "midside": fx.MidGain = 2; fx.SideGain = 2; break;
            case "deesser": fx.DeEsserAmount = 0.5; break;
            case "mbcomp": fx.MBOn = true; fx.MBLowThresh = -18; fx.MBMidThresh = -18; fx.MBHighThresh = -18; fx.MBRatio = 3; fx.MBMakeup = 2; break;
            case "clipper": fx.ClipperAmount = 0.5; fx.ClipperDrive = 0.5; break;
            case "autofilter": fx.AutoFilterMix = 0.5; fx.AutoFilterSensitivity = 0.6; fx.AutoFilterResonance = 0.5; break;
            case "gatedverb": fx.GateVerbMix = 0.4; fx.GateVerbDecay = 0.6; fx.GateVerbHold = 0.25; break;
            case "reverseverb": fx.ReverseVerbMix = 0.4; fx.ReverseVerbDecay = 0.6; break;
            case "multitap": fx.MultiTapMix = 0.4; fx.MultiTapTime = 180; fx.MultiTapFb = 0.4; break;
            case "doubler": fx.DoublerMix = 0.5; fx.DoublerDelay = 18; break;
            case "formant": fx.FormantMix = 0.5; fx.FormantVowel = 0; fx.FormantShift = 1; break;
            case "ladder": fx.LadderMix = 0.6; fx.LadderCutoff = 1200; fx.LadderReso = 0.5; fx.LadderDrive = 0.3; break;
            case "tube": fx.TubeMix = 0.6; fx.TubeDrive = 0.5; fx.TubeBias = 0.2; break;
            case "tapesat": fx.TapeSatMix = 0.6; fx.TapeSatDrive = 0.45; fx.TapeSatTone = 0.5; break;
            case "diode": fx.DiodeMix = 0.6; fx.DiodeDrive = 0.5; fx.DiodeTone = 0.5; break;
            case "fuzz": fx.FuzzAmount = 0.5; fx.FuzzGate = 0.2; fx.FuzzMix = 0.5; break;
            case "chiptune": fx.ChipMix = 0.6; fx.ChipBits = 6; fx.ChipRate = 8; break;
            case "subbass": fx.SubBassMix = 0.5; fx.SubBassFreq = 60; fx.SubBassDrive = 0.3; break;
            case "bassenh": fx.BassEnhMix = 0.5; fx.BassEnhFreq = 120; fx.BassEnhAmount = 0.4; break;
            case "freqshift": fx.FreqShiftMix = 0.5; fx.FreqShiftHz = 100; break;
            case "airband": fx.AirMix = 0.5; fx.AirFreq = 9000; fx.AirGain = 4; break;
            case "stereoimage": fx.StereoWidth = 1.3; fx.BassMonoFreq = 120; break;
            case "pingpong": fx.PingPongMix = 0.35; fx.PingPongTime = 350; fx.PingPongFb = 0.45; break;
            case "dubdelay": fx.DubMix = 0.35; fx.DubTime = 380; fx.DubFb = 0.55; fx.DubTone = 0.5; break;
            case "cathedral": fx.CathedralMix = 0.35; fx.CathedralSize = 0.8; fx.CathedralDamp = 0.5; break;
            case "roomverb": fx.RoomMix = 0.3; fx.RoomSize = 0.5; fx.RoomDamp = 0.5; break;
            case "ambience": fx.AmbienceMix = 0.3; fx.AmbienceSize = 0.6; fx.AmbienceDamp = 0.5; break;
            case "wahpedal": fx.WahPedalMix = 0.6; fx.WahPedalRate = 2; fx.WahPedalDepth = 0.7; fx.WahPedalQ = 0.6; break;
            case "talkbox": fx.TalkMix = 0.6; fx.TalkRate = 3; fx.TalkVowel = 0; break;
            case "ducker": fx.DuckerMix = 0.5; fx.DuckerThreshold = -30; fx.DuckerDepth = 0.6; fx.DuckerRelease = 150; break;
            case "rider": fx.RiderAmount = 0.5; fx.RiderTarget = -18; fx.RiderSpeed = 0.3; break;
            case "harmtrem": fx.HarmTremMix = 0.6; fx.HarmTremRate = 4; fx.HarmTremDepth = 0.5; break;
            case "beatrepeat": fx.BeatRepeatMix = 0.5; fx.BeatRepeatDiv = 4; fx.BeatRepeatChance = 0.3; break;
            case "grain": fx.GrainMix = 0.5; fx.GrainSize = 80; fx.GrainDensity = 20; break;
            case "glitchgate": fx.GlitchMix = 0.5; fx.GlitchDensity = 0.3; fx.GlitchSeed = 1; break;
            case "sweep": fx.SweepMix = 0.6; fx.SweepRate = 0.5; fx.SweepDepth = 0.6; fx.SweepQ = 0.5; fx.SweepCenter = 1200; break;
            case "optocomp": fx.OptoMix = 0.6; fx.OptoThresh = -18; fx.OptoRatio = 3; fx.OptoRelease = 250; break;
            case "fetcomp": fx.FetMix = 0.6; fx.FetThresh = -20; fx.FetRatio = 4; fx.FetAttack = 2; break;
            case "vcacomp": fx.VcaMix = 0.6; fx.VcaThresh = -16; fx.VcaRatio = 2; fx.VcaRelease = 200; break;
            case "expander": fx.ExpMix = 0.6; fx.ExpThresh = -40; fx.ExpRatio = 2; fx.ExpRange = 24; break;
            case "mscomp": fx.MSCompMix = 0.6; fx.MSCompThresh = -20; fx.MSCompRatio = 3; fx.MSCompRelease = 120; break;
            case "dyneq": fx.DynEqMix = 0.6; fx.DynEqFreq = 2500; fx.DynEqThresh = -24; fx.DynEqAmount = 0.5; break;
            case "pulteceq": fx.PultecLow = 3; fx.PultecHigh = 4; fx.PultecAtten = 2; break;
            case "overdrive": fx.OverdriveMix = 0.6; fx.OverdriveDrive = 0.5; fx.OverdriveTone = 0.5; break;
            case "metaldrive": fx.MetalMix = 0.6; fx.MetalDrive = 0.6; fx.MetalMid = 0.3; break;
            case "cabsim": fx.CabMix = 0.6; fx.CabDrive = 0.4; fx.CabTone = 0.4; break;
            case "mbdist": fx.MBDistMix = 0.6; fx.MBDistLow = 0.3; fx.MBDistMid = 0.6; fx.MBDistHigh = 0.4; break;
            case "tapeflutter": fx.FlutterMix = 0.6; fx.FlutterRate = 0.7; fx.FlutterDepth = 0.5; break;
            case "dimension": fx.DimMix = 0.6; fx.DimRate = 0.4; fx.DimDepth = 0.6; break;
            case "envphaser": fx.EnvPhaserMix = 0.6; fx.EnvPhaserThresh = -30; fx.EnvPhaserDepth = 0.7; fx.EnvPhaserRate = 0.5; break;
            case "envflanger": fx.EnvFlangeMix = 0.6; fx.EnvFlangeThresh = -30; fx.EnvFlangeDepth = 0.7; fx.EnvFlangeFb = 0.4; break;
            case "chopper": fx.ChopMix = 0.7; fx.ChopRate = 8; fx.ChopDepth = 1; fx.ChopShape = 0.5; break;
            case "barberpole": fx.BarberMix = 0.6; fx.BarberRate = 0.3; fx.BarberDepth = 0.8; fx.BarberStages = 4; break;
            case "halfspeed": fx.HalfSpeedMix = 0.6; fx.HalfSpeedAmount = 1; break;
            case "pitchecho": fx.PitchEchoMix = 0.4; fx.PitchEchoTime = 350; fx.PitchEchoFb = 0.4; fx.PitchEchoSemi = 12; break;
            case "spectralfreeze": fx.FreezeMix = 0.6; fx.FreezeThreshold = -50; fx.FreezeDecay = 0.8; break;
            case "revdelay": fx.RevDelayMix = 0.4; fx.RevDelayTime = 300; fx.RevDelayFb = 0; break;
            case "echoverb": fx.EchoVerbMix = 0.4; fx.EchoVerbTime = 320; fx.EchoVerbFb = 0.4; fx.EchoVerbSize = 0.6; break;
            case "bloomverb": fx.BloomMix = 0.5; fx.BloomSize = 0.8; fx.BloomThresh = -40; fx.BloomAttack = 0.15; break;
            case "nonlinverb": fx.NonLinVerbMix = 0.4; fx.NonLinVerbSize = 0.7; fx.NonLinVerbDrive = 0.6; fx.NonLinVerbGate = 0.3; break;
            case "vocoder": fx.VocoderMix = 0.6; fx.VocoderBands = 8; fx.VocoderRelease = 80; fx.VocoderCarrier = 0; break;
            case "specgate": fx.SpecGateMix = 0.6; fx.SpecGateThresh = 0.02; fx.SpecGateBands = 6; break;
            case "specblur": fx.SpecBlurMix = 0.5; fx.SpecBlurAmount = 0.5; fx.SpecBlurSpread = 0.6; break;
            case "specdelay": fx.SpecDelayMix = 0.4; fx.SpecDelayTime = 260; fx.SpecDelayFb = 0.4; fx.SpecDelaySpread = 0.6; break;
            case "convreverb": fx.ConvMix = 0.35; fx.ConvSize = 0.6; fx.ConvDamp = 0.45; fx.ConvPre = 20; break;
            case "tankreverb": fx.TankMix = 0.4; fx.TankDecay = 0.6; fx.TankTone = 0.5; fx.TankDrip = 0.4; break;
            case "autotune": fx.TuneMix = 0.6; fx.TuneSpeed = 0.5; fx.TuneScale = 0; fx.TuneMinHz = 70; break;
            case "octaveup": fx.OctUpMix = 0.45; fx.OctUpTone = 0.5; break;
            case "chordgen": fx.ChordMix = 0.4; fx.ChordType = 0; fx.ChordSpread = 0.3; break;
            case "upcomp": fx.UpCompMix = 0.5; fx.UpCompThresh = -30; fx.UpCompRatio = 2; fx.UpCompRange = 18; break;
            case "trangate": fx.TranGateMix = 0.6; fx.TranGateThresh = -28; fx.TranGateHold = 80; fx.TranGateRelease = 120; break;
            case "shuffle": fx.ShuffleMix = 0.6; fx.ShuffleLow = 0.5; fx.ShuffleHigh = 1.5; fx.ShuffleFreq = 180; break;
            case "monomaker": fx.MonoMix = 0.6; fx.MonoFreq = 120; fx.MonoWidth = 0.5; break;
            case "phasealign": fx.PhaseMix = 0.5; fx.PhaseFreq = 800; fx.PhaseAmount = 0.5; break;
            case "tapeloop": fx.LoopMix = 0.5; fx.LoopLength = 500; fx.LoopSpeed = 1; fx.LoopFb = 0.2; break;
            case "stuttergate": fx.StutterGateMix = 0.6; fx.StutterGateRate = 8; fx.StutterGateDiv = 4; fx.StutterGateChance = 0.35; break;
        }
        return n;
    }
    // Project-to-patch: every track becomes a node in one modular network. A track sent to a legacy
    // bus gets its own FX module node that references the LIVE shared bus FX instance (all tracks on
    // a bus share that one instance). A track with no bus gets a dedicated FX module node that
    // references the LIVE track FX instance, so enabling/tweaking effects on the track applies.
    // Unpatched sessions therefore sound exactly like the mixer they replace, through the graph.
    public static void AutoPatch(Project p) {
        PatchGraph g = p.Graph; g.Nodes.Clear(); g.Links.Clear(); g.Groups.Clear();
        double y = 40;
        PatchNode outNode = new PatchNode { Kind = "out", Label = "out", X = 1080, Y = 160 }; g.Nodes.Add(outNode);
        Dictionary<int, PatchNode> busFx = new Dictionary<int, PatchNode>();
        for (int i = 0; i < p.Tracks.Count; i++) { Track t = p.Tracks[i]; PatchNode tn = new PatchNode { Kind = "track", Label = t.Name, TrackUid = t.Uid, X = 40, Y = y }; g.Nodes.Add(tn);
            PatchNode fx = null; bool viaBus = false;
            if (t.Bus >= 1 && t.Bus <= 4 && p.Buses != null && t.Bus <= p.Buses.Count && p.Buses[t.Bus - 1].Enabled) {
                int bIdx = t.Bus;
                if (!busFx.TryGetValue(bIdx, out fx)) {
                    Bus bus = p.Buses[bIdx - 1];
                    fx = new PatchNode { Kind = "fx", Type = "busfx", Label = "BUS " + (char)(64 + bIdx), X = 560, Y = y };
                    fx.Fx = bus.Fx; fx.Auto = bus.Auto;             // LIVE shared instance - one FX per bus, heard by all its tracks
                    g.Nodes.Add(fx); busFx[bIdx] = fx;
                }
                viaBus = true;
            } else {
                fx = new PatchNode { Kind = "fx", Type = "trackfx", Label = t.Name + " FX", X = 560, Y = y };
                fx.Fx = t.Fx;                                  // LIVE instance owned by the track
                g.Nodes.Add(fx);
            }
            g.Links.Add(new PatchLink(tn.Id, fx.Id));
            if (!viaBus) { g.Links.Add(new PatchLink(fx.Id, outNode.Id)); }
            y += 96;
        }
        foreach (KeyValuePair<int, PatchNode> kv in busFx) g.Links.Add(new PatchLink(kv.Value.Id, outNode.Id));
        g.Initialized = true;
    }
    // Appends one new track (and its FX module) to an already-initialized graph without wiping
    // the user's wiring. Safe to call when the graph is uninitialized (no-op).
    public static void AddTrackNode(Project p, Track t) {
        if (p == null || t == null) return;
        PatchGraph g = p.Graph;
        if (g == null || !g.Initialized) return;
        // skip if this track already has a node
        foreach (PatchNode n in g.Nodes) if (n.Kind == "track" && n.TrackUid == t.Uid) return;
        double maxY = 40;
        PatchNode outNode = null;
        foreach (PatchNode n in g.Nodes) {
            if (n.Y > maxY) maxY = n.Y;
            if (n.Kind == "out") outNode = n;
        }
        double y = maxY + 96;
        if (outNode == null) { outNode = new PatchNode { Kind = "out", Label = "out", X = 1080, Y = 160 }; g.Nodes.Add(outNode); }
        PatchNode tn = new PatchNode { Kind = "track", Label = t.Name, TrackUid = t.Uid, X = 40, Y = y };
        g.Nodes.Add(tn);
        PatchNode fx = null; bool viaBus = false;
        if (t.Bus >= 1 && t.Bus <= 4 && p.Buses != null && t.Bus <= p.Buses.Count && p.Buses[t.Bus - 1].Enabled) {
            int bIdx = t.Bus;
            // reuse existing busfx node if it exists
            foreach (PatchNode n in g.Nodes) if (n.Kind == "fx" && n.Type == "busfx" && n.Label == "BUS " + (char)(64 + bIdx)) fx = n;
            if (fx == null) {
                Bus bus = p.Buses[bIdx - 1];
                fx = new PatchNode { Kind = "fx", Type = "busfx", Label = "BUS " + (char)(64 + bIdx), X = 560, Y = y };
                fx.Fx = bus.Fx;
                fx.Auto = bus.Auto;
                g.Nodes.Add(fx);
            }
            viaBus = true;
        } else {
            fx = new PatchNode { Kind = "fx", Type = "trackfx", Label = t.Name + " FX", X = 560, Y = y };
            fx.Fx = t.Fx;
            g.Nodes.Add(fx);
        }
        g.Links.Add(new PatchLink(tn.Id, fx.Id));
        if (!viaBus) g.Links.Add(new PatchLink(fx.Id, outNode.Id));
    }
}
