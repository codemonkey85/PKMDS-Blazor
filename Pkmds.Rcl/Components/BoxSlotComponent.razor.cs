namespace Pkmds.Rcl.Components;

public partial class BoxSlotComponent
{
    [Parameter]
    public PKM? Pokemon { get; set; }

    private void SetSelectedPokemon()
    {
        AppState.SelectedPokemon = Pokemon;
        AppState.Refresh();
    }
}
