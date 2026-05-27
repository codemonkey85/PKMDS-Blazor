using CoreGraphics;
using Foundation;
using ObjCRuntime;
using PKHeX.Core;
using Pkmds.Preview;
using QuickLookThumbnailing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using UIKit;

namespace Pkmds.QuickLookThumbnail;

[Register("ThumbnailProvider")]
public sealed class ThumbnailProvider : QLThumbnailProvider
{
    // The net10.0-ios binding omits public constructors for QLThumbnailReply.
    // Call the ObjC factory replyWithImageFileURL: directly via P/Invoke.
    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    public override void ProvideThumbnail(QLFileThumbnailRequest request,
                                          Action<QLThumbnailReply, NSError> handler)
    {
        var fileUrl = request.FileUrl;
        var maxSize = request.MaximumSize;
        var scale   = request.Scale;

        Task.Run(() =>
        {
            using var nsData = NSData.FromUrl(fileUrl, NSDataReadingOptions.Mapped, out var error);
            if (nsData is null)
            {
                handler(null!, error!);
                return;
            }

            var fileData = nsData.ToArray();
            var ext      = fileUrl.PathExtension?.ToLowerInvariant() ?? string.Empty;

            // All PKHeX.Core work on this background thread before drawing.
            var saveInfo = SaveCard.TryParseSave(fileData);

            UIImage? sprite  = null;
            CGRect   srcRect = CGRect.Empty;
            if (saveInfo is null)
            {
                var rel = FileSprite.GetRelativeSpritePath(fileData, ext);
                sprite  = SpriteDrawer.LoadBundledSprite(rel);
                if (sprite is not null)
                    srcRect = SpriteDrawer.OpaqueSourceRect(sprite);
            }

            // UIGraphicsImageRenderer sets up a UIKit context so UIBezierPath /
            // NSAttributedString drawing works without manually pushing a CGContext.
            var contextSize = new CGSize(maxSize.Width * scale, maxSize.Height * scale);
            var renderer    = new UIGraphicsImageRenderer(contextSize);
            var image       = renderer.CreateImage(rendCtx =>
            {
                var ctx = rendCtx.CGContext;
                if (saveInfo is not null)
                    SaveCard.Draw(ctx, contextSize, saveInfo);
                else if (sprite is not null && srcRect.Width > 0)
                    SpriteDrawer.DrawCentered(ctx, contextSize, sprite, srcRect);
            });

            // Write to a temp PNG. The QL system reads this file when it consumes the
            // reply; leave it in the temp directory for the OS to clean up.
            var tmpPath = Path.Combine(Path.GetTempPath(),
                                       Guid.NewGuid().ToString("N") + ".png");
            using var pngData = image.AsPNG();
            pngData!.Save(tmpPath, false, out _);

            var tmpUrl    = NSUrl.FromFilename(tmpPath);
            var clsHandle = Class.GetHandle("QLThumbnailReply");
            var selHandle = Selector.GetHandle("replyWithImageFileURL:");
            var handle    = ObjcMsgSend(clsHandle, selHandle, tmpUrl.Handle);
            var reply     = Runtime.GetNSObject<QLThumbnailReply>(handle)!;

            handler(reply, null!);
        });
    }
}
