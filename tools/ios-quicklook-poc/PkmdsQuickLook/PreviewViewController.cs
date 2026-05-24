using System;
using CoreGraphics;
using Foundation;
using PKHeX.Core;
using Pkmds.Preview;
using QuickLook;
using UIKit;
using WebKit;

namespace Pkmds.QuickLook;

[Register("PreviewViewController")]
public sealed class PreviewViewController : UIViewController, IQLPreviewingController
{
    private WKWebView? webView;

    public PreviewViewController(IntPtr handle) : base(handle) { }

    public PreviewViewController() { }

    public override void LoadView()
    {
        var view = new WKWebView(CGRect.Empty, new WKWebViewConfiguration())
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            Opaque = false,
            BackgroundColor = UIColor.Clear,
        };
        view.ScrollView.BackgroundColor = UIColor.Clear;
        webView = view;
        View = view;
    }

    public void PreparePreviewOfFile(NSUrl url, Action<NSError> handler)
    {
        try
        {
            using var data = NSData.FromUrl(url, NSDataReadingOptions.Mapped, out var error);
            if (error is not null || data is null)
            {
                handler(error ?? Error("Unable to read file."));
                return;
            }

            var bytes = data.ToArray();
            var ext = url.PathExtension?.ToLowerInvariant() ?? string.Empty;
            var html = ext == "sav" ? RenderSave(bytes) : RenderPkm(bytes);
            webView?.LoadHtmlString(html, baseUrl: null!);
            handler(null!);
        }
        catch (Exception ex)
        {
            handler(Error(ex.Message));
        }
    }

    private static string RenderPkm(byte[] bytes)
    {
        var pkm = EntityFormat.GetFromBytes(bytes);
        return pkm is null
            ? FailureHtml("Unable to read entity file.")
            : HtmlRenderer.RenderPkm(pkm);
    }

    private static string RenderSave(byte[] bytes) =>
        SaveUtil.TryGetSaveFile(bytes, out var sav)
            ? HtmlRenderer.RenderSave(sav)
            : FailureHtml("Unable to read save file.");

    private static string FailureHtml(string message) =>
        $"<!doctype html><html><body style=\"font:13px -apple-system\"><p>{message}</p></body></html>";

    private static NSError Error(string message) =>
        new(new NSString("com.bondcodes.pkmds.quicklook"), 1, new NSDictionary<NSString, NSObject>(
            NSError.LocalizedDescriptionKey, new NSString(message)));
}
