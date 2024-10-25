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

        Pokemon.SetNature(nature);
        AppService.LoadPokemonStats(Pokemon);
    }

    private static int GetEvMax(int generation) => generation switch 
    {
        1 or 2 => EffortValues.Max12,
        3 or 4 or 5 => EffortValues.Max255,
        _ => EffortValues.Max252
    };
}
