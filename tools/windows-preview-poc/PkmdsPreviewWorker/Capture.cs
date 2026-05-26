using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Pkmds.Preview.Windows.Worker;

/// <summary>
/// Headless render-to-PNG, for verifying the rendering pipeline without the shell or a visible
/// window. Renders the file off-screen in a WebView2 and saves a screenshot via
/// <c>CapturePreviewAsync</c>.
/// </summary>
internal static class Capture
{
    public static void RenderToPng(string filePath, string outPng)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "PkmdsPreview");

        var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, -3000),
            ClientSize = new Size(560, 680),
        };
        var webView = new WebView2
        {
            Dock = DockStyle.Fill,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(dataDir, "WebView2"),
            },
        };
        form.Controls.Add(webView);

        webView.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || webView.CoreWebView2 is null)
            {
                Application.Exit();
                return;
            }

            string html;
            try
            {
                var ext = Path.GetExtension(filePath);
                html = HtmlRenderer.RenderFile(File.ReadAllBytes(filePath), ext);
            }
            catch (Exception ex)
            {
                html = HtmlRenderer.ErrorHtml(ex.Message);
            }

            webView.CoreWebView2.NavigationCompleted += async (_, _) =>
            {
                try
                {
                    // Give remote PokeAPI sprites a moment to load before the screenshot.
                    await Task.Delay(2000);
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPng))!);
                    await using var fs = File.Create(outPng);
                    await webView.CoreWebView2.CapturePreviewAsync(
                        CoreWebView2CapturePreviewImageFormat.Png, fs);
                }
                finally
                {
                    Application.Exit();
                }
            };

            webView.CoreWebView2.NavigateToString(html);
        };

        form.Show();
        _ = webView.EnsureCoreWebView2Async();
        Application.Run();
    }
}
