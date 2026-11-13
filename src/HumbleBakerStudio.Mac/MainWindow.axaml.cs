using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Mac;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void RenderDemo_Click(object? sender, RoutedEventArgs e)
    {
        Status.Text = "Rendering the demo project...";
        try
        {
            string path = await Task.Run(() =>
            {
                var project = ProjectStore.CreateDefault();
                var pcm = ExportService.RenderPcm(project, song: false, loop: true, master: true);
                string output = Path.Combine(Path.GetTempPath(), "Humble Baker Demo.wav");
                Dsp.WriteWav(output, pcm);
                return output;
            });
            Status.Text = $"Rendered {path}";
        }
        catch (Exception ex)
        {
            Status.Text = "Render failed: " + ex.Message;
        }
    }

    private async void RenderProject_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open a Humble Baker Studio project",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Humble Baker project") { Patterns = new[] { "*.json", "*.hbstudio.json" } }
                }
            });
            if (files.Count == 0) return;

            string? input = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(input))
            {
                Status.Text = "That file is not available on the local filesystem.";
                return;
            }

            Status.Text = $"Rendering {Path.GetFileName(input)}...";
            string output = await Task.Run(() =>
            {
                var project = ProjectStore.Load(input);
                var pcm = ExportService.RenderPcm(project, song: false, loop: true, master: true);
                string wav = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(input) + "-render.wav");
                Dsp.WriteWav(wav, pcm);
                return wav;
            });
            Status.Text = $"Rendered {output}";
        }
        catch (Exception ex)
        {
            Status.Text = "Render failed: " + ex.Message;
        }
    }
}
