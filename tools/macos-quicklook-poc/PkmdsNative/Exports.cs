using System.Runtime.InteropServices;
using System.Text;
using PKHeX.Core;
using Pkmds.Preview;

namespace Pkmds.Native;

public static unsafe class Exports
{
    [UnmanagedCallersOnly(EntryPoint = "pkmds_describe_pkm")]
    public static int DescribePkm(byte* data, int length, byte* outJson, int outCap)
    {
        try
        {
            if (data is null || outJson is null || length <= 0 || outCap <= 0)
                return -1;

            var bytes = CopyIn(data, length);
            var pkm = EntityFormat.GetFromBytes(bytes);
            if (pkm is null)
                return -2;

            return WriteJson(BuildPkmJson(pkm), outJson, outCap);
        }
        catch
        {
            return -99;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "pkmds_describe_save")]
    public static int DescribeSave(byte* data, int length, byte* outJson, int outCap)
    {
        try
        {
            if (data is null || outJson is null || length <= 0 || outCap <= 0)
                return -1;

            var bytes = CopyIn(data, length);
            if (!SaveUtil.TryGetSaveFile(bytes, out var sav))
                return -2;

            return WriteJson(BuildSaveJson(sav), outJson, outCap);
        }
        catch
        {
            return -99;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "pkmds_render_pkm_html")]
    public static int RenderPkmHtml(byte* data, int length, byte* outHtml, int outCap)
    {
        try
        {
            if (data is null || outHtml is null || length <= 0 || outCap <= 0)
                return -1;

            var bytes = CopyIn(data, length);
            var pkm = EntityFormat.GetFromBytes(bytes);
            if (pkm is null)
                return -2;

            return WriteJson(HtmlRenderer.RenderPkm(pkm), outHtml, outCap);
        }
        catch
        {
            return -99;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "pkmds_render_save_html")]
    public static int RenderSaveHtml(byte* data, int length, byte* outHtml, int outCap)
    {
        try
        {
            if (data is null || outHtml is null || length <= 0 || outCap <= 0)
                return -1;

            var bytes = CopyIn(data, length);
            if (!SaveUtil.TryGetSaveFile(bytes, out var sav))
                return -2;

            return WriteJson(HtmlRenderer.RenderSave(sav), outHtml, outCap);
        }
        catch
        {
            return -99;
        }
    }

    // Returns the relative bundled-sprite path for a file (e.g. "a/a_448.png").
    // ext/extLen: UTF-8 file extension without leading dot (e.g. "pk5"), no NUL terminator needed.
    // Used by macOS/iOS QLThumbnailProvider to pick a sprite without re-rendering HTML.
    [UnmanagedCallersOnly(EntryPoint = "pkmds_get_sprite_path")]
    public static int GetSpritePath(byte* data, int length, byte* ext, int extLen, byte* outPath, int outCap)
    {
        try
        {
            if (data is null || outPath is null || length <= 0 || outCap <= 0)
                return -1;

            var bytes = CopyIn(data, length);
            var extStr = ext is null || extLen <= 0 ? "" : Encoding.UTF8.GetString(ext, extLen);
            return WriteJson(FileSprite.GetRelativeSpritePath(bytes, extStr), outPath, outCap);
        }
        catch
        {
            return -99;
        }
    }

    // Auto-dispatching renderer: save → mystery gift (extension-guided) → PKM entity.
    // ext/extLen: UTF-8 file extension without leading dot (e.g. "wb8"), no NUL terminator needed.
    [UnmanagedCallersOnly(EntryPoint = "pkmds_render_file_html")]
    public static int RenderFileHtml(byte* data, int length, byte* ext, int extLen, byte* outHtml, int outCap)
    {
        try
        {
            if (data is null || outHtml is null || length <= 0 || outCap <= 0)
                return -1;

            var bytes = CopyIn(data, length);
            var extStr = ext is null || extLen <= 0 ? "" : Encoding.UTF8.GetString(ext, extLen);
            return WriteJson(HtmlRenderer.RenderFile(bytes, extStr), outHtml, outCap);
        }
        catch
        {
            return -99;
        }
    }

    private static byte[] CopyIn(byte* data, int length)
    {
        var bytes = new byte[length];
        Marshal.Copy((nint)data, bytes, 0, length);
        return bytes;
    }

    private static int WriteJson(string json, byte* outJson, int outCap)
    {
        var encoded = Encoding.UTF8.GetBytes(json);
        if (encoded.Length + 1 > outCap)
            return -3;

        Marshal.Copy(encoded, 0, (nint)outJson, encoded.Length);
        outJson[encoded.Length] = 0;
        return encoded.Length;
    }

