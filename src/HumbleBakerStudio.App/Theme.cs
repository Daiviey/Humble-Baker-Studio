using System.Runtime.InteropServices;

namespace HumbleBakerStudio;

/// <summary>Single place for the look and feel, so every tab stays consistent.</summary>
internal static class Theme
{
    public static readonly Color Back = Color.FromArgb(16, 18, 24);
    public static readonly Color Panel = Color.FromArgb(24, 27, 35);
    public static readonly Color PanelAlt = Color.FromArgb(31, 35, 46);
    public static readonly Color Card = Color.FromArgb(35, 40, 53);
    public static readonly Color CardAlt = Color.FromArgb(40, 46, 61);
    public static readonly Color Border = Color.FromArgb(54, 61, 78);
    public static readonly Color Hover = Color.FromArgb(46, 53, 68);
    public static readonly Color Selection = Color.FromArgb(46, 70, 110);
    public static readonly Color Text = Color.FromArgb(234, 238, 246);
    public static readonly Color Muted = Color.FromArgb(150, 161, 182);
    public static readonly Color Accent = Color.FromArgb(108, 170, 255);
    public static readonly Color AccentSoft = Color.FromArgb(56, 88, 138);
    public static readonly Color AccentWarm = Color.FromArgb(255, 170, 96);
    public static readonly Color Good = Color.FromArgb(104, 222, 150);
    public static readonly Color Bad = Color.FromArgb(242, 108, 120);

    public static readonly Font Ui = new("Segoe UI", 9f);
    public static readonly Font UiBold = new("Segoe UI Semibold", 9f);
    public static readonly Font Small = new("Segoe UI", 8.25f);
    public static readonly Font Title = new("Segoe UI Semibold", 12f);
    public static readonly Font Section = new("Segoe UI Semibold", 8.5f);
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
                Apply(c);
            }
            else if (c is TextBoxBase tb)
            {
                tb.BackColor = PanelAlt;
                tb.ForeColor = Text;
                tb.BorderStyle = BorderStyle.FixedSingle;
                continue;
            }
            else if (c is SimpleList sl)
            {
                sl.BackColor = PanelAlt;
                sl.ForeColor = Text;
                continue;
            }
            else if (c is ListBox or CheckedListBox)
            {
                c.BackColor = PanelAlt;
                c.ForeColor = Text;
                continue;
            }
            else if (c is NumericUpDown nud)
            {
                nud.BackColor = PanelAlt;
                nud.ForeColor = Text;
                nud.BorderStyle = BorderStyle.FixedSingle;
                continue;
            }
            else if (c is ComboBox cb)
            {
                cb.BackColor = PanelAlt;
                cb.ForeColor = Text;
                cb.FlatStyle = FlatStyle.Flat;
                continue;
            }
            else if (c is ScrollBar sb)
            {
                sb.BackColor = PanelAlt;
                sb.ForeColor = Text;
                continue;
            }
            else if (c is CardPanel card)
            {
                card.BackColor = Card;
                Apply(c);
            }
            else if (c is MenuStrip or StatusStrip)
            {
                c.BackColor = Panel;
                c.ForeColor = Text;
                continue;
            }
            else if (c is Button)
            {
                continue;
            }
            else if (c is Label lbl)
            {
                if (lbl.ForeColor == SystemColors.ControlText || lbl.ForeColor == Color.Black) lbl.ForeColor = Text;
                continue;
            }
            else if (c is CheckBox or RadioButton)
            {
                c.BackColor = InCard(c) ? Card : Back;
                c.ForeColor = Text;
                continue;
            }
            else if (c is System.Windows.Forms.Panel or FlowLayoutPanel or TableLayoutPanel)
            {
                c.BackColor = InCard(c) ? Card : Back;
                Apply(c);
            }
            else
            {
                Apply(c);
            }
        }
    }

    private static bool InCard(Control control)
    {
        for (var p = control.Parent; p != null; p = p.Parent)
            if (p is CardPanel) return true;
        return false;
    }

    public static Button Button(string text, EventHandler? click = null, bool primary = false, string? tooltip = null)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? AccentSoft : PanelAlt,
            ForeColor = Text,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(3),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderColor = primary ? Accent : Border;
        b.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(72, 110, 170) : Hover;
        b.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(46, 76, 124) : CardAlt;
        if (click != null) b.Click += click;
        if (!string.IsNullOrWhiteSpace(tooltip)) new ToolTip().SetToolTip(b, tooltip);
        return b;
    }

    public static Label Label(string text, Color? color = null, Font? font = null) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = color ?? Text,
        Font = font ?? Ui,
        Margin = new Padding(3, 7, 3, 3)
    };

    public static Label SectionLabel(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        AutoSize = true,
        ForeColor = Accent,
        Font = Section,
        Margin = new Padding(0, 12, 0, 4)
    };

    public static Panel Header(string text)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Panel, Padding = new Padding(14, 8, 12, 6) };
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

    /// <summary>A bordered card used to group related controls.</summary>
    public static CardPanel CardPanel(string? title = null, int padding = 12)
    {
        var card = new CardPanel { BackColor = Card, Padding = new Padding(padding), Margin = new Padding(0, 0, 0, 10) };
        if (!string.IsNullOrEmpty(title))
        {
            card.Controls.Add(new Label
            {
                Text = title.ToUpperInvariant(),
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Accent,
                Font = Section
            });
        }
        return card;
    }

    public static void StyleTabs(TabControl tabs)
    {
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode = TabSizeMode.Normal;
        tabs.Padding = new Point(14, 5);
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
            TextRenderer.DrawText(g, tabs.TabPages[e.Index].Text, selected ? UiBold : Ui, r, fore.Color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        };
    }

    public static void StyleMenu(MenuStrip menu)
    {
        menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
        menu.BackColor = Panel;
        menu.ForeColor = Text;
        menu.Font = Ui;
        menu.Padding = new Padding(4, 2, 4, 2);
        menu.GripStyle = ToolStripGripStyle.Hidden;
        foreach (ToolStripItem item in menu.Items) StyleMenuItem(item);
    }

    private static void StyleMenuItem(ToolStripItem item)
    {
        item.ForeColor = Text;
        if (item is ToolStripMenuItem mi)
            foreach (ToolStripItem child in mi.DropDownItems) StyleMenuItem(child);
    }

    public static void StyleStatus(StatusStrip strip)
    {
        strip.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
        strip.BackColor = Panel;
        strip.ForeColor = Text;
        strip.Font = Ui;
        strip.SizingGrip = false;
        strip.Padding = new Padding(8, 2, 8, 2);
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

    /// <summary>Darkens native scrollbars on Windows 10/11 (best effort).</summary>
    public static void ApplyDarkScrollbars(Control root)
    {
        foreach (Control c in root.Controls)
        {
            try
            {
                if (c.IsHandleCreated && (c is ScrollBar || c is ListBox || c is TextBoxBase || c is SimpleList))
                    SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
            }
            catch
            {
                // Theme APIs vary between Windows builds.
            }
            ApplyDarkScrollbars(c);
        }
    }
}

