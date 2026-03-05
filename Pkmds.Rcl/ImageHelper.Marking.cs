namespace Pkmds.Rcl;

/// <summary>
/// Partial class for marking sprite filename generation (box marks, generation origin, shiny, region indicators).
/// </summary>
public static partial class ImageHelper
{
    /// <summary>Gets the sprite filename for a box mark/marking icon (1-6, representing the colored marks).</summary>
    public static string GetBoxMarkSpriteFileName(int markNumber) =>
        $"{SpritesRoot}m/box_mark_{markNumber:00}.png";

    /// <summary>Gets the sprite filename for a generation origin icon (gen_6, gen_7, gen_8, etc.).</summary>
    public static string GetGenerationOriginSpriteFileName(string generationCode)
    {
        var code = generationCode.ToLowerInvariant();
        return $"{SpritesRoot}m/gen_{code}.png";
    }

    /// <summary>Gets the sprite filename for a shiny indicator (rare icon).</summary>
    public static string GetShinyIndicatorSpriteFileName(bool isAlt = false) =>
        $"{SpritesRoot}m/rare_icon{(isAlt ? "_2" : string.Empty)}.png";

    /// <summary>Gets the sprite filename for an Alola origin indicator.</summary>
    public static string GetAlolaOriginSpriteFileName() =>
        $"{SpritesRoot}m/alora.png";

    /// <summary>Gets the sprite filename for a Galar region crown indicator (Crown Tundra).</summary>
    public static string GetGalarCrownSpriteFileName() =>
        $"{SpritesRoot}m/crown.png";

    /// <summary>Gets the sprite filename for a Hisui origin indicator.</summary>
    public static string GetHisuiOriginSpriteFileName() =>
        $"{SpritesRoot}m/leaf.png";

    /// <summary>Gets the sprite filename for a Virtual Console indicator.</summary>
    public static string GetVirtualConsoleSpriteFileName() =>
        $"{SpritesRoot}m/vc.png";

    /// <summary>Gets the sprite filename for a Battle ROM indicator.</summary>
    public static string GetBattleRomIndicatorSpriteFileName() =>
        $"{SpritesRoot}m/icon_btlrom.png";

    /// <summary>Gets the sprite filename for a favorite/marked indicator.</summary>
    public static string GetFavoriteIndicatorSpriteFileName() =>
        $"{SpritesRoot}m/icon_favo.png";

    /// <summary>Gets the sprite filename for an anti-Pokerus icon.</summary>
    public static string GetAntiPokerusIconSpriteFileName() =>
        $"{SpritesRoot}m/anti_pokerus_icon.png";

    /// <summary>Gets the sprite filename for a Pokémon Go indicator.</summary>
    public static string GetPokemonGoIndicatorSpriteFileName() =>
        $"{SpritesRoot}m/gen_go.png";
}
