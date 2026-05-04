namespace Pkmds.Rcl.Components;

public partial class LetsGoBoxGrid
{
    private async Task SetSelectedPokemon(PKM? pokemon, int slotNumber)
    {
        if (!await UnsavedChangesGuard.ConfirmAsync(
                AppService,
                DialogService,
                "This Pokémon has unsaved changes. Save them to the slot before switching to another?",
                snackbar: Snackbar))
        {
            return;
        }

        AppService.SetSelectedLetsGoPokemon(pokemon, slotNumber);
    }

    private string GetClass(int slotNumber) => AppState.SelectedBoxSlotNumber == slotNumber
        ? Constants.SelectedSlotClass
        : string.Empty;
}
