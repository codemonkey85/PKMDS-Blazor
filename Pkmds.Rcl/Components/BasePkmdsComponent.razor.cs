namespace Pkmds.Rcl.Components;

public partial class BasePkmdsComponent
{
    /// <summary>
    /// Returns the nature modifier for a given stat, taking into account Gen 8+
    /// StatNature vs Nature.
    /// </summary>
    protected static NatureModifier GetNatureModifier(Nature nature, Stats stat)
    {
        var (up, dn) = nature.GetNatureModification();
        if (up == dn)
            return NatureModifier.Neutral;
        if (up == (int)stat)
            return NatureModifier.Boosted;
        return dn == (int)stat ? NatureModifier.Hindered : NatureModifier.Neutral;
    }

    protected static string GetNatureAdornmentIcon(NatureModifier modifier) => modifier switch
    {
        NatureModifier.Boosted => Icons.Material.Filled.ArrowUpward,
        NatureModifier.Hindered => Icons.Material.Filled.ArrowDownward,
        _ => string.Empty,
    };

    protected static Color GetNatureAdornmentColor(NatureModifier modifier) => modifier switch
    {
        NatureModifier.Boosted => Color.Error,
        NatureModifier.Hindered => Color.Info,
        _ => Color.Default,
    };

    protected static Adornment GetNatureAdornment(NatureModifier modifier) =>
        modifier is NatureModifier.Neutral ? Adornment.None : Adornment.End;

    protected static string GetNatureTooltip(NatureModifier modifier) => modifier switch
    {
        NatureModifier.Boosted => "Boosted by nature (+10%)",
        NatureModifier.Hindered => "Hindered by nature (-10%)",
        _ => string.Empty,
    };
}
