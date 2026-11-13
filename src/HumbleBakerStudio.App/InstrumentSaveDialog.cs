namespace HumbleBakerStudio;

/// <summary>Small modal used by the spectral and meta tabs when saving into the shared instrument library.</summary>
internal sealed class InstrumentSaveDialog : Form
{
    private readonly TextBox _name = new() { Width = 250 };
    private readonly ComboBox _role = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _octave = new() { Minimum = 0, Maximum = 8, Value = 0, Width = 64 };
    private readonly NumericUpDown _root = new() { Minimum = 0, Maximum = 127, Value = 60, Width = 64 };
    private readonly CheckBox _gate = new() { Text = "Gate to note length", AutoSize = true, Checked = true };
    private readonly TextBox _notes = new() { Width = 250, Height = 54, Multiline = true, ScrollBars = ScrollBars.Vertical };

    public string InstrumentName => _name.Text.Trim();
    public string Role => _role.SelectedItem as string ?? "any";
    public int Octave => (int)_octave.Value;
    public int Root => (int)_root.Value;
    public bool Gate => _gate.Checked;
    public string Notes => _notes.Text.Trim();

    public InstrumentSaveDialog(string title, string defaultName, bool showRoot = true, bool showGate = true)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Width = 430;
        Height = 360;
        BackColor = Theme.Panel;
        ForeColor = Theme.Text;

        _name.Text = defaultName;
        _role.Items.AddRange(new object[] { "any", "lead", "bass", "arpeggio", "pad", "counter", "hook", "texture", "percussion" });
        _role.SelectedIndex = 0;

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(14),
            BackColor = Theme.Panel
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        void AddRow(string label, Control control)
        {
            body.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(3, 8, 3, 3) });
            body.Controls.Add(control);
        }

        AddRow("Name", _name);
        AddRow("Role", _role);
        AddRow("Octave", _octave);
        if (showRoot) AddRow("Root (spectral)", _root);
        if (showGate) AddRow("", _gate);
        AddRow("Notes for the AI", _notes);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Theme.Panel
        };
        var save = Theme.Button("Save", (_, _) => { DialogResult = DialogResult.OK; }, true);
        var cancel = Theme.Button("Cancel", (_, _) => { DialogResult = DialogResult.Cancel; });
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        Controls.Add(body);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
        Theme.Apply(this);
    }
}
