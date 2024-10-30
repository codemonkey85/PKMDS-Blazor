namespace Pkmds.Web.Components.EditForms.Tabs;

public partial class StatsTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    public static string GetCharacteristic(PKM? pokemon) =>
        pokemon?.Characteristic is int characteristicIndex &&
        characteristicIndex > -1 &&
        GameInfo.Strings.characteristics is { Length: > 0 } characteristics &&
        characteristicIndex < characteristics.Length
            ? characteristics[characteristicIndex]
            : string.Empty;

    private void OnNatureSet(Nature nature)
    {
        if (Pokemon is null)
        {
            return;
        }

        if (!nature.IsFixed())
        {
            nature = 0; // default valid
        }

        switch (Pokemon.Format)
        {
            case 3 or 4:
                Pokemon.SetPIDNature(nature);
                break;
            default:
                Pokemon.Nature = nature;
                break;
        }

        AppService.LoadPokemonStats(Pokemon);
    }

    private void OnStatNatureSet(Nature statNature)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.StatNature = statNature;
        AppService.LoadPokemonStats(Pokemon);
    }

    private static int GetEvMax(int generation) => generation switch
    {
        1 or 2 => EffortValues.Max12,
        3 or 4 or 5 => EffortValues.Max255,
        _ => EffortValues.Max252
    };

    private string GetStatClass(Stats stat)
    {
        if (Pokemon is not INature)
        {
            return string.Empty;
        }

        var nature = Pokemon.Format >= 8 ? Pokemon.StatNature : Pokemon.Nature;

        var (up, dn) = NatureAmp.GetNatureModification(nature);

        return up == dn
            ? string.Empty
            : up == (int)stat
                ? "plus-nature"
                : dn == (int)stat
                    ? "minus-nature"
                    : string.Empty;
    }

    private enum Stats
    {
        Attack,
        Defense,
        Speed,
        SpecialAttack,
        SpecialDefense
    }

    private static string GetTeraTypeDisplayName(byte teraTypeId) => teraTypeId == TeraTypeUtil.Stellar
        ? GameInfo.Strings.Types[TeraTypeUtil.StellarTypeDisplayStringIndex]
        : GameInfo.Strings.Types[teraTypeId];
}
