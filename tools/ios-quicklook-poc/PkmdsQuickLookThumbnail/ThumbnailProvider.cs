using CoreGraphics;
using Foundation;
using PKHeX.Core;
using Pkmds.Preview;
using QuickLookThumbnailing;
using UIKit;

namespace Pkmds.QuickLookThumbnail;

[Register("ThumbnailProvider")]
public sealed class ThumbnailProvider : QLThumbnailProvider
{
    public override void ProvideThumbnail(QLFileThumbnailRequest request,
                                          Action<QLThumbnailReply?, NSError?> handler)
    {
        var fileUrl = request.FileURL;
        var maxSize = request.MaximumSize;
        var scale   = request.Scale;

        DispatchQueue.GetGlobalQueue(DispatchQueuePriority.UserInitiated).DispatchAsync(() =>
        {
            using var nsData = NSData.FromUrl(fileUrl, NSDataReadingOptions.Mapped, out var error);
            if (nsData is null)
            {
                handler(null, error);
                return;
            }

            var fileData = nsData.ToArray();
            var ext      = fileUrl.PathExtension?.ToLowerInvariant() ?? string.Empty;

            // All PKHeX.Core work happens here on the background queue, before the
            // drawing closure so the QL context thread has no managed dependencies.
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

            // QLThumbnailReply(contextSize:drawing:) — system provides a pre-configured CGContext.
            // UIGraphics.PushContext / PopContext are the iOS equivalent of NSGraphicsContext.current.
            var contextSize = new CGSize(maxSize.Width * scale, maxSize.Height * scale);
            var reply = new QLThumbnailReply(contextSize, ctx =>
            {
                UIGraphics.PushContext(ctx);
                try
                {
                    if (saveInfo is not null)
                    {
                        SaveCard.Draw(ctx, contextSize, saveInfo);
                    }
                    else if (sprite?.CGImage is { } cgImg && srcRect.Width > 0)
                    {
                        SpriteDrawer.DrawCentered(ctx, contextSize, cgImg, srcRect);
                    }
                }
                finally
                {
                    UIGraphics.PopContext();
                }
                return true;
            });

            handler(reply, null);
        });
    }
}
