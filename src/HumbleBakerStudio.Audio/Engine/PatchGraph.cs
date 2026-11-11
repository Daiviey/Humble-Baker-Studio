namespace HumbleBakerStudio.Audio;


public class PatchGraph {
    public List<PatchNode> Nodes = new List<PatchNode>();
    public List<PatchLink> Links = new List<PatchLink>();
    public List<PatchGroup> Groups = new List<PatchGroup>();
    public double PanX, PanY;
    public bool Initialized;
    public PatchNode Find(string id) { foreach (PatchNode n in Nodes) if (n.Id == id) return n; return null; }
    public PatchNode OutNode() { foreach (PatchNode n in Nodes) if (n.Kind == "out") return n; return null; }
    public List<PatchLink> LinksTo(string id) { List<PatchLink> o = new List<PatchLink>(); foreach (PatchLink l in Links) if (l.To == id) o.Add(l); return o; }
    public List<PatchLink> LinksFrom(string id) { List<PatchLink> o = new List<PatchLink>(); foreach (PatchLink l in Links) if (l.From == id) o.Add(l); return o; }
    public bool HasLink(string from, string to) { foreach (PatchLink l in Links) if (l.From == from && l.To == to) return true; return false; }
}
