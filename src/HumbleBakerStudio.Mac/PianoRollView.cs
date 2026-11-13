using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using HumbleBakerStudio.Audio;

namespace HumbleBakerStudio.Mac;

/// <summary>
/// Lightweight piano roll for the macOS shell. Click to add a 1/2-beat note at a 1/16 grid,
/// right-click to delete. It draws the selected track only, plus the transport playhead.
/// </summary>
public sealed class PianoRollView : Control
{
    private const double PxPerBeat = 52;
    private const double RowHeight = 12;
    private const int LowPitch = 24;
    private const int HighPitch = 96;

    private static readonly IBrush Background = new SolidColorBrush(Color.Parse("#101218"));
    private static readonly IBrush RowBrush = new SolidColorBrush(Color.Parse("#181B23"));
    private static readonly IBrush OctaveBrush = new SolidColorBrush(Color.Parse("#222734"));
    private static readonly IBrush BeatBrush = new SolidColorBrush(Color.Parse("#2A3040"));
    private static readonly IBrush BarBrush = new SolidColorBrush(Color.Parse("#3A4358"));
    private static readonly IBrush NoteBrush = new SolidColorBrush(Color.Parse("#6CAFFF"));
    private static readonly IBrush NoteEdgeBrush = new SolidColorBrush(Color.Parse("#B9DEFF"));
    private static readonly IBrush PlayheadBrush = new SolidColorBrush(Color.Parse("#FF8A5C"));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#96A1B6"));
    private static readonly Typeface Face = new("Inter");

    private Project? _project;
    private Track? _track;
    private double _playhead = -1;

    public event Action? Changed;

    public Project? Project
    {
        get => _project;
        set { _project = value; InvalidateMeasure(); InvalidateVisual(); }
    }

    public Track? Track
    {
        get => _track;
        set { _track = value; InvalidateVisual(); }
    }

    public double PlayheadBeats
    {
        get => _playhead;
        set { _playhead = value; InvalidateVisual(); }
    }

    private double TotalBeats => _project?.LoopBeats ?? 16;

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = TotalBeats * PxPerBeat + 40;
        if (!double.IsInfinity(availableSize.Width) && availableSize.Width > width) width = availableSize.Width;
        return new Size(width, (HighPitch - LowPitch + 1) * RowHeight);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Background, bounds);

        int rows = HighPitch - LowPitch + 1;
        for (int r = 0; r < rows; r++)
        {
            int pitch = HighPitch - r;
            var brush = pitch % 12 == 0 ? OctaveBrush : RowBrush;
            context.FillRectangle(brush, new Rect(0, r * RowHeight, bounds.Width, RowHeight));
        }

        double totalBeats = TotalBeats;
        int beatsPerBar = Math.Max(1, _project?.BeatsPerBar ?? 4);
        for (double beat = 0; beat <= totalBeats; beat += 0.25)
        {
            double x = beat * PxPerBeat;
            bool bar = Math.Abs(beat % beatsPerBar) < 0.001;
            bool whole = Math.Abs(beat - Math.Round(beat)) < 0.001;
            if (!bar && !whole) continue;
            var pen = new Pen(bar ? BarBrush : BeatBrush, bar ? 1.5 : 1);
            context.DrawLine(pen, new Point(x, 0), new Point(x, bounds.Height));
        }

        if (_track != null)
        {
            foreach (var note in _track.Notes)
            {
                double x = note.Start * PxPerBeat;
                double w = Math.Max(4, note.Len * PxPerBeat);
                double y = (HighPitch - note.Pitch) * RowHeight + 1;
                var rect = new Rect(x + 1, y, w - 2, RowHeight - 3);
                context.FillRectangle(NoteBrush, rect, 2);
                context.DrawRectangle(null, new Pen(NoteEdgeBrush, 1), rect, 2, 2);
            }
        }

        // Bar numbers.
        for (int bar = 0; bar * beatsPerBar <= totalBeats; bar++)
        {
            var text = new FormattedText((bar + 1).ToString(CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, 9, TextBrush);
            context.DrawText(text, new Point(bar * beatsPerBar * PxPerBeat + 3, 2));
        }

        if (_playhead >= 0 && totalBeats > 0)
        {
            double x = Math.Clamp(_playhead, 0, totalBeats) * PxPerBeat;
            context.DrawLine(new Pen(PlayheadBrush, 2), new Point(x, 0), new Point(x, bounds.Height));
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_track == null || _project == null) return;

        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;
        double beat = Math.Clamp(Math.Round(point.X / PxPerBeat * 4) / 4, 0, Math.Max(0, TotalBeats - 0.25));
        int pitch = HighPitch - (int)(point.Y / RowHeight);
        pitch = Math.Clamp(pitch, LowPitch, HighPitch);

        if (properties.IsRightButtonPressed)
        {
            var hit = _track.Notes.LastOrDefault(n =>
                beat >= n.Start && beat <= n.Start + n.Len && n.Pitch == pitch);
            if (hit != null)
            {
                _track.Notes.Remove(hit);
                InvalidateVisual();
                Changed?.Invoke();
            }
            return;
        }

        if (properties.IsLeftButtonPressed)
        {
            _track.Notes.Add(new Note(pitch, beat, 0.5, 100));
            InvalidateVisual();
            Changed?.Invoke();
        }
    }
}