    private static string BuildPkmJson(PKM pkm)
    {
        var s = GameInfo.Strings;
        var sb = new StringBuilder(1024);
        sb.Append('{');
        AppendInt(sb, "format", pkm.Format); sb.Append(',');
        AppendInt(sb, "species", pkm.Species); sb.Append(',');
        AppendString(sb, "speciesName", Lookup(s.specieslist, pkm.Species)); sb.Append(',');
        AppendInt(sb, "form", pkm.Form); sb.Append(',');
        AppendInt(sb, "level", pkm.CurrentLevel); sb.Append(',');
        AppendInt(sb, "ability", pkm.Ability); sb.Append(',');
        AppendString(sb, "abilityName", Lookup(s.abilitylist, pkm.Ability)); sb.Append(',');
        AppendInt(sb, "nature", (int)pkm.Nature); sb.Append(',');
        AppendString(sb, "natureName", Lookup(s.natures, (int)pkm.Nature)); sb.Append(',');
        AppendInt(sb, "tid", pkm.TID16); sb.Append(',');
        AppendInt(sb, "sid", pkm.SID16); sb.Append(',');
        AppendBool(sb, "isShiny", pkm.IsShiny); sb.Append(',');
        AppendString(sb, "ot", pkm.OriginalTrainerName ?? string.Empty); sb.Append(',');
        AppendString(sb, "nickname", pkm.Nickname ?? string.Empty); sb.Append(',');
        AppendIntArray(sb, "ivs", [pkm.IV_HP, pkm.IV_ATK, pkm.IV_DEF, pkm.IV_SPA, pkm.IV_SPD, pkm.IV_SPE]); sb.Append(',');
        AppendIntArray(sb, "evs", [pkm.EV_HP, pkm.EV_ATK, pkm.EV_DEF, pkm.EV_SPA, pkm.EV_SPD, pkm.EV_SPE]); sb.Append(',');
        AppendIntArray(sb, "moves", [pkm.Move1, pkm.Move2, pkm.Move3, pkm.Move4]); sb.Append(',');
        AppendStringArray(sb, "moveNames",
            Lookup(s.movelist, pkm.Move1),
            Lookup(s.movelist, pkm.Move2),
            Lookup(s.movelist, pkm.Move3),
            Lookup(s.movelist, pkm.Move4));
        sb.Append('}');
        return sb.ToString();
    }

    private static string BuildSaveJson(SaveFile sav)
    {
        var sb = new StringBuilder(512);
        sb.Append('{');
        AppendString(sb, "type", sav.GetType().Name); sb.Append(',');
        AppendString(sb, "version", sav.Version.ToString()); sb.Append(',');
        AppendInt(sb, "generation", sav.Generation); sb.Append(',');
        AppendString(sb, "ot", sav.OT ?? string.Empty); sb.Append(',');
        AppendInt(sb, "tid", sav.TID16); sb.Append(',');
        AppendInt(sb, "sid", sav.SID16); sb.Append(',');
        AppendInt(sb, "language", sav.Language); sb.Append(',');
        AppendInt(sb, "boxCount", sav.BoxCount); sb.Append(',');
        AppendInt(sb, "boxSlotCount", sav.BoxSlotCount); sb.Append(',');
        AppendInt(sb, "partyCount", sav.HasParty ? sav.PartyCount : 0); sb.Append(',');
        AppendInt(sb, "playedHours", sav.PlayedHours); sb.Append(',');
        AppendInt(sb, "playedMinutes", sav.PlayedMinutes); sb.Append(',');
        sb.Append("\"party\":[");
        if (sav.HasParty)
        {
            var count = Math.Min(sav.PartyCount, 6);
            var s = GameInfo.Strings;
            for (var i = 0; i < count; i++)
            {
                var pkm = sav.GetPartySlotAtIndex(i);
                if (i > 0) sb.Append(',');
                sb.Append('{');
                AppendInt(sb, "species", pkm.Species); sb.Append(',');
                AppendString(sb, "speciesName", Lookup(s.specieslist, pkm.Species)); sb.Append(',');
                AppendInt(sb, "level", pkm.CurrentLevel); sb.Append(',');
                AppendString(sb, "nickname", pkm.Nickname ?? string.Empty);
                sb.Append('}');
            }
        }
        sb.Append(']');
        sb.Append('}');
        return sb.ToString();
    }

    private static string Lookup(IReadOnlyList<string> list, int index)
        => (uint)index < (uint)list.Count ? list[index] : string.Empty;

    private static void AppendInt(StringBuilder sb, string key, int value)
    {
        sb.Append('"').Append(key).Append("\":").Append(value);
    }

    private static void AppendBool(StringBuilder sb, string key, bool value)
    {
        sb.Append('"').Append(key).Append("\":").Append(value ? "true" : "false");
    }

    private static void AppendString(StringBuilder sb, string key, string value)
    {
        sb.Append('"').Append(key).Append("\":");
        AppendStringValue(sb, value);
    }

    private static void AppendStringValue(StringBuilder sb, string value)
    {
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    private static void AppendIntArray(StringBuilder sb, string key, ReadOnlySpan<int> values)
    {
        sb.Append('"').Append(key).Append("\":[");
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(values[i]);
        }
        sb.Append(']');
    }

    private static void AppendStringArray(StringBuilder sb, string key, params string[] values)
    {
        sb.Append('"').Append(key).Append("\":[");
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(',');
            AppendStringValue(sb, values[i]);
        }
        sb.Append(']');
    }
}
