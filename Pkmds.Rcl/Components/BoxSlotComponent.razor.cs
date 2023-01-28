namespace Pkmds.Rcl.Components;

public partial class BoxSlotComponent
{
    [Parameter]
    public PKM? Pokemon { get; set; }

    private void SetPreviewPokemon()
    {
        AppState.SelectedPokemon = Pokemon;
        AppState.Refresh();
    }
}
