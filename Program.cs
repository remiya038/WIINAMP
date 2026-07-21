using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WinampXp;

internal static class Program
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [STAThread]
    private static void Main()
    {
        SetCurrentProcessExplicitAppUserModelID("WIINAMP.Player");
        ApplicationConfiguration.Initialize();
        Application.Run(new PlayerForm());
    }
}

internal sealed class PlayerForm : Form
{
    private readonly WebView2 browser = new() { Dock = DockStyle.Fill };
    private SystemMediaMonitor? systemMedia;
    public PlayerForm()
    {
        // Windows uses this title for the taskbar entry and accessibility UI.
        Text = "WIINAMP";
        // The expanded playlist needs enough room for the larger 128px spectrum
        // and the complete transport/options row.
        // Start in the compact, playlist-hidden layout.  The web UI expands it
        // to 720px when the user opens PLAYLIST EDITOR.
        ClientSize = new Size(610, 350);
        MinimumSize = new Size(550, 380);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(7, 80, 172);
        ShowInTaskbar = true;
        ShowIcon = true;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        Controls.Add(browser);
        Resize += (_, _) => ApplyRoundedWindowRegion();
        ApplyRoundedWindowRegion();
        FormClosed += (_, _) => systemMedia?.Dispose();
        Shown += async (_, _) => await OpenPlayerAsync();
    }

    private void ApplyRoundedWindowRegion()
    {
        if (Width <= 0 || Height <= 0) return;

        var regionHandle = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 14, 14);
        try
        {
            var roundedRegion = Region.FromHrgn(regionHandle);
            Region?.Dispose();
            Region = roundedRegion;
        }
        finally
        {
            DeleteObject(regionHandle);
        }
    }

    private async Task OpenPlayerAsync()
    {
        var assets = ExtractWebAssets();
        await browser.EnsureCoreWebView2Async();
        browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
        browser.CoreWebView2.WebMessageReceived += (_, eventArgs) => HandleHostAction(eventArgs.TryGetWebMessageAsString());
        systemMedia = new SystemMediaMonitor(PostToWeb);
        browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "wiinamp.local", assets, CoreWebView2HostResourceAccessKind.Allow);
        browser.CoreWebView2.Navigate("https://wiinamp.local/index.html");
    }

    private void PostToWeb(string json)
    {
        if (IsDisposed || !browser.IsHandleCreated) return;
        BeginInvoke(() => browser.CoreWebView2?.PostWebMessageAsJson(json));
    }

    private void HandleHostAction(string action)
    {
        if (action == "minimize") WindowState = FormWindowState.Minimized;
        else if (action == "close") Close();
        else if (action == "drag")
        {
            ReleaseCapture();
            SendMessage(Handle, 0xA1, 0x2, IntPtr.Zero);
        }
        else if (action == "system-audio-toggle") systemMedia?.Toggle();
        else if (action == "system-audio-enable") systemMedia?.EnsureStarted();
        else if (action.StartsWith("system-media:", StringComparison.Ordinal))
            _ = systemMedia?.ControlAppleMusicAsync(action["system-media:".Length..]);
        else if (action.StartsWith("always-on-top:", StringComparison.Ordinal)) TopMost = action.EndsWith("true", StringComparison.Ordinal);
        else if (action.StartsWith("playlist-collapsed:", StringComparison.Ordinal))
        {
            var collapsed = action.EndsWith("true", StringComparison.Ordinal);
            ClientSize = new Size(ClientSize.Width, collapsed ? 350 : 720);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(
        int left,
        int top,
        int right,
        int bottom,
        int ellipseWidth,
        int ellipseHeight);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr objectHandle);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

    private static string ExtractWebAssets()
    {
        var assets = Path.Combine(Path.GetTempPath(), "WinampXp", "web");
        Directory.CreateDirectory(assets);
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var file in new[] { "index.html", "style.css", "app.js" })
        {
            var destination = Path.Combine(assets, file);
            using var source = assembly.GetManifestResourceStream($"web.{file}")
                ?? throw new InvalidOperationException($"Missing embedded asset: {file}");
            using var target = File.Create(destination);
            source.CopyTo(target);
        }
        return assets;
    }
}
