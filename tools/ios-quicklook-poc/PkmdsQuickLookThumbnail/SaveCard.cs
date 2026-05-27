using CoreGraphics;
using CoreText;
using Foundation;
using PKHeX.Core;
using UIKit;

namespace Pkmds.QuickLookThumbnail;

internal static class SaveCard
{
    internal sealed record Info(string Version, string Ot, int Tid, int Sid, int Hours, int Minutes);

    internal static Info? TryParseSave(byte[] data)
    {
        if (!SaveUtil.TryGetSaveFile(data, out var sav))
            return null;
        return new Info(
            Version: sav.Version.ToString(),
            Ot:      sav.OT ?? string.Empty,
            Tid:     sav.TID16,
            Sid:     sav.SID16,
            Hours:   sav.PlayedHours,
            Minutes: sav.PlayedMinutes);
    }

    // Draws a trainer card into the QL CGContext.
    // iOS QL context origin: top-left, y-down — same as UIKit's drawing coordinate system
    // when entered via UIGraphics.PushContext. All y offsets are measured from the top.
    internal static void Draw(CGContext ctx, CGSize size, Info info)
    {
        var margin   = (nfloat)(size.Width * 0.05);
        var cardRect = new CGRect(margin, margin,
                                  size.Width  - 2 * margin,
                                  size.Height - 2 * margin);
        var radius      = (nfloat)(size.Width * 0.10);
        var borderWidth = (nfloat)Math.Max(2.0, size.Width * 0.014);

        var (accent, accent2) = AccentColors(info.Version);

        // Card background — use CGContext directly since UIBezierPath fills go through
        // UIGraphics.GetCurrentContext(), which is set by PushContext.
        var cardPath = UIBezierPath.FromRoundedRect(cardRect, radius);
        UIColor.White.SetFill();
        cardPath.Fill();
        cardPath.LineWidth = borderWidth;
        accent.SetStroke();
        cardPath.Stroke();

        // Version code — large, at the top of the card (y = margin + 5% padding).
        var codeH    = (nfloat)(size.Height * 0.34);
        var codeY    = margin + (nfloat)(size.Height * 0.05);
        var codeRect = new CGRect(cardRect.X, codeY, cardRect.Width, codeH);
        if (accent2 is { } a2)
            DrawFittedGradientText(info.Version, codeRect, (nfloat)(size.Height * 0.40), bold: true, a1: accent, a2: a2);
        else
            DrawFittedText(info.Version, codeRect, (nfloat)(size.Height * 0.40), bold: true, color: accent);

        // OT name
        var otH    = (nfloat)(size.Height * 0.15);
        var otY    = margin + (nfloat)(size.Height * 0.43);
        var otPad  = (nfloat)(cardRect.Width * 0.05);
        DrawFittedText(info.Ot.Length > 0 ? info.Ot : "Trainer",
                       new CGRect(cardRect.X + otPad, otY, cardRect.Width - 2 * otPad, otH),
                       (nfloat)(size.Height * 0.135), bold: true,
                       color: UIColor.FromRGB(33, 33, 33));

        // TID/SID
        var idsH = (nfloat)(size.Height * 0.12);
        var idsY = margin + (nfloat)(size.Height * 0.60);
        DrawFittedText($"{info.Tid}/{info.Sid}",
                       new CGRect(cardRect.X, idsY, cardRect.Width, idsH),
                       (nfloat)(size.Height * 0.10), bold: false,
                       color: UIColor.FromRGB(102, 102, 102));

        // Playtime
        var playH = (nfloat)(size.Height * 0.12);
        var playY = margin + (nfloat)(size.Height * 0.73);
        DrawFittedText(string.Format("{0}:{1:00}", info.Hours, info.Minutes),
                       new CGRect(cardRect.X, playY, cardRect.Width, playH),
                       (nfloat)(size.Height * 0.10), bold: false,
                       color: UIColor.FromRGB(102, 102, 102));
    }

    // Shrinks the font until text fits inside rect, then draws it horizontally centred.
    private static void DrawFittedText(string text, CGRect rect, nfloat maxEm, bool bold, UIColor color)
    {
        var style = new NSMutableParagraphStyle { Alignment = UITextAlignment.Center };
        var em = maxEm;
        while (em > 6)
        {
            var attrs = MakeAttrs(em, bold, color, style);
            var sz    = MeasureText(text, rect.Width, attrs);
            if (sz.Width <= rect.Width && sz.Height <= rect.Height)
            {
                DrawText(text, VertCentred(sz, rect), attrs);
                return;
            }
            em -= 1;
        }
        var fallbackAttrs = MakeAttrs(6, bold, color, style);
        var fallbackSz    = MeasureText(text, rect.Width, fallbackAttrs);
        DrawText(text, VertCentred(fallbackSz, rect), fallbackAttrs);
    }

