namespace Pkmds.Rcl.Components;

public partial class PartyGrid : RefreshAwareComponent
{
    protected override RefreshEvents SubscribeTo => RefreshEvents.AppState | RefreshEvents.PartyState;

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

        AppService.SetSelectedPartyPokemon(pokemon, slotNumber);
    }

    private string GetClass(int slotNumber) => AppState.SelectedPartySlotNumber == slotNumber
        ? Constants.SelectedSlotClass
        : string.Empty;

    private async Task ExportAsShowdown()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<ShowdownExportDialog>("Showdown Export", options);
    }

    private async Task ExportToPokePaste()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<PokePasteExportDialog>(
            "Export to PokePaste",
            new DialogParameters<PokePasteExportDialog>(),
            options);
    }

    private async Task ImportFromShowdown()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<ShowdownImportDialog>(
            "Import from Showdown / PokePaste",
            new DialogParameters<ShowdownImportDialog>(),
            options);
    }
}
