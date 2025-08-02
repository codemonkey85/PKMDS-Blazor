namespace Pkmds.Rcl.Components;

public partial class LetsGoBoxGrid
{
    private void SetSelectedPokemon(PKM? pokemon, int slotNumber) =>
        AppService.SetSelectedLetsGoPokemon(pokemon, slotNumber);

    private string GetClass(int slotNumber) => AppState.SelectedBoxSlotNumber == slotNumber
        ? Constants.SelectedSlotClass
        : string.Empty;
}
