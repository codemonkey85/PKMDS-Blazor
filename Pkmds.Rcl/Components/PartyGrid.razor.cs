using Pkmds.Rcl.Components.Dialogs;

namespace Pkmds.Rcl.Components;

public partial class PartyGrid : IDisposable
{
    public void Dispose()
    {
        RefreshService.OnAppStateChanged -= StateHasChanged;
        RefreshService.OnPartyStateChanged -= StateHasChanged;
    }

    private void SetSelectedPokemon(PKM? pokemon, int slotNumber) =>
        AppService.SetSelectedPartyPokemon(pokemon, slotNumber);

    private string GetStyle(int slotNumber) => AppState.SelectedPartySlotNumber == slotNumber
        ? Constants.SelectedSlotStyle
        : string.Empty;

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += StateHasChanged;
        RefreshService.OnPartyStateChanged += StateHasChanged;
    }

    private void ExportAsShowdown() =>
        DialogService.ShowAsync<ShowdownExportDialog>(
            "Showdown Export",
            new DialogOptions { CloseOnEscapeKey = true });
}
