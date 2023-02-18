namespace Pkmds.Rcl.Components;

public partial class PokemonDetailsComponent : IDisposable
{
    protected override void OnInitialized() => AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() => AppState.OnAppStateChanged -= StateHasChanged;

    private void ClearSelection()
    {
        AppState.SelectedPokemon = null;
        AppState.SelectedBoxSlot = null;
        AppState.Refresh();
    }
}
