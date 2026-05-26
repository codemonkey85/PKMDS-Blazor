using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Pkmds.Preview.Windows.Worker;

/// <summary>
/// A borderless WinForms host for a WebView2 control. Either reparented into the preview
/// pane's HWND (<see cref="AttachToHost"/>) or shown as a normal top-level window
/// (<see cref="ShowAsTopLevel"/>). Renders via the shared <c>HtmlRenderer</c>.
/// </summary>
internal sealed class PreviewForm : Form
{
    private const int GwlStyle = -16;
    private const int WsChild = 0x40000000;

    // prevhost spawns us; if we inherit Low integrity, %LOCALAPPDATA% isn't writable but
    // LocalLow is — and a self-contained EXE starts its own runtime fine regardless of IL.
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "AppData", "LocalLow", "PkmdsPreview");

    private readonly WebView2 _webView;
    private string? _pendingFile;
    private bool _ready;
    private nint _parentHwnd;

    public PreviewForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        Visible = false;

        // Force native-window creation on this STA thread before reparenting.
        _ = Handle;

        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = System.Drawing.Color.Transparent,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(DataDir, "WebView2"),
            },
        };
        _webView.CoreWebView2InitializationCompleted += OnCoreWebView2Ready;
        Controls.Add(_webView);
        _ = _webView.EnsureCoreWebView2Async();
    }

    /// <summary>Reparent into the host preview-pane window and size to its rect.</summary>
    public bool AttachToHost(nint parent, Rectangle bounds)
    {
        if (parent == 0 || !NativeMethods.IsWindow(parent))
            return false;

        _parentHwnd = parent;

        var style = NativeMethods.GetWindowLong(Handle, GwlStyle);
        if ((style & WsChild) == 0)
            NativeMethods.SetWindowLong(Handle, GwlStyle, style | WsChild);

        // SetParent returns the PREVIOUS parent, which is NULL (0) for a top-level form even on
        // success — so a 0 return only means failure if GetLastError is non-zero.
        Marshal.SetLastSystemError(0);
        _ = NativeMethods.SetParent(Handle, parent);
        var err = Marshal.GetLastWin32Error();
        if (err != 0)
        {
            Diag.Log($"attach: SetParent failed err={err}");
            return false;
        }

        Bounds = bounds;
        Visible = true;
        Diag.Log($"attached: requested {bounds.Width}x{bounds.Height} at ({bounds.X},{bounds.Y}); " +
                 $"actual Bounds={Bounds.Width}x{Bounds.Height} DeviceDpi={DeviceDpi}");
        return true;
    }

    /// <summary>Show as a normal resizable window (manual visual check).</summary>
    public void ShowAsTopLevel(string filePath)
    {
        FormBorderStyle = FormBorderStyle.Sizable;
        Text = "PKMDS Preview";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 680);
        Visible = true;
        Render(filePath);
    }

    public void Render(string filePath)
    {
        _pendingFile = filePath;
        RenderIfReady();
    }

    /// <summary>
    /// Watch the shim's named auto-reset event; when signaled (the pane was resized), re-fit to
    /// the parent's current client rect on the UI thread. WebView2 reflows the responsive CSS.
    /// </summary>
    public void WatchForResize(string eventName)
    {
        if (string.IsNullOrEmpty(eventName) || !EventWaitHandle.TryOpenExisting(eventName, out var evt))
        {
            Diag.Log($"resize event '{eventName}' unavailable; resize disabled");
            return;
        }

        var thread = new Thread(() =>
        {
            try
            {
                while (evt.WaitOne())
                {
                    if (IsDisposed)
                        return;
                    BeginInvoke((Action)UpdateBoundsFromParent);
                }
            }
            catch
            {
                // form closed / handle gone — stop watching
            }
            finally
            {
                evt.Dispose();
            }
        })
        {
            IsBackground = true,
            Name = "pkmds-resize-watcher",
        };
        thread.Start();
    }

    private void UpdateBoundsFromParent()
    {
        if (_parentHwnd == 0 || !NativeMethods.IsWindow(_parentHwnd))
        {
            Application.Exit();   // the preview pane is gone
            return;
        }
        if (NativeMethods.GetClientRect(_parentHwnd, out var rc))
        {
            var bounds = Rectangle.FromLTRB(rc.Left, rc.Top, rc.Right, rc.Bottom);
            if (bounds != Bounds)
                Bounds = bounds;
        }
    }

    private void OnCoreWebView2Ready(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        Diag.Log($"CoreWebView2 ready: success={e.IsSuccess} ex={e.InitializationException?.Message}");
        if (!e.IsSuccess || _webView.CoreWebView2 is null)
        {
            Application.Exit();
            return;
        }

        var settings = _webView.CoreWebView2.Settings;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;

        _ready = true;
        RenderIfReady();
    }

    private void RenderIfReady()
    {
        if (!_ready || _pendingFile is null || _webView.CoreWebView2 is null)
            return;

        string html;
        try
        {
            var ext = Path.GetExtension(_pendingFile);
            var bytes = File.ReadAllBytes(_pendingFile);
            html = HtmlRenderer.RenderFile(bytes, ext);
        }
        catch (Exception ex)
        {
            html = HtmlRenderer.ErrorHtml($"Failed to read file: {ex.Message}");
        }

        _pendingFile = null;
        Diag.Log($"navigating: html {html.Length} chars; clientSize {ClientSize.Width}x{ClientSize.Height}");
        _webView.CoreWebView2.NavigateToString(html);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _webView.Dispose();
        base.Dispose(disposing);
    }
}

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint SetParent(nint hWndChild, nint hWndNewParent);

    [DllImport("user32.dll")]
    internal static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    internal static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetClientRect(nint hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}
