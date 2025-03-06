namespace Pkmds.Rcl.Components;

public partial class LetsGoBoxGrid
{
    private void SetSelectedPokemon(PKM? pokemon, int slotNumber) =>
        AppService.SetSelectedLetsGoPokemon(pokemon, slotNumber);

    private string GetStyle(int slotNumber) => AppState.SelectedBoxSlotNumber == slotNumber
        ? Constants.SelectedSlotStyle
        : string.Empty;
}
