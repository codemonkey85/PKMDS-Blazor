using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PKHeX.Core;
using Pkmds.Core.Utilities;

namespace Pkmds.Preview.Windows.Worker;

/// <summary>
/// Renders a file's thumbnail to a square PNG for the native <c>IThumbnailProvider</c> shim.
/// Save files render as a trainer card (<see cref="SaveCard" />); everything else draws its
/// representative bundled sprite (<see cref="FileSprite" />). Fully offline.
/// </summary>
internal static class Thumbnail
{
    private static readonly string SpritesRoot = Path.Combine(AppContext.BaseDirectory, "sprites");

    public static void Render(string filePath, int size, string outputPng)
    {
        byte[] data;
        try
        {
            data = File.ReadAllBytes(filePath);
        }
        catch
        {
            DrawSprite(SpritePaths.PokemonFallbackFile, size, outputPng);
            return;
        }

        // Saves → trainer card (identifies the game + playthrough); everything else → sprite.
        if (SaveUtil.TryGetSaveFile(data, out var sav))
        {
            try
            {
                SaveCard.Render(sav, size, outputPng);
                return;
            }
            catch
            {
                // fall back to a sprite below
            }
        }

        string spriteRelative;
        try
        {
            spriteRelative = FileSprite.GetRelativeSpritePath(data, Path.GetExtension(filePath));
        }
        catch
        {
            spriteRelative = SpritePaths.PokemonFallbackFile;
        }
        DrawSprite(spriteRelative, size, outputPng);
    }

    private static void DrawSprite(string spriteRelative, int size, string outputPng)
    {
        var spritePath = Path.Combine(SpritesRoot, spriteRelative.Replace('/', Path.DirectorySeparatorChar));

        using var canvas = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(canvas))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            if (File.Exists(spritePath))
            {
                using var src = new Bitmap(spritePath);
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
