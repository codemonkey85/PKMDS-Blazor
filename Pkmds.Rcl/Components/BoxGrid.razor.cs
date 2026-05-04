namespace Pkmds.Rcl.Components;

public partial class BoxGrid : RefreshAwareComponent
{
    [Parameter]
    [EditorRequired]
    public int BoxNumber { get; set; }

    private string BoxGridClass =>
        AppState.SaveFile?.BoxSlotCount == 20
            ? "w-80 h-[400px] grid grid-cols-4 grid-rows-5 gap-1"
            : "grid grid-cols-6 gap-1 w-full aspect-[6/5] mx-auto";

    private async Task SetSelectedPokemon(PKM? pokemon, int boxNumber, int slotNumber)
    {
        if (!await UnsavedChangesGuard.ConfirmAsync(
                AppService,
                DialogService,
                "This Pokémon has unsaved changes. Save them to the slot before switching to another?",
                snackbar: Snackbar))
        {
            return;
        }

        AppService.SetSelectedBoxPokemon(pokemon, boxNumber, slotNumber);
    }

    private string GetClass(int slotNumber) =>
        AppState.SelectedBoxNumber == BoxNumber && AppState.SelectedBoxSlotNumber == slotNumber
            ? Constants.SelectedSlotClass
            : string.Empty;
}
