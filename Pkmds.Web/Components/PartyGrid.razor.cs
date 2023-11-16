namespace Pkmds.Web.Components;

public partial class PartyGrid
{
    private void SetSelectedPokemon(PKM? pokemon, int slotNumber) =>
        AppService.SetSelectedPartyPokemon(pokemon, slotNumber);

    private string GetStyle(int slotNumber) => AppState.SelectedPartySlotNumber == slotNumber
        ? "border: 4px solid orange; border-radius: 6px;"
        : string.Empty;

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += StateHasChanged;
        RefreshService.OnPartyStateChanged += StateHasChanged;
    }

    public void Dispose()
    {
        RefreshService.OnAppStateChanged -= StateHasChanged;
        RefreshService.OnPartyStateChanged -= StateHasChanged;
    }
}
