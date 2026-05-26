using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using PKHeX.Core;

namespace Pkmds.Preview.Windows.Worker;

/// <summary>
/// Renders a save file as a compact "trainer card" thumbnail: the game version code (large, in the
/// game's accent colour) plus the OT name, TID/SID, and playtime. Identifies both the game and the
/// specific playthrough — unlike a single Pokémon sprite. Drawn with System.Drawing (no WebView2)
/// so a folder of saves thumbnails quickly.
/// </summary>
internal static class SaveCard
{
    public static void Render(SaveFile sav, int size, string outputPng)
    {
        var code = sav.Version.ToString();
        var (accent, accent2) = AccentColors(sav.Version);
        var ot = string.IsNullOrWhiteSpace(sav.OT) ? "Trainer" : sav.OT.Trim();
        var ids = string.Create(CultureInfo.InvariantCulture, $"{sav.TID16}/{sav.SID16}");
        var play = string.Create(CultureInfo.InvariantCulture, $"{sav.PlayedHours}:{sav.PlayedMinutes:00}");

        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // Rounded white card with a thin accent border. Version *groups* (RB, GS) — which the save
        // format can't disambiguate — get a diagonal two-colour gradient instead of a single colour.
        var margin = size * 0.05f;
        var rect = new RectangleF(margin, margin, size - 2 * margin, size - 2 * margin);
        var borderWidth = Math.Max(2f, size * 0.014f);
        using (var path = RoundedRect(rect, size * 0.10f))
        using (var fill = new SolidBrush(Color.White))
        {
            g.FillPath(fill, path);
            if (accent2 is { } a2)
            {
                using var grad = new LinearGradientBrush(rect, accent, a2, LinearGradientMode.ForwardDiagonal);
                using var pen = new Pen(grad, borderWidth);
                g.DrawPath(pen, path);
            }
            else
            {
                using var pen = new Pen(accent, borderWidth);
                g.DrawPath(pen, path);
            }
        }

        using var center = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter,
        };
        using var accentBrush = new SolidBrush(accent);
        using var darkBrush = new SolidBrush(Color.FromArgb(0x22, 0x22, 0x22));
        using var mutedBrush = new SolidBrush(Color.FromArgb(0x66, 0x66, 0x66));

        // Code (large). For a group, a horizontal gradient lands each letter on its game's colour
        // (e.g. "RB" → R red, B blue).
        var codeArea = new RectangleF(rect.X, rect.Y + size * 0.05f, rect.Width, size * 0.34f);
        if (accent2 is { } codeAccent2)
        {
            using var codeGrad = new LinearGradientBrush(codeArea, accent, codeAccent2, LinearGradientMode.Horizontal);
            DrawFitted(g, code, FontStyle.Bold, size * 0.40f, codeGrad, center, codeArea);
        }
        else
        {
            DrawFitted(g, code, FontStyle.Bold, size * 0.40f, accentBrush, center, codeArea);
        }
        DrawFitted(g, ot, FontStyle.Bold, size * 0.135f, darkBrush, center,
            new RectangleF(rect.X + size * 0.05f, rect.Y + size * 0.43f, rect.Width - size * 0.10f, size * 0.15f));
        DrawFitted(g, ids, FontStyle.Regular, size * 0.10f, mutedBrush, center,
            new RectangleF(rect.X, rect.Y + size * 0.60f, rect.Width, size * 0.12f));
        DrawFitted(g, play, FontStyle.Regular, size * 0.10f, mutedBrush, center,
            new RectangleF(rect.X, rect.Y + size * 0.73f, rect.Width, size * 0.12f));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPng))!);
        bmp.Save(outputPng, ImageFormat.Png);
    }

    // Shrink the font until the text fits the area, then draw it.
    private static void DrawFitted(Graphics g, string text, FontStyle style, float maxEm, Brush brush,
        StringFormat fmt, RectangleF area)
    {
        for (var em = maxEm; em > 6f; em -= 1f)
        {
            using var f = new Font("Segoe UI", em, style, GraphicsUnit.Pixel);
            var sz = g.MeasureString(text, f);
            if (sz.Width <= area.Width && sz.Height <= area.Height)
            {
                g.DrawString(text, f, brush, area, fmt);
                return;
            }
        }
        using var min = new Font("Segoe UI", 6f, style, GraphicsUnit.Pixel);
        g.DrawString(text, min, brush, area, fmt);
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // Accent for a version. Paired groups the save format can't disambiguate return two colours
    // (rendered as a gradient); everything else returns a single colour with a null second.
    private static (Color, Color?) AccentColors(GameVersion v) => v switch
    {
        GameVersion.RB => (Rgb(0xC0392B), Rgb(0x2980B9)), // Red + Blue
        GameVersion.GS => (Rgb(0xD4860B), Rgb(0x7F8C9A)), // Gold + Silver
        _ => (AccentColor(v), null),
    };

    // Game-appropriate accent colour per version (kept readable on a white card).
    private static Color AccentColor(GameVersion v) => v switch
    {
        GameVersion.RD or GameVersion.R or GameVersion.FR or GameVersion.OR or GameVersion.Y => Rgb(0xC0392B), // red
        GameVersion.SL => Rgb(0xD0432A),                                                   // scarlet
        GameVersion.GN or GameVersion.LG => Rgb(0x27AE60),                                 // green
        GameVersion.E => Rgb(0x1E9E6A),                                                    // emerald
        GameVersion.BU or GameVersion.S or GameVersion.AS or GameVersion.X => Rgb(0x2980B9), // blue
        GameVersion.D or GameVersion.BD => Rgb(0x4A78B0),                                  // diamond
        GameVersion.P or GameVersion.SP => Rgb(0xC56AA0),                                  // pearl
        GameVersion.Pt => Rgb(0x7A6FA0),                                                   // platinum
        GameVersion.YW or GameVersion.GP => Rgb(0xC79100),                                 // yellow / pikachu
        GameVersion.GE => Rgb(0x8B5A2B),                                                   // eevee
        GameVersion.GD or GameVersion.HG => Rgb(0xD4860B),                                 // gold
        GameVersion.SI or GameVersion.SS => Rgb(0x7F8C9A),                                 // silver
        GameVersion.C => Rgb(0x16A0A0),                                                    // crystal
        GameVersion.B or GameVersion.B2 => Rgb(0x333333),                                  // black
        GameVersion.W or GameVersion.W2 => Rgb(0x8A8A8A),                                  // white
        GameVersion.SN or GameVersion.US => Rgb(0xE07B2A),                                 // sun
        GameVersion.MN or GameVersion.UM => Rgb(0x5B4B8A),                                 // moon
        GameVersion.SW => Rgb(0x1F9BC2),                                                   // sword
        GameVersion.SH => Rgb(0xC0306A),                                                   // shield
        GameVersion.PLA => Rgb(0x2FA37A),                                                  // legends arceus
        GameVersion.VL => Rgb(0x7A3FA0),                                                   // violet
        _ => Rgb(0x3B6EA5),                                                                // neutral
    };

    private static Color Rgb(int rgb) => Color.FromArgb(0xFF, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
}
