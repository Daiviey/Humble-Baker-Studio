using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Mac;

public partial class MainWindow : Window
{
    private readonly MacAudioOutput _audio = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Stopwatch _clock = new();
    private Project _project = ProjectStore.CreateDefault();
    private string? _path;
    private double _loopSeconds;
    private bool _playing;

    public MainWindow()
    {
        InitializeComponent();

        TrackList.SelectionChanged += (_, _) =>
        {
            int index = TrackList.SelectedIndex;
            Roll.Track = index >= 0 && index < _project.Tracks.Count ? _project.Tracks[index] : null;
            LoadTrackValues();
        };
        Roll.Changed += () => Status.Text = "Note edited. Use Save to write the project.";
        _timer.Tick += (_, _) => UpdatePlayhead();
        Closing += (_, _) => _audio.Dispose();

        Volume.PropertyChanged += (_, e) =>
        {
            if (e.Property != Avalonia.Controls.Primitives.RangeBase.ValueProperty || _loadingTrack) return;
            var track = SelectedTrack;
            if (track == null) return;
            track.Vol = Volume.Value;
            Status.Text = $"Volume: {track.Vol:0.00}";
        };
        Pan.PropertyChanged += (_, e) =>
        {
            if (e.Property != Avalonia.Controls.Primitives.RangeBase.ValueProperty || _loadingTrack) return;
            var track = SelectedTrack;
            if (track == null) return;
            track.Pan = Pan.Value;
            Status.Text = $"Pan: {track.Pan:0.00}";
        };
        Mute.PropertyChanged += (_, e) =>
        {
            if (e.Property != Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty || _loadingTrack) return;
            var track = SelectedTrack;
            if (track == null) return;
            track.Mute = Mute.IsChecked == true;
            RefreshTracks();
        };
        BpmBox.LostFocus += (_, _) => ApplyBpm();
        Loaded += (_, _) => RefreshAuEffects();

        LoadProject(_project, null);
    }

    private void LoadProject(Project project, string? path)
    {
        StopPlayback();
        _project = project;
        _path = path;
        Roll.Project = project;
        RefreshTracks();
        BpmBox.Text = project.Bpm.ToString("0.##");
        UpdateInfo();
        Status.Text = path == null ? "Demo project loaded." : "Loaded " + Path.GetFileName(path);
    }

    private bool _loadingTrack;

    private Track? SelectedTrack
    {
        get
        {
            int index = TrackList.SelectedIndex;
            return index >= 0 && index < _project.Tracks.Count ? _project.Tracks[index] : null;
        }
    }

    private void RefreshTracks()
    {
        int selected = TrackList.SelectedIndex;
        TrackList.ItemsSource = _project.Tracks
            .Select(t => $"{t.Name}  ({t.Synth})")
            .ToList();
        TrackList.SelectedIndex = _project.Tracks.Count == 0
            ? -1
            : Math.Clamp(selected < 0 ? 0 : selected, 0, _project.Tracks.Count - 1);
        Roll.InvalidateVisual();
    }

    private void LoadTrackValues()
    {
        _loadingTrack = true;
        try
        {
            var track = SelectedTrack;
            if (track != null)
            {
                Volume.Value = track.Vol;
                Pan.Value = track.Pan;
                Mute.IsChecked = track.Mute;
            }
        }
        finally
        {
            _loadingTrack = false;
        }
    }

    private void ApplyBpm()
    {
        if (!double.TryParse(BpmBox.Text, out double bpm) || bpm < 20 || bpm > 300) return;
        _project.Bpm = bpm;
        UpdateInfo();
        Roll.InvalidateMeasure();
        Roll.InvalidateVisual();
        Status.Text = $"Tempo: {_project.Bpm:0.##} BPM";
    }

    private void UpdateInfo() =>
        ProjectInfo.Text = $"{_project.Bpm:0.##} BPM   {_project.Bars} bars   {_project.LoopBeats:0.##} beats";

    private void AddTrack_Click(object? sender, RoutedEventArgs e)
    {
        var track = ProjectStore.NewTrack("Track " + (_project.Tracks.Count + 1), "acid");
        _project.Tracks.Add(track);
        RefreshTracks();
        TrackList.SelectedIndex = _project.Tracks.Count - 1;
        Status.Text = "Added " + track.Name;
    }

    private void DeleteTrack_Click(object? sender, RoutedEventArgs e)
    {
        int index = TrackList.SelectedIndex;
        if (index < 0 || index >= _project.Tracks.Count) return;
        string name = _project.Tracks[index].Name;
        _project.Tracks.RemoveAt(index);
        RefreshTracks();
        Status.Text = "Deleted " + name;
    }