    // Per-character gradient text for the ambiguous RB / GS groups.
    private static void DrawFittedGradientText(string text, CGRect rect, nfloat maxEm,
                                               bool bold, UIColor a1, UIColor a2)
    {
        if (text.Length == 0) return;
        var chars = text.ToCharArray();
        var em    = maxEm;
        while (em > 6)
        {
            var attrs = MakeAttrs(em, bold, a1, style: null);
            var sz    = MeasureText(text, rect.Width, attrs);
            if (sz.Width <= rect.Width && sz.Height <= rect.Height) break;
            em -= 1;
        }

        var baseAttrs = MakeAttrs(em, bold, a1, style: null);
        var totalW    = MeasureText(text, rect.Width, baseAttrs).Width;
        var curX      = (nfloat)(rect.MidX - totalW / 2);

        for (var i = 0; i < chars.Length; i++)
        {
            var t     = chars.Length > 1 ? (nfloat)i / (chars.Length - 1) : (nfloat)0;
            var color = Interpolate(a1, a2, t);
            var attrs = MakeAttrs(em, bold, color, style: null);
            var ch    = chars[i].ToString();
            var sz    = MeasureText(ch, rect.Width, attrs);
            DrawText(ch, new CGRect(curX, rect.MidY - sz.Height / 2, sz.Width, sz.Height), attrs);
            curX += sz.Width;
        }
    }

    private static NSDictionary MakeAttrs(nfloat em, bool bold, UIColor color, NSParagraphStyle? style)
    {
        var font = bold ? UIFont.BoldSystemFontOfSize(em) : UIFont.SystemFontOfSize(em);
        var keys   = new NSObject[] { UIStringAttributeKey.Font, UIStringAttributeKey.ForegroundColor };
        var values = new NSObject[] { font, color };
        if (style is not null)
        {
            keys   = [.. keys,   UIStringAttributeKey.ParagraphStyle];
            values = [.. values, style];
        }
        return NSDictionary.FromObjectsAndKeys(values, keys);
    }

    private static CGSize MeasureText(string text, nfloat maxWidth, NSDictionary attrs)
    {
        var ns  = new NSString(text);
        var opt = NSStringDrawingOptions.UsesLineFragmentOrigin;
        return ns.GetBoundingRect(new CGSize(maxWidth, nfloat.MaxValue), opt, attrs, null).Size;
    }

    private static void DrawText(string text, CGRect rect, NSDictionary attrs)
    {
        var ns = new NSString(text);
        ns.DrawString(rect, attrs);
    }

    private static CGRect VertCentred(CGSize sz, CGRect rect) =>
        new(rect.X, rect.MidY - sz.Height / 2, rect.Width, sz.Height);

    // ── Accent colours — mirrors macOS ThumbnailProvider.swift accentColors ──────────────────────

    private static (UIColor accent, UIColor? accent2) AccentColors(string version) => version switch
    {
        "RB" => (Hex(0xC0392B), Hex(0x2980B9)),
        "GS" => (Hex(0xD4860B), Hex(0x7F8C9A)),
        _    => (AccentColor(version), null),
    };

    private static UIColor AccentColor(string version) => version switch
    {
        "RD" or "R" or "FR" or "OR"    => Hex(0xC0392B),
        "SL"                            => Hex(0xD0432A),
        "GN" or "LG"                   => Hex(0x27AE60),
        "E"                             => Hex(0x1E9E6A),
        "BU" or "S" or "AS" or "X"    => Hex(0x2980B9),
        "D" or "BD"                    => Hex(0x4A78B0),
        "P" or "SP"                    => Hex(0xC56AA0),
        "Pt"                            => Hex(0x7A6FA0),
        "YW" or "GP"                   => Hex(0xC79100),
        "GE"                            => Hex(0x8B5A2B),
        "GD" or "HG"                   => Hex(0xD4860B),
        "SI" or "SS"                   => Hex(0x7F8C9A),
        "C"                             => Hex(0x16A0A0),
        "B" or "B2"                    => Hex(0x333333),
        "W" or "W2"                    => Hex(0x8A8A8A),
        "SN" or "US"                   => Hex(0xE07B2A),
        "MN" or "UM"                   => Hex(0x5B4B8A),
        "SW"                            => Hex(0x1F9BC2),
        "SH"                            => Hex(0xC0306A),
        "PLA"                           => Hex(0x2FA37A),
        "VL"                            => Hex(0x7A3FA0),
        "Y"                             => Hex(0xC0392B),
        _                               => Hex(0x3B6EA5),
    };

    private static UIColor Hex(int rgb) =>
        UIColor.FromRGB(
            (byte)((rgb >> 16) & 0xFF),
            (byte)((rgb >>  8) & 0xFF),
            (byte)( rgb        & 0xFF));

    private static UIColor Interpolate(UIColor a, UIColor b, nfloat t)
    {
        a.GetRGBA(out var ar, out var ag, out var ab, out _);
        b.GetRGBA(out var br, out var bg, out var bb, out _);
        return UIColor.FromRGBA(
            (nfloat)(ar + (br - ar) * t),
            (nfloat)(ag + (bg - ag) * t),
            (nfloat)(ab + (bb - ab) * t),
            1);
    }
}
