using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

/// <summary>
/// The instrument and one-shot library: saved synth patches, morphs, spectral canvases and the
/// sampled Foley / Orchestral / Guitar libraries, plus the drum kits. It reads the same instrument
/// JSON files the PowerShell studio writes, so the two apps share one library.
/// </summary>
internal sealed class LibraryTab : UserControl
{
    private readonly MainForm _host;
    private readonly TextBox _search = new() { Width = 220, PlaceholderText = "Search instruments and sounds" };
    private readonly ComboBox _category = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ListView _list = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = true,
        HideSelection = false,
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Back,
        ForeColor = Theme.Text
    };
    private readonly Label _status = new()
    {
        Dock = DockStyle.Bottom,
        Height = 26,
        Padding = new Padding(10, 5, 4, 0),
        ForeColor = Theme.Muted,
        Font = Theme.Small,
        BackColor = Theme.Panel
    };
    private readonly AudioPlayer _audition = new();

    private readonly List<InstrumentDef> _defs = new();
    private bool _loaded;

    public LibraryTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Theme.Panel,
            Padding = new Padding(10, 8, 6, 4),
            WrapContents = false
        };
        bar.Controls.Add(_search);
        _category.Items.AddRange(new object[] { "All", "Patches", "Spectral", "Orchestral", "Guitar", "Foley", "Samples", "Drum kits" });
        _category.SelectedIndex = 0;
        bar.Controls.Add(_category);
        bar.Controls.Add(Theme.Button("Audition", (_, _) => AuditionSelected(), false, "Preview the selected sound"));
        bar.Controls.Add(Theme.Button("Add as track", (_, _) => AddSelected(), true, "Create tracks using the selected sounds"));
        bar.Controls.Add(Theme.Button("Refresh", (_, _) => Reload()));

        _list.Columns.Add("Name", 300);
        _list.Columns.Add("Type", 90);
        _list.Columns.Add("Role", 100);
        _list.Columns.Add("Library", 120);
        _list.Columns.Add("Notes", 520);

        Controls.Add(_list);
        Controls.Add(_status);
        Controls.Add(bar);
        Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(12, 7, 0, 0),
            Text = "INSTRUMENT AND SOUND LIBRARY",
            ForeColor = Theme.Accent,
            Font = Theme.Section,
            BackColor = Theme.Panel
        });

        _search.TextChanged += (_, _) => ApplyFilter();
        _category.SelectedIndexChanged += (_, _) => ApplyFilter();
        _list.DoubleClick += (_, _) => AddSelected();
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.Handled = true; AddSelected(); }
        };

        ApplyFilter();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible && !_loaded) Reload();
    }

    public void RefreshView()
    {
        if (Visible) Reload();
    }

    private void Reload()
    {
        _loaded = true;
        _defs.Clear();

        var roots = InstrumentLibrary.FindRoots(SampleLibrary.RootPaths);
        _defs.AddRange(InstrumentLibrary.LoadAll(roots));

        var seenKits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string kit in SampleLibrary.BuiltInKits)
        {
            if (!seenKits.Add(kit)) continue;
            bool sampled = SampleLibrary.RootPaths.Any(r =>
            {
                try { return Directory.Exists(Path.Combine(r, "DrumKits", kit)); } catch { return false; }
            });
            _defs.Add(new InstrumentDef
            {
                Name = kit,
                Kind = "kit",
                Role = "percussion",
                CategoryOverride = "Drum kits",
                Notes = sampled ? "Sampled drum kit from DrumKits" : "Procedural drum kit"
            });
        }
        foreach (string root in SampleLibrary.RootPaths)
        {
            try
            {
                string dk = Path.Combine(root, "DrumKits");
                if (!Directory.Exists(dk)) continue;
                foreach (string dir in Directory.GetDirectories(dk))
                {
                    string name = Path.GetFileName(dir);
                    if (!seenKits.Add(name)) continue;
                    _defs.Add(new InstrumentDef
                    {
                        Name = name,
                        Kind = "kit",
                        Role = "percussion",
                        CategoryOverride = "Drum kits",
                        Notes = "Sampled drum kit from " + pathTail(root)
                    });
                }
            }
            catch { }
        }

        var sorted = _defs.OrderBy(d => d.Category == "Drum kits" ? 0 : 1)
                          .ThenBy(d => d.Category, StringComparer.OrdinalIgnoreCase)
                          .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                          .ToList();
        _defs.Clear();
        _defs.AddRange(sorted);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string text = _search.Text.Trim();
        string category = _category.SelectedItem as string ?? "All";
        var shown = _defs.Where(d =>
        {
            if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(d.Category, category, StringComparison.OrdinalIgnoreCase)) return false;
            if (text.Length == 0) return true;
            return Contains(d.Name, text) || Contains(d.Role, text) || Contains(d.Category, text) ||
                   Contains(d.TypeLabel, text) || Contains(d.Notes, text);
        }).ToList();

        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var def in shown)
            {
                var item = new ListViewItem(def.Name) { Tag = def };
                item.SubItems.Add(def.Kind == "kit" ? "drum kit" : def.TypeLabel);
                item.SubItems.Add(def.Role);
                item.SubItems.Add(def.Category);
                item.SubItems.Add(def.Notes);
                if (def.Category is "Foley" or "Orchestral" or "Guitar" or "Samples") item.ForeColor = Theme.Text;
                _list.Items.Add(item);
            }
        }
        finally
        {
            _list.EndUpdate();
        }
        _status.Text = $"{shown.Count} of {_defs.Count} instruments and sounds";
    }

    private static bool Contains(string? haystack, string needle) =>
        !string.IsNullOrEmpty(haystack) && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    private static string pathTail(string root)
    {
        try
        {
            var parts = root.TrimEnd('\\').Split('\\');
            return parts.Length >= 2 ? parts[^2] + "\\" + parts[^1] : root;
        }
        catch { return root; }
    }

    private List<InstrumentDef> SelectedDefs()
    {
        var picked = new List<InstrumentDef>();
        foreach (ListViewItem item in _list.SelectedItems)
            if (item.Tag is InstrumentDef def) picked.Add(def);
        return picked;
    }

    private void AddSelected()
    {
        var picked = SelectedDefs();
        if (picked.Count == 0) { SetStatus("Select a sound first."); return; }

        _host.BeginEdit("add instrument");
        Track? last = null;
        int added = 0;
        var errors = new List<string>();
        foreach (var def in picked)
        {
            Track track;
            if (def.Kind == "kit")
            {
                track = ProjectStore.NewTrack(def.Name, "drums");
                track.DrumKit = def.Name;
            }
            else
            {
                track = ProjectStore.NewTrack(def.Name, def.Kind is "patch" or "morph" ? "patch" : "sample");
                if (!InstrumentLibrary.Apply(def, track, SampleLibrary.RootPaths, out string error))
                {
                    errors.Add(def.Name + ": " + error);
                    continue;
                }
            }
            _host.Project.Tracks.Add(track);
            last = track;
            added++;
        }

        if (last != null)
        {
            _host.NotifyTracksChanged(last);
            _host.MarkDirty();
        }
        SetStatus(added == 0
            ? (errors.Count > 0 ? errors[0] : "Nothing was added.")
            : $"Added {added} track{(added == 1 ? "" : "s")} from the library." + (errors.Count > 0 ? " " + errors[0] : ""));
    }

    private void AuditionSelected()
    {
        var def = SelectedDefs().FirstOrDefault();
        if (def == null) { SetStatus("Select a sound first."); return; }

        try
        {
            var track = new Track { Name = def.Name, Vol = 0.8 };
            if (def.Kind == "kit")
            {
                track.Synth = "drums";
                track.DrumKit = def.Name;
                track.Notes.Add(new Note(36, 0, 0.25, 112));
                track.Notes.Add(new Note(42, 0.5, 0.25, 90));
                track.Notes.Add(new Note(38, 1.0, 0.25, 112));
                track.Notes.Add(new Note(42, 1.5, 0.25, 90));
            }
            else
            {
                if (!InstrumentLibrary.Apply(def, track, SampleLibrary.RootPaths, out string error))
                {
                    SetStatus(error);
                    return;
                }
                int pitch = def.Kind is "sample" or "spectral" ? Math.Clamp(def.Root, 0, 127) : 60;
                track.Notes.Add(new Note(pitch, 0, def.Kind is "sample" or "spectral" ? 4.0 : 2.0, 108));
            }

            var preview = new Project
            {
                Bpm = _host.Project.Bpm,
                BeatsPerBar = _host.Project.BeatsPerBar,
                BeatUnit = _host.Project.BeatUnit,
                Bars = 1,
                PhraseBars = 1,
                Swing = 0
            };
            preview.Tracks.Add(track);
            var pcm = Dsp.Render(preview, loop: true, master: false);
            if (pcm.Length < 4) { SetStatus("That sound is silent."); return; }
            _audition.Play(pcm, Dsp.SR);
            SetStatus("Auditioning " + def.Name + ".");
        }
        catch (Exception ex)
        {
            SetStatus("Could not audition: " + ex.Message);
        }
    }

    private void SetStatus(string text) => _status.Text = text;

    protected override void Dispose(bool disposing)
    {
        if (disposing) _audition.Dispose();
        base.Dispose(disposing);
    }
}
