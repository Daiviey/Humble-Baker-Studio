namespace HumbleBakerStudio.Audio;

// One step of the song order: a pattern played Repeats times. Bars overrides the pattern's
// own length for this entry (0 = use the pattern's bars), so one pattern can appear as a
// 2-bar fill and an 8-bar section.
public class SongItem
{
    public int Pattern; public int Repeats = 1; public int Bars;
    public SongItem() { }
    public SongItem(int p, int r) { Pattern = p; Repeats = r; }
    public SongItem(int p, int r, int bars) { Pattern = p; Repeats = r; Bars = bars; }
    public int EffectiveBars(int patternBars) { return Bars > 0 ? Math.Max(1, Math.Min(128, Bars)) : Math.Max(1, patternBars); }
}
