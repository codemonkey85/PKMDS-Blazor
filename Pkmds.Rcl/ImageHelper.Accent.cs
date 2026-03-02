namespace Pkmds.Rcl;

/// <summary>
/// Partial class for accent icon sprite filename generation (gender, lock, party, legality indicators, etc.).
/// </summary>
public static partial class ImageHelper
{
    /// <summary>Gets the sprite filename for a gender indicator icon (0=male, 1=female, 2=genderless).</summary>
    public static string GetGenderIconSpriteFileName(byte gender) =>
        $"{SpritesRoot}ac/gender_{gender}.png";

    /// <summary>Gets the sprite filename for a lock icon (indicating a locked Pokémon).</summary>
    public static string GetLockIconSpriteFileName() =>
        $"{SpritesRoot}ac/locked.png";

    /// <summary>Gets the sprite filename for a party indicator icon.</summary>
    public static string GetPartyIndicatorSpriteFileName() =>
        $"{SpritesRoot}ac/party.png";

    /// <summary>Gets the sprite filename for a legality indicator (valid Pokémon).</summary>
    public static string GetLegalityValidSpriteFileName() =>
        $"{SpritesRoot}ac/valid.png";

    /// <summary>Gets the sprite filename for a legality warning indicator (invalid Pokémon).</summary>
    public static string GetLegalityWarnSpriteFileName() =>
        $"{SpritesRoot}ac/warn.png";

    /// <summary>Gets the sprite filename for a box wallpaper background (clean or default style).</summary>
    public static string GetBoxWallpaperBackgroundSpriteFileName(bool isClean = false) =>
        $"{SpritesRoot}ac/box_wp_{(isClean ? "clean" : "default")}.png";

    /// <summary>Gets the sprite filename for a ribbon affix state indicator.</summary>
    public static string GetRibbonAffixSpriteFileName(string affixState = "none") =>
        $"{SpritesRoot}ac/ribbon_affix_{affixState}.png";

    /// <summary>Gets the sprite filename for an origin mark icon, or null if there is no origin mark.</summary>
    public static string? GetOriginMarkSpriteFileName(OriginMark originMark) => originMark switch
    {
        OriginMark.Gen6Pentagon => $"{SpritesRoot}m/gen_6.png",
        OriginMark.Gen7Clover => $"{SpritesRoot}m/gen_7.png",
        OriginMark.Gen8Galar => $"{SpritesRoot}m/gen_8.png",
        OriginMark.Gen8Trio => $"{SpritesRoot}m/gen_bs.png",
        OriginMark.Gen8Arc => $"{SpritesRoot}m/gen_la.png",
        OriginMark.Gen9Paldea => $"{SpritesRoot}m/gen_sv.png",
        OriginMark.Gen9ZA => $"{SpritesRoot}m/gen_za.png",
        OriginMark.GameBoy => $"{SpritesRoot}m/gen_vc.png",
        OriginMark.GO => $"{SpritesRoot}m/gen_go.png",
        OriginMark.LetsGo => $"{SpritesRoot}m/gen_gg.png",
        _ => null,
    };
}
