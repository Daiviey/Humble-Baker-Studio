namespace HumbleBakerStudio.Audio;


public class GradientPanel : Panel {
    public Color TopColor = Color.FromArgb(36, 39, 51);
    public Color BottomColor = Color.FromArgb(18, 20, 28);
    protected override void OnPaint(PaintEventArgs e) {
        using (var b = new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle, TopColor, BottomColor, 90f)) e.Graphics.FillRectangle(b, ClientRectangle);
    }
}
