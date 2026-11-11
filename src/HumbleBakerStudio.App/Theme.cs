namespace HumbleBakerStudio;

/// <summary>Single place for the look and feel, so every tab stays consistent.</summary>
internal static class Theme
{
    public static readonly Color Back = Color.FromArgb(18, 20, 26);
    public static readonly Color Panel = Color.FromArgb(26, 29, 38);
    public static readonly Color PanelAlt = Color.FromArgb(32, 36, 47);
    public static readonly Color Border = Color.FromArgb(48, 53, 68);
    public static readonly Color Text = Color.FromArgb(226, 231, 240);
    public static readonly Color Muted = Color.FromArgb(148, 158, 178);
    public static readonly Color Accent = Color.FromArgb(120, 168, 255);
    public static readonly Color AccentWarm = Color.FromArgb(255, 168, 92);
    public static readonly Color Good = Color.FromArgb(110, 220, 150);
    public static readonly Color Bad = Color.FromArgb(240, 110, 120);

    public static readonly Font Ui = new("Segoe UI", 9f);
    public static readonly Font UiBold = new("Segoe UI Semibold", 9f);
    public static readonly Font Small = new("Segoe UI", 8.25f);
    public static readonly Font Title = new("Segoe UI Semibold", 12f);
    public static readonly Font Mono = new("Consolas", 9f);

    public static void Apply(Control root)
    {
        root.BackColor = Back;
        root.ForeColor = Text;
        root.Font = Ui;
        foreach (Control c in root.Controls)
        {
            if (c is TabControl tabs)
            {
                tabs.BackColor = Panel;
                tabs.ForeColor = Text;
            }
            if (c is TextBoxBase tb)
            {
                tb.BackColor = PanelAlt;
                tb.ForeColor = Text;
                tb.BorderStyle = BorderStyle.FixedSingle;
            }
            if (c is ListBox or CheckedListBox)
            {
                c.BackColor = PanelAlt;
                c.ForeColor = Text;
            }
            if (c is NumericUpDown nud)
            {
                nud.BackColor = PanelAlt;
                nud.ForeColor = Text;
                nud.BorderStyle = BorderStyle.FixedSingle;
            }
            if (c is ComboBox cb)
            {
                cb.BackColor = PanelAlt;
                cb.ForeColor = Text;
                cb.FlatStyle = FlatStyle.Flat;
            }
            if (c is ScrollBar sb)
            {
                sb.BackColor = PanelAlt;
                sb.ForeColor = Text;
            }
            Apply(c);
        }
    }

    public static Button Button(string text, EventHandler? click = null, bool primary = false)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(54, 92, 156) : PanelAlt,
            ForeColor = Text,
            Padding = new Padding(8, 3, 8, 3),
            Margin = new Padding(3),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = Border;
        b.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(68, 112, 186) : Border;
        if (click != null) b.Click += click;
        return b;
    }

    public static Label Label(string text, Color? color = null, Font? font = null) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = color ?? Text,
        Font = font ?? Ui,
        Margin = new Padding(3, 6, 3, 3)
    };

    public static Panel Header(string text)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Panel, Padding = new Padding(12, 8, 12, 6) };
        p.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Title,
            ForeColor = Text
        });
        return p;
    }

    public static void StyleTabs(TabControl tabs)
    {
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.ItemSize = new Size(98, 30);
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.Padding = new Point(0, 0);
        tabs.DrawItem += (_, e) =>
        {
            var g = e.Graphics;
            var r = tabs.GetTabRect(e.Index);
            bool selected = e.Index == tabs.SelectedIndex;
            using var back = new SolidBrush(selected ? PanelAlt : Back);
            using var fore = new SolidBrush(selected ? Accent : Muted);
            g.FillRectangle(back, r);
            if (selected)
            {
                using var underline = new Pen(Accent, 2f);
                g.DrawLine(underline, r.Left + 8, r.Bottom - 2, r.Right - 8, r.Bottom - 2);
            }
            TextRenderer.DrawText(g, tabs.TabPages[e.Index].Text, UiBold, r, fore.Color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        };
    }
}