    private async void Play_Click(object? sender, RoutedEventArgs e)
    {
        if (_playing) return;
        _loopSeconds = _project.LoopBeats * 60.0 / Math.Max(1, _project.Bpm);
        if (_loopSeconds <= 0.01)
        {
            Status.Text = "The project loop is empty.";
            return;
        }

        PlayButton.IsEnabled = false;
        Status.Text = "Rendering the loop...";
        try
        {
            short[] pcm = await RenderLoop();
            try
            {
                _audio.Play(pcm, Dsp.SR, LoopBox.IsChecked == true);
            }
            catch (PlatformNotSupportedException)
            {
                Status.Text = "Transport preview: CoreAudio output is only available in the macOS build.";
            }
            catch (Exception ex)
            {
                Status.Text = "CoreAudio failed: " + ex.Message;
            }

            _clock.Restart();
            _timer.Start();
            _playing = true;
            Status.Text = $"Playing {_loopSeconds:0.##} s loop.";
        }
        catch (Exception ex)
        {
            Status.Text = "Render failed: " + ex.Message;
        }
        finally
        {
            PlayButton.IsEnabled = true;
        }
    }

    private void UpdatePlayhead()
    {
        if (!_playing || _loopSeconds <= 0)
        {
            Transport.Text = "";
            return;
        }

        double seconds = _clock.Elapsed.TotalSeconds % _loopSeconds;
        double beat = seconds / _loopSeconds * _project.LoopBeats;
        Roll.PlayheadBeats = beat;
        Transport.Text = $"{seconds:0.00} / {_loopSeconds:0.00} s";
    }

    private void Stop_Click(object? sender, RoutedEventArgs e) => StopPlayback();

    private void StopPlayback()
    {
        _timer.Stop();
        _clock.Stop();
        _playing = false;
        _audio.Stop();
        Roll.PlayheadBeats = -1;
        Transport.Text = "";
    }

    private async void Open_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a Humble Baker Studio project",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Humble Baker project")
                {
                    Patterns = new[] { "*.hbstudio.json", "*.json" }
                }
            }
        });
        if (files.Count == 0) return;

        string? path = files[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            Status.Text = "That file is not available on the local filesystem.";
            return;
        }

        try
        {
            var project = ProjectStore.Load(path);
            LoadProject(project, path);
        }
        catch (Exception ex)
        {
            Status.Text = "Open failed: " + ex.Message;
        }
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_path))
        {
            await SaveAs();
            return;
        }
        try
        {
            ProjectStore.Save(_project, _path);
            Status.Text = "Saved " + Path.GetFileName(_path);
        }
        catch (Exception ex)
        {
            Status.Text = "Save failed: " + ex.Message;
        }
    }

    private async Task SaveAs()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Humble Baker Studio project",
            SuggestedFileName = "project.hbstudio.json",
            DefaultExtension = "json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Humble Baker project")
                {
                    Patterns = new[] { "*.hbstudio.json", "*.json" }
                }
            }
        });
        if (file == null) return;

        string? path = file.TryGetLocalPath();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            ProjectStore.Save(_project, path);
            _path = path;
            Status.Text = "Saved " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            Status.Text = "Save failed: " + ex.Message;
        }
    }

    private async void Render_Click(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Render the loop to WAV",
            SuggestedFileName = "humble-baker-loop.wav",
            DefaultExtension = "wav",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("WAV audio") { Patterns = new[] { "*.wav" } }
            }
        });
        if (file == null) return;

        string? path = file.TryGetLocalPath();
        if (string.IsNullOrEmpty(path)) return;
        Status.Text = "Rendering...";
        try
        {
            short[] pcm = await RenderLoop();
            await Task.Run(() => Dsp.WriteWav(path, pcm));
            Status.Text = "Rendered " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            Status.Text = "Render failed: " + ex.Message;
        }
    }

    private void RefreshAuEffects()
    {
        var items = new List<object> { "No AU effect" };
        if (OperatingSystem.IsMacOS())
        {
            try
            {
                foreach (var unit in AudioUnits.List().Where(u => u.IsEffect))
                    items.Add(unit);
            }
            catch (Exception ex)
            {
                Status.Text = "AU scan failed: " + ex.Message;
            }
        }
        MasterFx.ItemsSource = items;
        MasterFx.SelectedIndex = 0;
    }

    private async Task<short[]> RenderLoop()
    {
        var effect = MasterFx.SelectedItem as AudioUnitInfo;
        return await Task.Run(() =>
        {
            var (left, right) = ExportService.Render(_project, song: false, loop: true, master: true);
            if (effect != null)
            {
                try
                {
                    using var unit = AudioUnitEffect.Create(effect, Dsp.SR);
                    unit.Process(left, right);
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() =>
                        Status.Text = "AU effect failed; using the dry mix: " + ex.Message);
                }
            }
            return ExportService.ToPcm(left, right, true);
        });
    }

    private void AudioUnits_Click(object? sender, RoutedEventArgs e) =>
        new AudioUnitsWindow().ShowDialog(this);
}
