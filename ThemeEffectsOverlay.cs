using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

namespace XPappThemes;

internal sealed class ThemeEffectsOverlay : Form
{
    private const int FrameMargin = 190;
    private readonly Form owner;
    private readonly Bitmap pastelFrame = LoadFrame("fx.pastel-holo-frame.png");
    private readonly Bitmap bubbleFrame = LoadFrame("fx.bubble-chrome-frame.png");
    private readonly Bitmap cloudFrame = LoadFrame("fx.cloud-circuit-frame.png");
    private readonly List<Spark> sparks = [];
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 80 };
    private string theme = "luna";
    private bool suspended;

    public ThemeEffectsOverlay(Form ownerForm)
    {
        owner = ownerForm;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        var random = new Random(73);
        for (var index = 0; index < 42; index++)
            sparks.Add(new Spark((float)random.NextDouble(), (float)random.NextDouble(),
                random.Next(5, index % 5 == 0 ? 25 : 15), (float)random.NextDouble() * MathF.PI * 2));

        owner.Shown += (_, _) => { Show(); SyncToOwner(); };
        owner.Activated += (_, _) => SyncToOwner();
        owner.Move += (_, _) => SyncToOwner();
        owner.Resize += (_, _) => SyncToOwner();
        owner.FormClosed += (_, _) => Close();
        timer.Tick += (_, _) =>
        {
            if (Visible && !suspended && IsDecoratedTheme()) RenderLayered();
        };
        timer.Start();
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= 0x20 | 0x80 | 0x80000 | 0x08000000;
            return parameters;
        }
    }

    public void SetTheme(string value)
    {
        theme = NormalizeTheme(value);
        Visible = !suspended && IsDecoratedTheme();
        if (Visible) SyncToOwner();
    }

    public void SetSuspended(bool value)
    {
        suspended = value;
        Visible = !suspended && IsDecoratedTheme();
        if (Visible) SyncToOwner();
    }

    public void SyncToOwner()
    {
        if (owner.IsDisposed || owner.WindowState == FormWindowState.Minimized) return;
        var bounds = new Rectangle(owner.Left - FrameMargin, owner.Top - FrameMargin,
            owner.Width + FrameMargin * 2, owner.Height + FrameMargin * 2);
        Bounds = bounds;
        if (IsHandleCreated && owner.IsHandleCreated)
            SetWindowPos(Handle, owner.Handle, bounds.Left, bounds.Top, bounds.Width, bounds.Height, 0x0010);
        if (Visible) RenderLayered();
    }

    private static string NormalizeTheme(string value) =>
        value.Trim().ToLowerInvariant().Replace("_", "-") switch
        {
            "pastelholo" => "pastel-holo",
            "bubblechrome" => "bubble-chrome",
            "cloudcircuit" => "cloud-circuit",
            var normalized => normalized
        };

    private bool IsDecoratedTheme() => theme is "pastel-holo" or "bubble-chrome" or "cloud-circuit";

    private void RenderLayered()
    {
        if (!IsHandleCreated || IsDisposed || !Visible || suspended || Width < 1 || Height < 1) return;
        using var bitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            DrawTheme(graphics);
        }
        var screenDc = GetDC(IntPtr.Zero);
        var memoryDc = CreateCompatibleDC(screenDc);
        var bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
        var previous = SelectObject(memoryDc, bitmapHandle);
        try
        {
            var destination = new NativePoint(Left, Top);
            var size = new NativeSize(Width, Height);
            var source = new NativePoint(0, 0);
            var blend = new BlendFunction { SourceConstantAlpha = 255, AlphaFormat = 1 };
            UpdateLayeredWindow(Handle, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, 2);
        }
        finally
        {
            SelectObject(memoryDc, previous);
            DeleteObject(bitmapHandle);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private void DrawTheme(Graphics graphics)
    {
        switch (theme)
        {
            case "pastel-holo":
                DrawAlignedFrame(graphics, pastelFrame, new RectangleF(137, 146, 1260, 706));
                DrawPastelSparks(graphics);
                break;
            case "bubble-chrome":
                DrawAlignedFrame(graphics, bubbleFrame, new RectangleF(190, 180, 1150, 670));
                DrawBubbleMotion(graphics);
                break;
            case "cloud-circuit":
                DrawAlignedFrame(graphics, cloudFrame, new RectangleF(150, 155, 1230, 700));
                DrawGardenMotion(graphics);
                break;
        }
    }

    private void DrawAlignedFrame(Graphics graphics, Bitmap image, RectangleF opening)
    {
        var scaleX = owner.Width / opening.Width;
        var scaleY = owner.Height / opening.Height;
        var destination = new RectangleF(FrameMargin - opening.Left * scaleX, FrameMargin - opening.Top * scaleY,
            image.Width * scaleX, image.Height * scaleY);
        graphics.DrawImage(image, destination, new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
    }

    private void DrawPastelSparks(Graphics graphics)
    {
        var tick = Environment.TickCount / 850f;
        for (var index = 0; index < sparks.Count; index++)
        {
            var spark = sparks[index];
            var point = EdgePoint(spark.X, spark.Y, index);
            var pulse = .5f + .5f * MathF.Sin(tick * 1.35f + spark.Phase);
            var size = spark.Size * (.8f + pulse * .3f);
            using var glow = new SolidBrush(Color.FromArgb((int)(45 + pulse * 85), 155, 225, 255));
            graphics.FillEllipse(glow, point.X - size, point.Y - size, size * 2, size * 2);
            using var pen = new Pen(Color.FromArgb((int)(155 + pulse * 100), 255, 255, 255), Math.Max(1.2f, size / 7))
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            graphics.DrawLine(pen, point.X - size, point.Y, point.X + size, point.Y);
            graphics.DrawLine(pen, point.X, point.Y - size, point.X, point.Y + size);
            if (index % 3 == 0)
            {
                graphics.DrawLine(pen, point.X - size * .55f, point.Y - size * .55f, point.X + size * .55f, point.Y + size * .55f);
                graphics.DrawLine(pen, point.X + size * .55f, point.Y - size * .55f, point.X - size * .55f, point.Y + size * .55f);
            }
        }
    }

    private void DrawBubbleMotion(Graphics graphics)
    {
        var tick = Environment.TickCount / 1000f;
        DrawRingGloss(graphics, new PointF(FrameMargin + 8, FrameMargin + 7), 154, 55, -22, tick * 28, Color.FromArgb(245, 255, 126, 188));
        DrawRingGloss(graphics, new PointF(Width - FrameMargin - 8, Height - FrameMargin - 5), 135, 48, 22, -tick * 41, Color.FromArgb(245, 105, 213, 255));
        for (var index = 0; index < 14; index++)
        {
            var spark = sparks[index];
            var point = EdgePoint(spark.X, spark.Y, index);
            point.Y += MathF.Sin(tick + index * .7f) * 7;
            var size = 7 + spark.Size * .65f;
            using var fill = new SolidBrush(Color.FromArgb(62, 94, 216, 255));
            using var outline = new Pen(Color.FromArgb(210, 255, 255, 255), 1.5f);
            graphics.FillEllipse(fill, point.X - size / 2, point.Y - size / 2, size, size);
            graphics.DrawEllipse(outline, point.X - size / 2, point.Y - size / 2, size, size);
        }
    }

    private static void DrawRingGloss(Graphics graphics, PointF center, float width, float height, float angle, float phase, Color color)
    {
        var state = graphics.Save();
        graphics.TranslateTransform(center.X, center.Y);
        graphics.RotateTransform(angle);
        var ring = new RectangleF(-width / 2, -height / 2, width, height);
        var start = ((phase % 360) + 360) % 360;
        for (var index = 0; index < 5; index++)
        {
            using var gloss = new Pen(Color.FromArgb(Math.Max(40, 235 - index * 40), color), Math.Max(1.5f, 4.5f - index * .55f))
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            graphics.DrawArc(gloss, ring, start - index * 15, 11);
        }
        graphics.Restore(state);
    }

    private void DrawGardenMotion(Graphics graphics)
    {
        var tick = Environment.TickCount / 900f;
        for (var index = 0; index < 24; index++)
        {
            var spark = sparks[index];
            var point = EdgePoint(spark.X, spark.Y, index);
            point.X += MathF.Sin(tick * .7f + index) * 8;
            point.Y += MathF.Cos(tick * .52f + index * .8f) * 9;
            var pulse = .5f + .5f * MathF.Sin(tick + index);
            var color = index % 3 == 0
                ? Color.FromArgb((int)(110 + pulse * 130), 126, 234, 209)
                : Color.FromArgb((int)(110 + pulse * 130), 255, 247, 145);
            using var brush = new SolidBrush(color);
            var size = index % 5 == 0 ? 9 : 5;
            graphics.FillEllipse(brush, point.X - size / 2, point.Y - size / 2, size, size);
            if (index % 5 == 0)
            {
                var state = graphics.Save();
                graphics.TranslateTransform(point.X, point.Y);
                graphics.RotateTransform(MathF.Sin(tick + index) * 25);
                graphics.FillEllipse(brush, -2, -9, 5, 11);
                graphics.Restore(state);
            }
        }
    }

    private PointF EdgePoint(float xRatio, float yRatio, int index)
    {
        var side = index % 4;
        var x = side switch
        {
            0 => 8 + xRatio * (FrameMargin - 16),
            1 => Width - FrameMargin + 8 + xRatio * (FrameMargin - 16),
            _ => FrameMargin + xRatio * Math.Max(1, Width - FrameMargin * 2)
        };
        var y = side switch
        {
            2 => 8 + yRatio * (FrameMargin - 16),
            3 => Height - FrameMargin + 8 + yRatio * (FrameMargin - 16),
            _ => FrameMargin + yRatio * Math.Max(1, Height - FrameMargin * 2)
        };
        return new PointF(Math.Clamp(x, 4, Width - 4), Math.Clamp(y, 4, Height - 4));
    }

    private static Bitmap LoadFrame(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing frame resource: {resourceName}");
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            pastelFrame.Dispose();
            bubbleFrame.Dispose();
            cloudFrame.Dispose();
        }
        base.Dispose(disposing);
    }

    private readonly record struct Spark(float X, float Y, float Size, float Phase);
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint(int x, int y) { public int X = x; public int Y = y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeSize(int width, int height) { public int Width = width; public int Height = height; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct BlendFunction { public byte BlendOp; public byte BlendFlags; public byte SourceConstantAlpha; public byte AlphaFormat; }
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destinationDc, ref NativePoint destination, ref NativeSize size, IntPtr sourceDc, ref NativePoint source, int colorKey, ref BlendFunction blend, int flags);
}
