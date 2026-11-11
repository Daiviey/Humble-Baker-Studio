using HumbleBakerStudio.Audio;
using HumbleBakerStudio.Services;

namespace HumbleBakerStudio.Tabs;

internal sealed class MasterTab : UserControl
{
    private readonly MainForm _host;
    private readonly FlowLayoutPanel _knobs = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10), BackColor = Theme.Back };
    private readonly Label _heading = new() { Dock = DockStyle.Top, Height = 30, Padding = new Padding(12, 7, 0, 0), ForeColor = Theme.Text, Font = Theme.UiBold, BackColor = Theme.Panel };
    private readonly TextBox _report = new()
    {
        Dock = DockStyle.Bottom,
        Height = 96,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        BackColor = Theme.PanelAlt,
        ForeColor = Theme.Text,
        Font = Theme.Mono,
        BorderStyle = BorderStyle.FixedSingle
    };
    private readonly Label _state = new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(6, 8, 8, 3) };
    private bool _building;

    public MasterTab(MainForm host)
    {
        _host = host;
        BackColor = Theme.Back;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Panel, Padding = new Padding(10, 7, 6, 4), WrapContents = false };
        bar.Controls.Add(Theme.Button("Analyze", (_, _) => Analyze()));
        bar.Controls.Add(Theme.Button("Auto master", (_, _) => AutoMaster()));
        bar.Controls.Add(Theme.Button("AI advice", (_, _) => AiAdvice()));
        bar.Controls.Add(Theme.Button("Reset", (_, _) => Reset()));
        bar.Controls.Add(_state);

        Controls.Add(_knobs);
        Controls.Add(bar);
        Controls.Add(_heading);
        Controls.Add(_report);
    }

    public void RefreshView()
    {
        _building = true;
        try
        {
            _heading.Text = "Mastering — song output chain";
            var m = _host.Project.Master;
            _state.Text = m.Enabled ? "Master chain: on" : "Master chain: off";
            KnobPanel.Build(m, _knobs, (_, _) =>
            {
                if (_building) return;
                _host.BeginEdit("master");
                _state.Text = m.Enabled ? "Master chain: on" : "Master chain: off";
                _host.MarkDirty();
            }, Theme.Back, Theme.Text, Theme.Accent);
            _knobs.AutoScrollPosition = new Point(0, 0);
        }
        finally
        {
            _building = false;
        }
    }

    private void Analyze()
    {
        try
        {
            _report.Text = "Rendering for analysis…";
            var project = _host.Project;
            Task.Run(() =>
            {
                var (l, r) = ExportService.Render(project, song: false, loop: true, master: false);
                return Mastering.Analyze(l, r).Report();
            }).ContinueWith(t =>
            {
                if (IsDisposed) return;
                BeginInvoke(() => _report.Text = t.IsFaulted ? "Analysis failed: " + t.Exception?.GetBaseException().Message : t.Result);
            });
        }
        catch (Exception ex)
        {
            _report.Text = "Analysis failed: " + ex.Message;
        }
    }

    private void AutoMaster()
    {
        try
        {
            _state.Text = "Auto master: rendering…";
            var project = _host.Project;
            double target = project.Master.TargetLufs;
            Task.Run(() =>
            {
                var (l, r) = ExportService.Render(project, song: false, loop: true, master: false);
                return Mastering.Auto(l, r, target);
            }).ContinueWith(t =>
            {
                if (IsDisposed) return;
                BeginInvoke(() =>
                {
                    if (t.IsFaulted)
                    {
                        _state.Text = "Auto master failed";
                        _report.Text = "Auto master failed: " + t.Exception?.GetBaseException().Message;
                        return;
                    }
                    Copy(t.Result, project.Master);
                    _host.BeginEdit("master");
                    _host.MarkDirty();
                    RefreshView();
                    _state.Text = "Auto master: applied";
                    Analyze();
                });
            });
        }
        catch (Exception ex)
        {
            _report.Text = "Auto master failed: " + ex.Message;
        }
    }

    private async void AiAdvice()
    {
        try
        {
            _state.Text = "AI: thinking…";
            var project = _host.Project;
            _host.BeginEdit("master");
            var (advice, applied) = await _host.Ai.AdviseMasterAsync(project, project.Master, CancellationToken.None);
            _report.Text = advice + (applied > 0 ? $"\n\nApplied {applied} suggested setting(s)." : "\n\nNo settings were changed.");
            _host.MarkDirty();
            RefreshView();
            _state.Text = "AI advice: done";
        }
        catch (Exception ex)
        {
            _state.Text = "AI advice failed";
            _report.Text = ex.Message;
        }
    }

    private void Reset()
    {
        _host.BeginEdit("master");
        var fresh = new MasterSettings();
        Copy(fresh, _host.Project.Master);
        _host.MarkDirty();
        RefreshView();
    }

    private static void Copy(MasterSettings from, MasterSettings to)
    {
        foreach (var pi in typeof(MasterSettings).GetProperties())
            if (pi.CanWrite && pi.GetIndexParameters().Length == 0 && pi.Name != "TargetLufs")
                pi.SetValue(to, pi.GetValue(from));
    }
}
