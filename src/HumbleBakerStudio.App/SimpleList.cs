namespace HumbleBakerStudio;

/// <summary>
/// A small owner-painted list. Native ListBox item rendering is inconsistent under the
/// studio's dark theme and DPI settings, so the tab editors use this instead.
/// </summary>
internal sealed class SimpleList : ScrollableControl
{
    private readonly List<string> _items = new();

    public SimpleList()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        DoubleBuffered = true;
        BackColor = Theme.PanelAlt;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        AutoScroll = true;
        TabStop = true;
        Cursor = Cursors.Hand;
    }

    public IReadOnlyList<string> Items => _items;
    public int ItemHeight { get; set; } = 22;

    private int _selected = -1;
    public int SelectedIndex
    {
        get => _selected;
        set
        {
            int v = _items.Count == 0 ? -1 : Math.Max(-1, Math.Min(value, _items.Count - 1));
            if (v == _selected) return;
            _selected = v;
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }

    public string? SelectedItem => _selected >= 0 && _selected < _items.Count ? _items[_selected] : null;
    public event EventHandler? SelectedIndexChanged;

    public void BeginUpdate() { }
    public void EndUpdate() => Invalidate();

    public void Clear()
    {
        _items.Clear();
        _selected = -1;
        AutoScrollMinSize = Size.Empty;
        Invalidate();
    }

    public void Add(string text)
    {
        _items.Add(text);
        UpdateScroll();
        Invalidate();
    }

    public void AddRange(IEnumerable<string> items)
    {
        _items.AddRange(items);
        UpdateScroll();
        Invalidate();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        _items.RemoveAt(index);
        if (_selected >= _items.Count) _selected = _items.Count - 1;
        UpdateScroll();
        Invalidate();
    }

    public new void Refresh() => Invalidate();

    private void UpdateScroll() => AutoScrollMinSize = new Size(0, Math.Max(0, _items.Count * ItemHeight));

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        using (var back = new SolidBrush(BackColor)) g.FillRectangle(back, ClientRectangle);
        int offset = AutoScrollPosition.Y;
        for (int i = 0; i < _items.Count; i++)
        {
            int y = i * ItemHeight + offset;
            if (y + ItemHeight < 0 || y > Height) continue;
            var row = new Rectangle(0, y, Width, ItemHeight);
            if (i == _selected)
                using (var sel = new SolidBrush(Theme.Border)) g.FillRectangle(sel, row);
            TextRenderer.DrawText(g, _items[i], Font, new Rectangle(6, y, Width - 10, ItemHeight), ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        int index = (e.Y - AutoScrollPosition.Y) / ItemHeight;
        SelectedIndex = index >= 0 && index < _items.Count ? index : -1;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_items.Count == 0) return;
        if (e.KeyCode == Keys.Down) { SelectedIndex = Math.Min(_items.Count - 1, _selected + 1); e.Handled = true; }
        else if (e.KeyCode == Keys.Up) { SelectedIndex = Math.Max(0, _selected - 1); e.Handled = true; }
    }
}
