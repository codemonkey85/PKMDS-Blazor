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

    // Returns the tight bounding box (in UIImage pixel coordinates) of non-transparent pixels.
    // Falls back to the full image bounds when pixel data cannot be read.
    internal static CGRect OpaqueSourceRect(UIImage image)
    {
        var cgImg = image.CGImage;
        if (cgImg is null)
            return new CGRect(CGPoint.Empty, image.Size);

        var pw = cgImg.Width;
        var ph = cgImg.Height;
        if (pw == 0 || ph == 0)
            return new CGRect(CGPoint.Empty, image.Size);

        var full   = new CGRect(0, 0, pw, ph);
        var stride = pw * 4;

        // BGRA / premultipliedFirst — alpha at byte +3, native ARM layout.
        var ctx = CGBitmapContext.Create(
            IntPtr.Zero, pw, ph, 8, stride,
            CGColorSpace.CreateDeviceRGB(),
            CGBitmapFlags.ByteOrder32Little | CGBitmapFlags.PremultipliedFirst);
        if (ctx is null) return full;

        // CGBitmapContext row 0 = visual top (raster order), y-down.
        ctx.DrawImage(new CGRect(0, 0, pw, ph), cgImg);
        var rawData = ctx.Data;
        if (rawData == IntPtr.Zero) return full;

        unsafe
        {
            var bytes = (byte*)rawData.ToPointer();
            int left = pw, right = -1, top = ph, bottom = -1;
            for (var row = 0; row < ph; row++)
            {
                for (var col = 0; col < pw; col++)
                {
                    // Alpha byte is at offset +3 in BGRA layout.
                    if (bytes[row * stride + col * 4 + 3] > 10)
                    {
                        if (col  < left)   left   = col;
                        if (col  > right)  right  = col;
                        if (row  < top)    top    = row;
                        if (row  > bottom) bottom = row;
                    }
                }
            }
            if (right < left || bottom < top) return full;

            // CGBitmapContext row 0 is the visual top (y-down), so pixel row maps directly to
            // y in the CGImage coordinate system (which is also y-down in raster storage).
            // UIImage.Size is in points; scale pixel coords to points for the return value.
            var sx = (nfloat)image.Size.Width  / pw;
            var sy = (nfloat)image.Size.Height / ph;
            return new CGRect(
                (nfloat)left              * sx,
                (nfloat)top               * sy,
                (nfloat)(right - left + 1) * sx,
                (nfloat)(bottom - top + 1) * sy);
        }
    }

    // Draws cgImg centred in the QL context, scaled so the opaque region fills 90% of the frame.
    // CGContext.DrawImage has bottom-left origin (y-up), so a vertical flip is required so the
    // sprite appears right-side up in the iOS QL context (which is y-down / top-left origin).
    internal static void DrawCentered(CGContext ctx, CGSize ctxSize, CGImage cgImg, CGRect srcRect)
    {
        const double fill = 0.90;
        var scale = Math.Min(ctxSize.Width  * fill / srcRect.Width,
                             ctxSize.Height * fill / srcRect.Height);
        var drawW = srcRect.Width  * scale;
        var drawH = srcRect.Height * scale;
        var destX = (ctxSize.Width  - drawW) / 2;
        var destY = (ctxSize.Height - drawH) / 2;

        // Clip to the opaque source region.
        var srcRectPixels = new CGRect(
            srcRect.X / (cgImg.Width  > 0 ? cgImg.Width  : 1),
            srcRect.Y / (cgImg.Height > 0 ? cgImg.Height : 1),
            srcRect.Width  / (cgImg.Width  > 0 ? cgImg.Width  : 1),
            srcRect.Height / (cgImg.Height > 0 ? cgImg.Height : 1));

        // Flip vertically around the draw rect's centre so CGContext.DrawImage renders upright.
        ctx.SaveState();
        ctx.TranslateCTM((nfloat)(destX + drawW / 2), (nfloat)(destY + drawH / 2));
        ctx.ScaleCTM(1, -1);

        // Crop to the opaque sub-region before drawing.
        var croppedImg = cgImg.WithImageInRect(new CGRect(
            (nfloat)(srcRect.X),
            (nfloat)(srcRect.Y),
            (nfloat)(srcRect.Width),
            (nfloat)(srcRect.Height)));

        ctx.DrawImage(new CGRect(
            (nfloat)(-drawW / 2), (nfloat)(-drawH / 2),
            (nfloat)drawW, (nfloat)drawH),
            croppedImg ?? cgImg);

        ctx.RestoreState();
    }
}
