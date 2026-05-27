using CoreGraphics;
using Foundation;
using UIKit;

namespace Pkmds.QuickLookThumbnail;

internal static class SpriteDrawer
{
    // Sprites are copied into Resources/sprites/ by build-extension.sh after dotnet build.
    internal static UIImage? LoadBundledSprite(string relativePath)
    {
        var resourcePath = NSBundle.MainBundle.ResourcePath;
        if (resourcePath is null) return null;

        var segments = relativePath.Split('/');
        var path     = Path.Combine(resourcePath, "sprites", Path.Combine(segments));
        return File.Exists(path) ? UIImage.FromFile(path) : null;
    }

    // Returns the tight bounding box (in CGImage pixel coordinates) of non-transparent pixels.
    // Falls back to the full image pixel bounds when pixel data cannot be read.
    internal static CGRect OpaqueSourceRect(UIImage image)
    {
        var cgImg = image.CGImage;
        if (cgImg is null)
            return new CGRect(0, 0, (int)image.Size.Width, (int)image.Size.Height);

        var pw = (int)cgImg.Width;
        var ph = (int)cgImg.Height;
        if (pw == 0 || ph == 0) return new CGRect(0, 0, pw, ph);

        var full   = new CGRect(0, 0, pw, ph);
        var stride = pw * 4;

        // BGRA / premultipliedFirst — alpha at byte offset +3, native ARM layout.
        // Use the constructor directly; CGBitmapContext.Create matches an adaptive overload
        // (with callback parameters) when passed IntPtr.Zero as the data argument.
        var bmpCtx = new CGBitmapContext(
            IntPtr.Zero, pw, ph, 8, stride,
            CGColorSpace.CreateDeviceRGB(),
            CGBitmapFlags.ByteOrder32Little | CGBitmapFlags.PremultipliedFirst);
        if (bmpCtx is null) return full;

        // CGBitmapContext row 0 = visual top (raster order, y-down).
        bmpCtx.DrawImage(new CGRect(0, 0, pw, ph), cgImg);
        var rawData = bmpCtx.Data;
        if (rawData == IntPtr.Zero) return full;

        unsafe
        {
            var bytes  = (byte*)rawData.ToPointer();
            int left   = pw, right = -1, top = ph, bottom = -1;
            for (var row = 0; row < ph; row++)
            {
                for (var col = 0; col < pw; col++)
                {
                    if (bytes[row * stride + col * 4 + 3] > 10)
                    {
                        if (col < left)   left   = col;
                        if (col > right)  right  = col;
                        if (row < top)    top    = row;
                        if (row > bottom) bottom = row;
                    }
                }
            }
            if (right < left || bottom < top) return full;

            // Return pixel coordinates — used by DrawCentered to crop the CGImage.
            return new CGRect(left, top, right - left + 1, bottom - top + 1);
        }
    }

    // Draws sprite centred in the QL context, scaled so the opaque region fills 90% of the frame.
    // Crops the CGImage to the opaque pixel bounding box, then draws via UIImage.Draw(CGRect),
    // which handles the CG coordinate flip (bottom-left origin) internally — no manual flip needed.
    internal static void DrawCentered(CGContext ctx, CGSize ctxSize, UIImage sprite, CGRect srcPixels)
    {
        const double fill = 0.90;
        var scale = Math.Min(ctxSize.Width  * fill / srcPixels.Width,
                             ctxSize.Height * fill / srcPixels.Height);
        var drawW = srcPixels.Width  * scale;
        var drawH = srcPixels.Height * scale;
        var destX = (ctxSize.Width  - drawW) / 2;
        var destY = (ctxSize.Height - drawH) / 2;

        // Crop to the opaque bounding box, then wrap in a UIImage for drawing.
        var croppedCg = sprite.CGImage?.WithImageInRect(srcPixels);
        var toDraw    = croppedCg is not null ? new UIImage(croppedCg) : sprite;
        toDraw.Draw(new CGRect((nfloat)destX, (nfloat)destY, (nfloat)drawW, (nfloat)drawH));
    }
}
