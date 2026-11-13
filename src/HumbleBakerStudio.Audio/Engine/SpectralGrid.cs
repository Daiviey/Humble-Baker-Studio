namespace HumbleBakerStudio.Audio;


public class SpectralGrid {
    public int Cols, Rows; public float[] A;
    public SpectralGrid(int cols, int rows) { Cols = Math.Max(8, Math.Min(4096, cols)); Rows = Math.Max(8, Math.Min(512, rows)); A = new float[Cols * Rows]; }
    public float Get(int c, int r) { return A[c * Rows + r]; }
    public void Clear() { Array.Clear(A, 0, A.Length); }

    // Soft round brush in cell coordinates. Row 0 is the lowest frequency.
    public void Dab(double cx, double cy, double radius, double amount, bool erase) {
        DabEllipse(cx, cy, radius, radius, amount, erase);
    }
    /// <summary>
    /// Elliptical brush. A 4096-column / 512-row canvas is stretched by the control, so the
    /// painter uses separate radii to keep the on-screen brush round at any resolution.
    /// </summary>
    public void DabEllipse(double cx, double cy, double rx, double ry, double amount, bool erase) {
        rx = Math.Max(0.5, rx); ry = Math.Max(0.5, ry);
        int x0 = Math.Max(0, (int)Math.Floor(cx - rx)), x1 = Math.Min(Cols - 1, (int)Math.Ceiling(cx + rx));
        int y0 = Math.Max(0, (int)Math.Floor(cy - ry)), y1 = Math.Min(Rows - 1, (int)Math.Ceiling(cy + ry));
        for (int c = x0; c <= x1; c++) for (int r = y0; r <= y1; r++) {
            double dx = (c - cx) / rx, dy = (r - cy) / ry;
            double d2 = dx * dx + dy * dy; if (d2 > 1) continue;
            float w = (float)(1 - d2); int i = c * Rows + r;
            if (erase) A[i] = A[i] * (1 - w * (float)Math.Min(1, amount * 1.5)); else A[i] = Math.Max(A[i], (float)amount * w);
        }
    }
    public void Line(double x0, double y0, double x1, double y1, double radius, double amount, bool erase) {
        LineEllipse(x0, y0, x1, y1, radius, radius, amount, erase);
    }
    public void LineEllipse(double x0, double y0, double x1, double y1, double rx, double ry, double amount, bool erase) {
        double len = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)); int n = Math.Max(1, (int)Math.Ceiling(len * 2));
        for (int i = 0; i <= n; i++) { double t = (double)i / n; DabEllipse(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, rx, ry, amount, erase); }
    }
    /// <summary>Bilinear resample used when the automation canvas resolution changes.</summary>
    public SpectralGrid Resample(int cols, int rows) {
        var dst = new SpectralGrid(cols, rows);
        if (dst.Cols == Cols && dst.Rows == Rows) { Array.Copy(A, dst.A, A.Length); return dst; }

        // Upsampling is nearest-neighbour so thin painted strokes keep their brightness;
        // downsampling is bilinear so the reduction does not alias.
        bool upsample = dst.Cols >= Cols && dst.Rows >= Rows;
        for (int c = 0; c < dst.Cols; c++) {
            double sx = dst.Cols <= 1 ? 0 : c / (double)(dst.Cols - 1) * (Cols - 1);
            int x0 = Math.Max(0, (int)Math.Floor(sx)), x1 = Math.Min(Cols - 1, x0 + 1); double fx = sx - x0;
            for (int r = 0; r < dst.Rows; r++) {
                if (upsample) {
                    int nc = Math.Min(Cols - 1, (int)Math.Round(sx));
                    int nr = Math.Min(Rows - 1, (int)Math.Round(r / (double)(dst.Rows - 1) * (Rows - 1)));
                    dst.A[c * dst.Rows + r] = A[nc * Rows + nr];
                    continue;
                }
                double sy = dst.Rows <= 1 ? 0 : r / (double)(dst.Rows - 1) * (Rows - 1);
                int y0 = Math.Max(0, (int)Math.Floor(sy)), y1 = Math.Min(Rows - 1, y0 + 1); double fy = sy - y0;
                double v = A[x0 * Rows + y0] * (1 - fx) * (1 - fy) + A[x1 * Rows + y0] * fx * (1 - fy)
                         + A[x0 * Rows + y1] * (1 - fx) * fy + A[x1 * Rows + y1] * fx * fy;
                dst.A[c * dst.Rows + r] = (float)Math.Max(0, Math.Min(1, v));
            }
        }
        return dst;
    }
    // A harmonic stack: fundamental plus overtones from column c0 to c1, each overtone quieter by rolloff.
    public void Harmonics(double fundHz, int c0, int c1, int count, double rolloff, double amount, double minHz, double maxHz) {
        for (int k = 1; k <= count; k++) {
            double row = Spectral.HzToRow(fundHz * k, minHz, maxHz, Rows);
            if (row < 0 || row > Rows - 1) break;
            Line(c0, row, c1, row, 0.9, amount * Math.Pow(rolloff, k - 1), false);
        }
    }
    public void Invert() { for (int i = 0; i < A.Length; i++) A[i] = 1 - A[i]; }
    public void FlipTime() { for (int c = 0; c < Cols / 2; c++) for (int r = 0; r < Rows; r++) { float t = A[c * Rows + r]; A[c * Rows + r] = A[(Cols - 1 - c) * Rows + r]; A[(Cols - 1 - c) * Rows + r] = t; } }
    public void FlipFreq() { for (int c = 0; c < Cols; c++) for (int r = 0; r < Rows / 2; r++) { float t = A[c * Rows + r]; A[c * Rows + r] = A[c * Rows + Rows - 1 - r]; A[c * Rows + Rows - 1 - r] = t; } }
    public void Gamma(double g) { for (int i = 0; i < A.Length; i++) A[i] = (float)Math.Pow(Math.Max(0, A[i]), g); }
    public void Threshold(double t) { for (int i = 0; i < A.Length; i++) A[i] = A[i] >= t ? 1 : 0; }
    public void Grain(double amount, int seed) { Random rnd = new Random(seed); for (int i = 0; i < A.Length; i++) A[i] *= (float)(1 - amount + amount * rnd.NextDouble() * 2); Clamp(); }
    public void Clamp() { for (int i = 0; i < A.Length; i++) A[i] = Math.Max(0, Math.Min(1, A[i])); }
    public void ShiftRows(int n) { float[] o = new float[A.Length]; for (int c = 0; c < Cols; c++) for (int r = 0; r < Rows; r++) { int rr = r + n; if (rr >= 0 && rr < Rows) o[c * Rows + rr] = A[c * Rows + r]; } A = o; }
    public void Blur(int rc, int rr) {
        float[] t = new float[A.Length], o = new float[A.Length];
        for (int c = 0; c < Cols; c++) for (int r = 0; r < Rows; r++) { float s = 0; int n = 0; for (int k = -rc; k <= rc; k++) { int cc = c + k; if (cc < 0 || cc >= Cols) continue; s += A[cc * Rows + r]; n++; } t[c * Rows + r] = s / n; }
        for (int c = 0; c < Cols; c++) for (int r = 0; r < Rows; r++) { float s = 0; int n = 0; for (int k = -rr; k <= rr; k++) { int r2 = r + k; if (r2 < 0 || r2 >= Rows) continue; s += t[c * Rows + r2]; n++; } o[c * Rows + r] = s / n; }
        A = o;
    }
    // Leaves a fading trail to the right of everything bright (decay per column, 0..1).
    public void Smear(double decay) { for (int c = 1; c < Cols; c++) for (int r = 0; r < Rows; r++) A[c * Rows + r] = Math.Max(A[c * Rows + r], (float)(A[(c - 1) * Rows + r] * decay)); }
    public void Normalize() { float m = 0; for (int i = 0; i < A.Length; i++) m = Math.Max(m, A[i]); if (m > 1e-6f) for (int i = 0; i < A.Length; i++) A[i] /= m; }
    public float Peak() { float m = 0; for (int i = 0; i < A.Length; i++) m = Math.Max(m, A[i]); return m; }