/// <summary>A panel that paints a one-pixel border, used for grouped cards.</summary>
internal sealed class CardPanel : Panel
{
    public Color BorderColor = Theme.Border;

    public CardPanel()
    {
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

internal sealed class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => Theme.Selection;
    public override Color MenuItemSelectedGradientBegin => Theme.Selection;
    public override Color MenuItemSelectedGradientEnd => Theme.Selection;
    public override Color MenuItemBorder => Theme.Accent;
    public override Color MenuBorder => Theme.Border;
    public override Color MenuItemPressedGradientBegin => Theme.Card;
    public override Color MenuItemPressedGradientEnd => Theme.Card;
    public override Color ToolStripDropDownBackground => Theme.PanelAlt;
    public override Color ImageMarginGradientBegin => Theme.PanelAlt;
    public override Color ImageMarginGradientMiddle => Theme.PanelAlt;
    public override Color ImageMarginGradientEnd => Theme.PanelAlt;
    public override Color SeparatorDark => Theme.Border;
    public override Color SeparatorLight => Theme.Border;
    public override Color ToolStripGradientBegin => Theme.Panel;
    public override Color ToolStripGradientMiddle => Theme.Panel;
    public override Color ToolStripGradientEnd => Theme.Panel;
    public override Color ToolStripBorder => Theme.Border;
    public override Color StatusStripGradientBegin => Theme.Panel;
    public override Color StatusStripGradientEnd => Theme.Panel;
    public override Color ButtonSelectedHighlight => Theme.Selection;
    public override Color ButtonSelectedBorder => Theme.Accent;
    public override Color ButtonPressedHighlight => Theme.CardAlt;
    public override Color ButtonPressedBorder => Theme.Accent;
}
