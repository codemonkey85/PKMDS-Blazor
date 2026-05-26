using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Pkmds.Core.Utilities;

namespace Pkmds.Preview.Windows.Worker;

/// <summary>
/// Renders a file's representative bundled sprite to a square PNG at the requested size, for the
/// native <c>IThumbnailProvider</c> shim to load into an HBITMAP. Fully offline (bundled sprites).
/// </summary>
internal static class Thumbnail
{
    private static readonly string SpritesRoot = Path.Combine(AppContext.BaseDirectory, "sprites");

    public static void Render(string filePath, int size, string outputPng)
    {
        string spriteRelative;
        try
        {
            spriteRelative = FileSprite.GetRelativeSpritePath(File.ReadAllBytes(filePath), Path.GetExtension(filePath));
        }
        catch
        {
            spriteRelative = SpritePaths.PokemonFallbackFile;
        }

        var spritePath = Path.Combine(SpritesRoot, spriteRelative.Replace('/', Path.DirectorySeparatorChar));

        using var canvas = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(canvas))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            if (File.Exists(spritePath))
            {
                using var src = new Bitmap(spritePath);
                // Scale to fit, preserving aspect ratio, centered.
                var scale = Math.Min((float)size / src.Width, (float)size / src.Height);
                var w = src.Width * scale;
                var h = src.Height * scale;
                g.DrawImage(src, (size - w) / 2f, (size - h) / 2f, w, h);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPng))!);
        canvas.Save(outputPng, ImageFormat.Png);
    }
}
