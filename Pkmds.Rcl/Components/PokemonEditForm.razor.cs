namespace Pkmds.Rcl.Components;

public partial class PokemonEditForm : IDisposable
{
    protected override void OnInitialized() => AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() => AppState.OnAppStateChanged -= StateHasChanged;
}
