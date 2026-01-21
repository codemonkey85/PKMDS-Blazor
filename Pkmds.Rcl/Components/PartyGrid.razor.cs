namespace Pkmds.Rcl.Components;

public partial class PartyGrid : RefreshAwareComponent
{
    protected override RefreshEvents SubscribeTo => RefreshEvents.AppState | RefreshEvents.PartyState;

    private void SetSelectedPokemon(PKM? pokemon, int slotNumber) =>
        AppService.SetSelectedPartyPokemon(pokemon, slotNumber);

    private string GetClass(int slotNumber) => AppState.SelectedPartySlotNumber == slotNumber
        ? Constants.SelectedSlotClass
        : string.Empty;

    private void ExportAsShowdown() =>
        DialogService.ShowAsync<ShowdownExportDialog>(
            "Showdown Export",
            new DialogOptions { CloseOnEscapeKey = true });
}
