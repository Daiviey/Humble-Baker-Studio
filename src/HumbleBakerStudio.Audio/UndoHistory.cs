namespace HumbleBakerStudio.Audio;

/// <summary>
/// Snapshot-based undo/redo for a <see cref="Project"/>.
///
/// The model is serialisable, so a snapshot is just the project JSON. Records are coalesced by
/// label and time: dragging a knob produces one undo step rather than hundreds.
/// </summary>
public sealed class UndoHistory
{
    private readonly record struct Snapshot(string Label, string Json);

    private readonly Stack<Snapshot> _undo = new();
    private readonly Stack<Snapshot> _redo = new();
    private string? _baseline;
    private string? _lastLabel;
    private long _lastTick;

    public int Capacity { get; set; } = 80;
    public int CoalesceMilliseconds { get; set; } = 700;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoLabel => _undo.Count > 0 ? _undo.Peek().Label : null;
    public string? RedoLabel => _redo.Count > 0 ? _redo.Peek().Label : null;

    /// <summary>Starts a fresh history at the given project state (call after open/new/undo/redo).</summary>
    public void Reset(Project project)
    {
        _undo.Clear();
        _redo.Clear();
        _baseline = ProjectStore.SaveToJson(project);
        _lastLabel = null;
        _lastTick = 0;
    }

    /// <summary>
    /// Call after a mutation. The first call for a gesture captures the state from before it,
    /// because <see cref="_baseline"/> is only refreshed once a step is committed.
    /// </summary>
    public void Record(Project project, string label)
    {
        if (string.IsNullOrWhiteSpace(label)) label = "edit";
        var live = ProjectStore.SaveToJson(project);
        if (_baseline == null)
        {
            _baseline = live;
            return;
        }

        long now = Environment.TickCount64;
        if (_lastLabel == label && now - _lastTick < CoalesceMilliseconds)
        {
            _lastTick = now;
            return;
        }

        if (live == _baseline)
        {
            _lastLabel = label;
            _lastTick = now;
            return;
        }

        _undo.Push(new Snapshot(label, _baseline));
        while (_undo.Count > Capacity)
        {
            var keep = _undo.Reverse().Take(Capacity).Reverse().ToArray();
            _undo.Clear();
            foreach (var s in keep) _undo.Push(s);
        }
        _redo.Clear();
        _baseline = live;
        _lastLabel = label;
        _lastTick = now;
    }

    public Project? Undo(Project current)
    {
        if (_undo.Count == 0) return null;
        var step = _undo.Pop();
        _redo.Push(new Snapshot(step.Label, _baseline ?? ProjectStore.SaveToJson(current)));
        _baseline = step.Json;
        _lastLabel = null;
        return ProjectStore.LoadFromJson(step.Json);
    }

    public Project? Redo(Project current)
    {
        if (_redo.Count == 0) return null;
        var step = _redo.Pop();
        _undo.Push(new Snapshot(step.Label, _baseline ?? ProjectStore.SaveToJson(current)));
        _baseline = step.Json;
        _lastLabel = null;
        return ProjectStore.LoadFromJson(step.Json);
    }
}
