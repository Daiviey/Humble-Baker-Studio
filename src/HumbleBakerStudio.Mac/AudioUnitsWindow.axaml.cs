using Avalonia.Controls;

namespace HumbleBakerStudio.Mac;

public partial class AudioUnitsWindow : Window
{
    public AudioUnitsWindow()
    {
        InitializeComponent();

        if (!OperatingSystem.IsMacOS())
        {
            Status.Text = "Audio Units are only available on macOS.";
            return;
        }

        try
        {
            var units = AudioUnits.List();
            UnitList.ItemsSource = units.Select(u => $"{u.Name}  -  {u.Kind}  ({u.Manufacturer})").ToList();
            Status.Text = units.Count == 0
                ? "No Audio Units were reported by AudioToolbox."
                : $"{units.Count} Audio Unit(s) found.";
        }
        catch (Exception ex)
        {
            Status.Text = "Audio Unit scan failed: " + ex.Message;
        }
    }
}
