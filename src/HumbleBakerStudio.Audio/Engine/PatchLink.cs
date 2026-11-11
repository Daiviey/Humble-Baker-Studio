namespace HumbleBakerStudio.Audio;


public class PatchLink {
    public string From, To;   // node ids: an OUT jack feeds any number of IN jacks; ins are summed
    public PatchLink() { }
    public PatchLink(string from, string to) { From = from; To = to; }
}
