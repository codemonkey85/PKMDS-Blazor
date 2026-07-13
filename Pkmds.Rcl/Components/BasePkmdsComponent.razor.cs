namespace Pkmds.Rcl.Components;

public partial class BasePkmdsComponent
{
    protected static readonly IReadOnlyDictionary<string, string> FlagLabels =
        new Dictionary<string, string>
        {
            ["contact"] = "Makes Contact",
            ["charge"] = "Has Charge Turn",
            ["recharge"] = "Must Recharge",
            ["protect"] = "Blocked by Protect",
            ["reflectable"] = "Reflectable",
            ["snatch"] = "Snatchable",
            ["mirror"] = "Copied by Mirror Move",
            ["punch"] = "Punch-based",
            ["sound"] = "Sound-based",
            ["gravity"] = "Unusable during Gravity",
            ["defrost"] = "Defrosts User",
            ["distance"] = "Hits Non-adjacent Targets",
            ["heal"] = "Heals",
            ["authentic"] = "Bypasses Substitute",
            ["mental"] = "Mental Herb / Overcoat",
            ["powder"] = "Powder-based",
            ["bite"] = "Jaw-based",
            ["pulse"] = "Pulse-based",
            ["ballistics"] = "Ballistics-based",
            ["non-sky-battle"] = "Unusable in Sky Battles",
            ["dance"] = "Dance",
            ["wind"] = "Wind-based",
            ["slicing"] = "Slicing-based"
        };

    /// <summary>
    /// Minimum save-file generation in which a flag's associated mechanic exists.
    /// Flags not listed here are relevant in all generations.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, int> FlagMinGeneration =
        new Dictionary<string, int>
        {
            ["reflectable"] = 3, // Magic Coat — Gen 3
            ["snatch"] = 3, // Snatch — Gen 3
            ["punch"] = 4, // Iron Fist — Gen 4
            ["gravity"] = 4, // Gravity move — Gen 4
            ["authentic"] = 4, // Substitute interaction — Gen 4
            ["heal"] = 4, // Heal Block — Gen 4
            ["mental"] = 5, // Overcoat / Mental Herb — Gen 5
            ["bite"] = 6, // Strong Jaw — Gen 6
            ["ballistics"] = 6, // Bulletproof — Gen 6
            ["powder"] = 6, // Overcoat powder immunity — Gen 6
            ["pulse"] = 6, // Mega Launcher — Gen 6
            ["non-sky-battle"] = 6, // Sky Battles — Gen 6
            ["dance"] = 7, // Dancer — Gen 7
            ["wind"] = 9, // Wind Rider / Wind Power — Gen 9
            ["slicing"] = 9 // Sharpness — Gen 9
        };

    protected async Task OpenTrashBytesEditorAsync(PKM? pokemon, StringSource field)
    {
        var parameters = new DialogParameters<TrashBytesEditorDialog> { { x => x.Pokemon, pokemon }, { x => x.Field, field } };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<TrashBytesEditorDialog>("Trash Bytes Editor", parameters, options);
    }

    /// <summary>
    /// Returns the nature modifier for a given stat, taking into account Gen 8+
    /// StatAlignment vs Nature.
    /// </summary>
    protected static NatureModifier GetNatureModifier(Nature nature, Stats stat)
    {
        var (up, dn) = nature.GetNatureModification();
        if (up == dn)
        {
            return NatureModifier.Neutral;
        }

        if (up == (int)stat)
        {
            return NatureModifier.Boosted;
        }

        return dn == (int)stat
            ? NatureModifier.Hindered
            : NatureModifier.Neutral;
    }

    protected static string GetNatureAdornmentIcon(NatureModifier modifier) => modifier switch
    {
        NatureModifier.Boosted => Icons.Material.Filled.ArrowUpward,
        NatureModifier.Hindered => Icons.Material.Filled.ArrowDownward,
        _ => string.Empty
    };

    protected static Color GetNatureAdornmentColor(NatureModifier modifier) => modifier switch
    {
        NatureModifier.Boosted => Color.Error,
        NatureModifier.Hindered => Color.Info,
        _ => Color.Default
    };

    protected static Adornment GetNatureAdornment(NatureModifier modifier) =>
        modifier is NatureModifier.Neutral
            ? Adornment.None
            : Adornment.End;

    protected static string GetNatureTooltip(NatureModifier modifier) => modifier switch
    {
        NatureModifier.Boosted => "Boosted by nature (+10%)",
        NatureModifier.Hindered => "Hindered by nature (-10%)",
        _ => string.Empty
    };

    /// <summary>Returns true if the flag should be shown for the given save-file generation.</summary>
    protected static bool IsFlagRelevant(string flag, int saveGeneration) =>
        !FlagMinGeneration.TryGetValue(flag, out var minGen) || saveGeneration >= minGen;

    protected static string FormatPriority(int priority) =>
        priority > 0
            ? $"+{priority}"
            : priority.ToString();
}
