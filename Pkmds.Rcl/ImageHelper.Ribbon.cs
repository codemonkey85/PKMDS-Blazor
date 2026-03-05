namespace Pkmds.Rcl;

/// <summary>
/// Partial class for ribbon icon sprite filename generation.
/// </summary>
public static partial class ImageHelper
{
    /// <summary>Gets the sprite filename for a ribbon icon by ribbon name.</summary>
    /// <remarks>Ribbon names follow the pattern: ribbon[name].png</remarks>
    public static string GetRibbonIconSpriteFileName(string ribbonName)
    {
        var name = ribbonName.ToLowerInvariant();
        return $"{SpritesRoot}r/ribbon{name}.png";
    }
}
