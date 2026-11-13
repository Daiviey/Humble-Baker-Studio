using HumbleBakerStudio.Tabs;

namespace HumbleBakerStudio.Tabs;

/// <summary>Groups the specialist editors so the main tab strip stays readable.</summary>
internal sealed class ToolsTab : UserControl
{
    private readonly SampleTab _sample;
    private readonly MidiTab _midi;
    private readonly SpectralTab _spectral;
    private readonly AutomationTab _automation;
    private readonly TabControl _inner = new() { Dock = DockStyle.Fill };

    public ToolsTab(MainForm host)
    {
        BackColor = Theme.Back;
        _sample = new SampleTab(host);
        _midi = new MidiTab(host);
        _spectral = new SpectralTab(host);
        _automation = new AutomationTab(host);

        _inner.TabPages.Add(Page("Sample lab", _sample));
        _inner.TabPages.Add(Page("MIDI input", _midi));
        _inner.TabPages.Add(Page("Spectral / Meta", _spectral));
        _inner.TabPages.Add(Page("Automation paint", _automation));
        Theme.StyleTabs(_inner);
        Controls.Add(_inner);
    }

    private static TabPage Page(string text, Control content)
    {
        var page = new TabPage(text) { BackColor = Theme.Back, Padding = new Padding(0) };
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        return page;
    }

    public void RefreshView()
    {
        _sample.RefreshView();
        _midi.RefreshView();
        _spectral.RefreshView();
        _automation.RefreshView();
    }

    public void Select(string pageTitle)
    {
        foreach (TabPage p in _inner.TabPages)
            if (string.Equals(p.Text, pageTitle, StringComparison.OrdinalIgnoreCase)) { _inner.SelectedTab = p; return; }
    }
}
