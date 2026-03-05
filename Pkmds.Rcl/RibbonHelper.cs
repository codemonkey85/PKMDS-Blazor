namespace Pkmds.Rcl;

/// <summary>
/// Helper class for ribbon-related operations in the ribbon editor.
/// </summary>
public static class RibbonHelper
{
    /// <summary>Gets a human-friendly display name for a ribbon property name.</summary>
    /// <remarks>
    /// Uses <see cref="RibbonStrings" /> from <see cref="GameInfo.Strings" /> when available, falling back to the
    /// property name itself.
    /// </remarks>
    /// <param name="propertyName">The property name, e.g. "RibbonChampionKalos".</param>
    public static string GetRibbonDisplayName(string propertyName) =>
        GameInfo.Strings.Ribbons.GetNameSafe(propertyName, out var name)
            ? name
            : propertyName;

    /// <summary>Gets the sprite path for a ribbon by its <see cref="RibbonInfo" />.</summary>
    public static string GetRibbonSprite(RibbonInfo info) =>
        ImageHelper.GetRibbonIconSpriteFileName(
            info.Name.StartsWith("Ribbon", StringComparison.Ordinal)
                ? info.Name["Ribbon".Length..]
                : info.Name);

    /// <summary>
    /// Returns <see langword="true" /> when the ribbon name corresponds to a personality mark
    /// (Gen 8 encounter marks or Gen 9 alpha/mightiest/titan marks).
    /// </summary>
    public static bool IsMarkEntry(string name)
    {
        var indexName = name.StartsWith("Ribbon", StringComparison.Ordinal)
            ? name["Ribbon".Length..]
            : name;
        if (!Enum.TryParse<RibbonIndex>(indexName, out var index))
        {
            return false;
        }

        // Personality marks: Gen 8 (MarkLunchtime..MarkSlump) and Gen 9 (MarkAlpha, MarkMightiest, MarkTitan)
        return index is >= RibbonIndex.MarkLunchtime and <= RibbonIndex.MarkSlump
            or RibbonIndex.MarkAlpha or RibbonIndex.MarkMightiest or RibbonIndex.MarkTitan;
    }

    /// <summary>
    /// Gets all ribbon info entries for the given Pokémon, or an empty list if <paramref name="pokemon" /> is
    /// <see langword="null" />.
    /// </summary>
    public static List<RibbonInfo> GetAllRibbonInfo(PKM? pokemon) =>
        pokemon is not null
            ? RibbonInfo.GetRibbonInfo(pokemon)
            : [];
}