#if WINDOWS
    public void FromBitmap(Bitmap src, bool invert) {
        using (Bitmap b = new Bitmap(src, Cols, Rows)) {
            for (int c = 0; c < Cols; c++) for (int y = 0; y < Rows; y++) {
                Color px = b.GetPixel(c, y); double l = (0.299 * px.R + 0.587 * px.G + 0.114 * px.B) / 255.0 * (px.A / 255.0);
                A[c * Rows + (Rows - 1 - y)] = (float)(invert ? 1 - l : l);
            }
        }
    }
    public Bitmap ToBitmap() {
        // LockBits keeps the 512 x 128 automation canvas smooth while the user paints.
        Bitmap b = new Bitmap(Cols, Rows, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        System.Drawing.Imaging.BitmapData data = b.LockBits(new Rectangle(0, 0, Cols, Rows), System.Drawing.Imaging.ImageLockMode.WriteOnly, b.PixelFormat);
        int stride = data.Stride;
        byte[] buf = new byte[stride * Rows];
        for (int c = 0; c < Cols; c++) for (int r = 0; r < Rows; r++) {
            double v = Math.Max(0, Math.Min(1, A[c * Rows + r]));
            int R = (int)(255 * Math.Min(1, v * 2.2)), G = (int)(255 * Math.Max(0, Math.Min(1, v * 2.2 - 0.8))), B2 = (int)(200 * Math.Max(0, 1 - v * 1.5) + 255 * Math.Max(0, v - 0.85));
            int rr = Math.Min(255, R / 2 + (int)(B2 * 0.2)), bb = Math.Min(255, B2 + R / 4);
            int i = (Rows - 1 - r) * stride + c * 4;
            buf[i] = (byte)Math.Max(0, Math.Min(255, bb));
            buf[i + 1] = (byte)Math.Max(0, Math.Min(255, G));
            buf[i + 2] = (byte)Math.Max(0, Math.Min(255, rr));
            buf[i + 3] = 255;
        }
        System.Runtime.InteropServices.Marshal.Copy(buf, 0, data.Scan0, buf.Length);
        b.UnlockBits(data);
        return b;
    }
#endif
}
