using System.Globalization;
using System.Text;
using PKHeX.Core;
using Pkmds.Core.Extensions;
using Pkmds.Core.Utilities;

namespace Pkmds.Preview;

public static class HtmlRenderer
{
    // Final fallback when even the base-species URL can't be built (invalid species).
    private const string PlaceholderSpriteUrl =
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/home/0.png";

    /// <summary>
    /// Optional hook that resolves a bundled sprite (a relative path from <see cref="SpritePaths"/>)
    /// to an <c>&lt;img src&gt;</c> value — typically a <c>data:</c> URI read from a local sprite
    /// bundle. When set and it returns non-null, the renderer uses it instead of a remote PokeAPI
    /// URL, making previews fully offline. Hosts that don't bundle sprites leave it null (or return
    /// null for a missing file) to fall back to PokeAPI.
    /// </summary>
    public static Func<string, string?>? SpriteResolver { get; set; }

    public static string RenderPkm(PKM pkm)
    {
        var s = GameInfo.Strings;
        var la = TryAnalyze(pkm);
        var sb = new StringBuilder(4096);
        AppendDocStart(sb, $"{Lookup(s.specieslist, pkm.Species)} (Lv. {pkm.CurrentLevel})");
        sb.Append("<div class=\"pkm\">");

        AppendSprite(sb, pkm);

        sb.Append("<div class=\"info\">");
        var nickname = pkm.Nickname ?? string.Empty;
        var speciesName = Lookup(s.specieslist, pkm.Species);
        var displayName = nickname.Length > 0 && nickname != speciesName
            ? $"{Escape(nickname)} <span class=\"muted\">({Escape(speciesName)})</span>"
            : Escape(speciesName);

        sb.Append("<h1>").Append(displayName);
        if (pkm.IsShiny)
        {
            sb.Append(" <span class=\"shiny\" title=\"Shiny\">★</span>");
        }
        sb.Append("</h1>");

        sb.Append("<div class=\"meta\">")
            .Append("Lv. ").Append(pkm.CurrentLevel)
            .Append(" &middot; ").Append(Escape(Lookup(s.natures, (int)pkm.Nature))).Append(" nature");
        if (pkm.Form > 0)
        {
            sb.Append(" &middot; Form ").Append(pkm.Form);
        }
        if (la is not null)
        {
            var (label, css) = GetLegalityStatus(la);
            sb.Append(" &middot; <span class=\"badge ").Append(css).Append("\">").Append(label).Append("</span>");
        }
        sb.Append("</div>");

        sb.Append("<dl class=\"details\">");
        AppendDt(sb, "OT", $"{Escape(pkm.OriginalTrainerName ?? string.Empty)} <span class=\"muted\">({pkm.TID16}/{pkm.SID16})</span>");
        AppendDt(sb, "Ability", Escape(Lookup(s.abilitylist, pkm.Ability)));
        if (pkm.HeldItem > 0)
        {
            AppendDt(sb, "Item", Escape(Lookup(s.itemlist, pkm.HeldItem)));
        }
        AppendDt(sb, "Format", $"PK{pkm.Format} (Gen {pkm.Generation})");
        sb.Append("</dl>");

        AppendStatsTable(sb, pkm);
        AppendMovesTable(sb, pkm, s);

        sb.Append("</div>"); // .info
        sb.Append("</div>"); // .pkm

        if (la is not null)
        {
            AppendLegalityIssues(sb, la);
        }
        AppendShowdownSet(sb, pkm);

        AppendDocEnd(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Auto-dispatches to the appropriate renderer based on file content and extension.
    /// Tries save → mystery gift (extension-guided) → PKM entity → error page.
    /// </summary>
    /// <param name="data">Raw file bytes.</param>
    /// <param name="fileExtension">File extension with or without leading dot (e.g. "wb8" or ".wb8").</param>
    public static string RenderFile(byte[] data, string fileExtension)
    {
        var ext = fileExtension.Length > 0 && fileExtension[0] != '.'
            ? "." + fileExtension
            : fileExtension;

        // WonderCard3 (.wc3) is not a DataMysteryGift — MysteryGift.GetMysteryGift won't handle it.
        if (ext.Equals(".wc3", StringComparison.OrdinalIgnoreCase))
            return RenderWc3File(data);

        if (SaveUtil.TryGetSaveFile(data, out var sav))
            return RenderSave(sav);
        if (MysteryGift.GetMysteryGift(data, ext.AsSpan()) is { } gift)
            return RenderMysteryGift(gift);
        if (EntityFormat.GetFromBytes(data) is { } pkm)
            return RenderPkm(pkm);
        return ErrorHtml("Unrecognized file format.");
    }

    private static string RenderWc3File(byte[] data)
    {
        // Full .wc3 file layout: WonderCard3 | WonderCard3Extra × 2 | MysteryEvent3
        // Japanese: 168 + 80 + 1004 = 1252 bytes
        // International: 336 + 80 + 1004 = 1420 bytes
        // Card-only and card+extras variants are also accepted.
        const int extrasSize = WonderCard3Extra.SIZE * 2;
        const int eventSize = MysteryEvent3.SIZE;
        int cardSize = data.Length switch
        {
            WonderCard3.SIZE_JAP => WonderCard3.SIZE_JAP,
            WonderCard3.SIZE_JAP + extrasSize => WonderCard3.SIZE_JAP,
            WonderCard3.SIZE_JAP + extrasSize + eventSize => WonderCard3.SIZE_JAP,
            WonderCard3.SIZE => WonderCard3.SIZE,
            WonderCard3.SIZE + extrasSize => WonderCard3.SIZE,
            WonderCard3.SIZE + extrasSize + eventSize => WonderCard3.SIZE,
            _ => -1,
        };
        if (cardSize < 0)
            return ErrorHtml($"Unrecognized WC3 file size ({data.Length} bytes).");
        try
        {
            var card = new WonderCard3(new Memory<byte>(data, 0, cardSize));
            return RenderWonderCard3(card);
        }
        catch (Exception ex)
        {
            return ErrorHtml($"Failed to read WC3 card: {ex.Message}");
        }
    }

    public static string RenderWonderCard3(WonderCard3 card)
    {
        var sb = new StringBuilder(1024);
        var title = card.Title.Trim();
        var displayTitle = string.IsNullOrEmpty(title) ? "Wonder Card" : title;
        AppendDocStart(sb, displayTitle);

        sb.Append("<div class=\"pkm\">");
        sb.Append("<div class=\"info\">");
        sb.Append("<h1>").Append(Escape(displayTitle)).Append("</h1>");

        var subtitle = card.Subtitle.Trim();
        if (!string.IsNullOrEmpty(subtitle))
            sb.Append("<div class=\"meta\">").Append(Escape(subtitle)).Append("</div>");

        sb.Append("<dl class=\"details\">");
        AppendDt(sb, "Card ID", card.CardID.ToString(CultureInfo.InvariantCulture));
        AppendDt(sb, "Type", card.Type switch
        {
            0 => "Pokémon",
            1 => "Item",
            2 => "Link Stats",
            _ => card.Type.ToString(CultureInfo.InvariantCulture),
        });
        AppendDt(sb, "Locale", card.Japanese ? "Japanese" : "International");
        sb.Append("</dl>");

        sb.Append("</div>"); // .info
        sb.Append("</div>"); // .pkm
        AppendDocEnd(sb);
        return sb.ToString();
    }

    public static string RenderMysteryGift(DataMysteryGift gift)
    {
        var s = GameInfo.Strings;
        var sb = new StringBuilder(2048);
        var cardName = string.IsNullOrWhiteSpace(gift.CardHeader) ? "Mystery Gift" : gift.CardHeader;
        AppendDocStart(sb, cardName);

        sb.Append("<div class=\"pkm\">");
        // Bundled sprite first (covers item gifts too via SpritePaths); else PokeAPI for species gifts.
        var giftSrc = SpriteResolver?.Invoke(SpritePaths.GetMysteryGiftSprite(gift))
            ?? (gift.Species > 0
                ? PokeApiSpriteUrls.GetPokeApiHomeSpriteUrl(gift.Species, gift.Form, 0, false, 0) ?? PlaceholderSpriteUrl
                : PlaceholderSpriteUrl);
        sb.Append("<div class=\"sprite\"><img alt=\"\" src=\"").Append(giftSrc).Append("\"></div>");

        sb.Append("<div class=\"info\">");
        sb.Append("<h1>").Append(Escape(cardName)).Append("</h1>");
        sb.Append("<div class=\"meta\">")
            .Append(Escape(gift.GetType().Name))
            .Append(" &middot; Gen ").Append(gift.Generation)
            .Append(" &middot; ").Append(Escape(gift.Version.ToString()))
            .Append("</div>");

        sb.Append("<dl class=\"details\">");
        if (gift.Species > 0)
        {
            AppendDt(sb, "Species", Escape(Lookup(s.specieslist, gift.Species)));
            if (gift.Form > 0)
                AppendDt(sb, "Form", gift.Form.ToString(CultureInfo.InvariantCulture));
            AppendDt(sb, "Level", gift.Level.ToString(CultureInfo.InvariantCulture));
            if (gift.IsEgg)
                AppendDt(sb, "Egg", "Yes");
        }
        sb.Append("</dl>");
        sb.Append("</div>"); // .info
        sb.Append("</div>"); // .pkm
        AppendDocEnd(sb);
        return sb.ToString();
    }

    public static string RenderSave(SaveFile sav)
    {
        var s = GameInfo.Strings;
        var sb = new StringBuilder(4096);
        var ot = sav.OT ?? string.Empty;
        AppendDocStart(sb, $"{ot} – {sav.Version}");

        sb.Append("<div class=\"sav\">");
        sb.Append("<h1>").Append(Escape(ot.Length > 0 ? ot : "Trainer")).Append("</h1>");
        sb.Append("<div class=\"meta\">")
            .Append(Escape(sav.Version.ToString()))
            .Append(" &middot; Gen ").Append(sav.Generation)
            .Append(" &middot; ID ").Append(sav.TID16).Append('/').Append(sav.SID16)
            .Append("</div>");

        sb.Append("<dl class=\"details\">");
        AppendDt(sb, "Save type", Escape(sav.GetType().Name));
        AppendDt(sb, "Language", LanguageName(sav.Language));
        // Playtime is more interesting than box capacity (which is near-constant per game).
        // Fall back to box capacity for save types that don't track playtime.
        var played = FormatPlaytime(sav);
        if (played is not null)
        {
            AppendDt(sb, "Played", played);
        }
        else
        {
            AppendDt(sb, "Boxes", $"{sav.BoxCount} × {sav.BoxSlotCount}");
        }
        AppendDt(sb, "Party", (sav.HasParty ? sav.PartyCount : 0).ToString(CultureInfo.InvariantCulture));
        sb.Append("</dl>");

        if (sav.HasParty && sav.PartyCount > 0)
        {
            sb.Append("<h2>Party</h2><div class=\"party\">");
            var count = Math.Min(sav.PartyCount, 6);
            for (var i = 0; i < count; i++)
            {
                var pkm = sav.GetPartySlotAtIndex(i);
                sb.Append("<div class=\"party-slot\">");
                AppendSpriteSmall(sb, pkm);
                sb.Append("<div class=\"party-info\">");
                sb.Append("<div class=\"party-name\">")
                    .Append(Escape(Lookup(s.specieslist, pkm.Species)));
                if (pkm.IsShiny)
                {
                    sb.Append(" <span class=\"shiny\" title=\"Shiny\">★</span>");
                }
                var partyLegality = TryAnalyze(pkm);
                if (partyLegality is not null)
                {
                    var (plabel, pcss) = GetLegalityStatus(partyLegality);
                    sb.Append(" <span class=\"badge ").Append(pcss).Append("\">").Append(plabel).Append("</span>");
                }
                sb.Append("</div>");
                sb.Append("<div class=\"muted\">Lv. ").Append(pkm.CurrentLevel);
                var nick = pkm.Nickname ?? string.Empty;
                if (nick.Length > 0 && nick != Lookup(s.specieslist, pkm.Species))
                {
                    sb.Append(" &middot; ").Append(Escape(nick));
                }
                sb.Append("</div>");
                sb.Append("</div>");
                sb.Append("</div>");
            }
            sb.Append("</div>");
        }

        sb.Append("</div>"); // .sav
        AppendDocEnd(sb);
        return sb.ToString();
    }

    private static void AppendSprite(StringBuilder sb, PKM pkm)
    {
        sb.Append("<div class=\"sprite\"><img alt=\"\" src=\"")
            .Append(ResolveSprite(pkm))
            .Append("\"></div>");
    }

    private static void AppendSpriteSmall(StringBuilder sb, PKM pkm)
    {
        sb.Append("<img class=\"sprite-sm\" alt=\"\" src=\"")
            .Append(ResolveSprite(pkm))
            .Append("\">");
    }

    // Prefer a bundled sprite (offline, via SpriteResolver); fall back to the PokeAPI home sprite.
    private static string ResolveSprite(PKM pkm) =>
        SpriteResolver?.Invoke(SpritePaths.GetPokemonSprite(pkm)) ?? BuildHomeSpriteUrl(pkm);

    // Full PokeAPI home-sprite lookup — handles Mega/regional/gender/Alcremie/Vivillon/etc.
    // For rare forms with no PokeAPI home sprite (Sinistea-Antique, Rockruff-Own-Tempo, GMax, …)
    // the helper returns null; we retry with form 0 so the user still sees the base species
    // instead of a broken image.
    private static string BuildHomeSpriteUrl(PKM pkm)
    {
        var url = PokeApiSpriteUrls.GetPokeApiHomeSpriteUrl(
            pkm.Species, pkm.Form, pkm.GetFormArgument(0), pkm.IsShiny, pkm.Gender);
        if (url is not null)
        {
            return url;
        }

        if (pkm.Species.IsValidSpecies())
        {
            return PokeApiSpriteUrls.GetPokeApiHomeSpriteUrl(
                pkm.Species, form: 0, isShiny: pkm.IsShiny, gender: pkm.Gender)
                ?? PlaceholderSpriteUrl;
        }

        return PlaceholderSpriteUrl;
    }

    private static void AppendStatsTable(StringBuilder sb, PKM pkm)
    {
        sb.Append("<table class=\"stats\"><thead><tr>")
            .Append("<th></th><th>HP</th><th>Atk</th><th>Def</th><th>SpA</th><th>SpD</th><th>Spe</th>")
            .Append("</tr></thead><tbody>");
        AppendStatRow(sb, "IVs", pkm.IV_HP, pkm.IV_ATK, pkm.IV_DEF, pkm.IV_SPA, pkm.IV_SPD, pkm.IV_SPE);
        AppendStatRow(sb, "EVs", pkm.EV_HP, pkm.EV_ATK, pkm.EV_DEF, pkm.EV_SPA, pkm.EV_SPD, pkm.EV_SPE);
        sb.Append("</tbody></table>");
    }

    private static void AppendStatRow(StringBuilder sb, string label, int hp, int atk, int def, int spa, int spd, int spe)
    {
        sb.Append("<tr><th>").Append(label).Append("</th>")
            .Append("<td>").Append(hp).Append("</td>")
            .Append("<td>").Append(atk).Append("</td>")
            .Append("<td>").Append(def).Append("</td>")
            .Append("<td>").Append(spa).Append("</td>")
            .Append("<td>").Append(spd).Append("</td>")
            .Append("<td>").Append(spe).Append("</td>")
            .Append("</tr>");
    }

    private static void AppendMovesTable(StringBuilder sb, PKM pkm, GameStrings s)
    {
        sb.Append("<table class=\"moves\"><thead><tr><th>Move</th><th>PP</th></tr></thead><tbody>");
        var pp = pkm.GetPP();
        ReadOnlySpan<ushort> moves = [pkm.Move1, pkm.Move2, pkm.Move3, pkm.Move4];
        for (var i = 0; i < moves.Length; i++)
        {
            var moveId = moves[i];
            if (moveId == 0)
            {
                continue;
            }
            sb.Append("<tr><td>").Append(Escape(Lookup(s.movelist, moveId))).Append("</td>")
                .Append("<td>").Append(pp[i]).Append('/').Append(pkm.GetMaxPP(i)).Append("</td></tr>");
        }
        sb.Append("</tbody></table>");
    }

    private static void AppendDt(StringBuilder sb, string key, string valueHtml)
    {
        sb.Append("<dt>").Append(key).Append("</dt><dd>").Append(valueHtml).Append("</dd>");
    }

    private static string Lookup(IReadOnlyList<string> list, int index) =>
        (uint)index < (uint)list.Count ? list[index] : string.Empty;

    private static string LanguageName(int language) => language switch
    {
        1 => "Japanese",
        2 => "English",
        3 => "French",
        4 => "Italian",
        5 => "German",
        7 => "Spanish",
        8 => "Korean",
        9 => "Chinese (Simplified)",
        10 => "Chinese (Traditional)",
        _ => "Unknown"
    };

    private static LegalityAnalysis? TryAnalyze(PKM pkm)
    {
        try
        {
            return new LegalityAnalysis(pkm);
        }
        catch
        {
            // Legality is best-effort in a preview — never let it break rendering.
            return null;
        }
    }

    // Overall status for the badge. la.Valid is false only when something is Invalid; Fishy
    // results keep Valid == true, so check for them separately.
    private static (string Label, string Css) GetLegalityStatus(LegalityAnalysis la)
    {
        if (!la.Valid)
        {
            return ("Illegal", "illegal");
        }
        foreach (var result in la.Results)
        {
            if (result.Judgement == Severity.Fishy)
            {
                return ("Fishy", "fishy");
            }
        }
        return ("Legal", "legal");
    }

    private static void AppendLegalityIssues(StringBuilder sb, LegalityAnalysis la)
    {
        var ctx = LegalityLocalizationContext.Create(la);
        var open = false;
        foreach (var result in la.Results)
        {
            if (result.Judgement == Severity.Valid)
            {
                continue;
            }
            if (!open)
            {
                sb.Append("<h2>Legality</h2><ul class=\"issues\">");
                open = true;
            }
            var r = result;
            var cls = result.Judgement == Severity.Fishy ? "fishy" : "invalid";
            sb.Append("<li class=\"").Append(cls).Append("\">").Append(Escape(ctx.Humanize(in r))).Append("</li>");
        }
        if (open)
        {
            sb.Append("</ul>");
        }
    }

    private static void AppendShowdownSet(StringBuilder sb, PKM pkm)
    {
        string text;
        try
        {
            text = new ShowdownSet(pkm).Text;
        }
        catch
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        sb.Append("<h2>Showdown set</h2><pre class=\"set\">").Append(Escape(text)).Append("</pre>");
    }

    // Playtime, or null for save types that don't track it (so the caller can fall back).
    private static string? FormatPlaytime(SaveFile sav)
    {
        int hours = sav.PlayedHours, minutes = sav.PlayedMinutes, seconds = sav.PlayedSeconds;
        if (hours == 0 && minutes == 0 && seconds == 0)
        {
            return null;
        }
        return $"{hours}:{minutes:00}:{seconds:00}";
    }

    private static void AppendDocStart(StringBuilder sb, string title)
    {
        // viewport meta is required for WKWebView on iOS — harmless on macOS/Windows WebViews.
        sb.Append("<!doctype html><html><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, viewport-fit=cover\">")
            .Append("<title>")
            .Append(Escape(title))
            .Append("</title><style>")
            .Append(Css)
            .Append("</style></head><body>");
    }

    private static void AppendDocEnd(StringBuilder sb)
    {
        sb.Append("</body></html>");
    }

    public static string ErrorHtml(string message) =>
        $"<!doctype html><html><head><meta charset=\"utf-8\"><style>{Css}</style></head>" +
        $"<body><p style=\"opacity:0.6\">{System.Net.WebUtility.HtmlEncode(message)}</p></body></html>";

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&#39;"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    // Responsive CSS: stacks vertically on narrow viewports (phone/small QL panels),
    // switches to side-by-side at 600px+ (desktop/tablet). The 17px base works well
    // at normal reading distance on all three platforms (macOS/iOS/Windows).
    private const string Css = """
        :root { color-scheme: light dark; }
        body {
            font: 17px -apple-system, BlinkMacSystemFont, "SF Pro Text", system-ui, sans-serif;
            margin: 0; padding: 20px;
            background: Canvas; color: CanvasText;
        }
        h1 { font-size: 24px; margin: 0 0 4px; font-weight: 600; }
        h2 { font-size: 17px; margin: 20px 0 10px; font-weight: 600; opacity: 0.75; text-transform: uppercase; letter-spacing: 0.04em; }
        .pkm { display: grid; grid-template-columns: 1fr; gap: 16px; align-items: start; }
        .sprite { text-align: center; }
        .sprite img { width: 140px; height: 140px; object-fit: contain; image-rendering: -webkit-optimize-contrast; }
        @media (min-width: 600px) {
            .pkm { grid-template-columns: 160px 1fr; gap: 20px; }
            .sprite { text-align: left; }
            .sprite img { width: 160px; height: 160px; }
        }
        .sprite-sm { width: 56px; height: 56px; object-fit: contain; flex-shrink: 0; }
        .meta { opacity: 0.7; margin-bottom: 14px; }
        .muted { opacity: 0.6; font-weight: normal; }
        .shiny { color: #d4a017; }
        dl.details { display: grid; grid-template-columns: max-content 1fr; gap: 6px 14px; margin: 0 0 14px; }
        dl.details dt { font-weight: 600; opacity: 0.7; }
        dl.details dd { margin: 0; }
        table { border-collapse: collapse; margin: 10px 0; }
        table.stats { width: 100%; }
        table.stats th, table.stats td { padding: 4px 4px; text-align: center; font-variant-numeric: tabular-nums; }
        table.stats thead th { font-weight: 600; opacity: 0.6; font-size: 13px; }
        table.moves { width: 100%; max-width: 480px; }
        table.moves th, table.moves td { padding: 5px 10px; text-align: left; }
        table.moves thead th { font-weight: 600; opacity: 0.6; font-size: 13px; border-bottom: 1px solid color-mix(in srgb, CanvasText 15%, transparent); }
        table.moves td:last-child { text-align: right; font-variant-numeric: tabular-nums; opacity: 0.7; }
        .party { display: grid; grid-template-columns: 1fr; gap: 10px; }
        @media (min-width: 600px) { .party { grid-template-columns: repeat(2, 1fr); } }
        .party-slot { display: flex; align-items: center; gap: 12px; padding: 10px; border: 1px solid color-mix(in srgb, CanvasText 12%, transparent); border-radius: 8px; }
        .party-name { font-weight: 600; font-size: 17px; }
        .badge { display: inline-block; padding: 1px 9px; border-radius: 999px; font-size: 13px; font-weight: 600; vertical-align: middle; }
        .badge.legal { color: #1a7f37; background: color-mix(in srgb, #1a7f37 16%, transparent); }
        .badge.fishy { color: #9a6700; background: color-mix(in srgb, #9a6700 18%, transparent); }
        .badge.illegal { color: #cf222e; background: color-mix(in srgb, #cf222e 16%, transparent); }
        ul.issues { margin: 6px 0 14px; padding-left: 20px; }
        ul.issues li { margin: 3px 0; }
        ul.issues li.invalid { color: #cf222e; }
        ul.issues li.fishy { color: #9a6700; }
        pre.set {
            font: 13px ui-monospace, "SF Mono", SFMono-Regular, Menlo, Consolas, monospace;
            background: color-mix(in srgb, CanvasText 7%, transparent);
            padding: 12px 14px; border-radius: 8px; overflow-x: auto; white-space: pre; margin: 6px 0 16px;
        }
        @media (prefers-color-scheme: dark) {
            .badge.legal { color: #3fb950; background: color-mix(in srgb, #3fb950 20%, transparent); }
            .badge.fishy { color: #d29922; background: color-mix(in srgb, #d29922 22%, transparent); }
            .badge.illegal { color: #f85149; background: color-mix(in srgb, #f85149 22%, transparent); }
            ul.issues li.invalid { color: #f85149; }
            ul.issues li.fishy { color: #d29922; }
        }
        """;
}
