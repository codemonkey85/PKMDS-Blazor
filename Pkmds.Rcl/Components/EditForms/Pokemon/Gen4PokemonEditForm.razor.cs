namespace Pkmds.Rcl.Components.EditForms.Pokemon;

public partial class Gen4PokemonEditForm : IDisposable
{
    protected override void OnInitialized() => AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() => AppState.OnAppStateChanged -= StateHasChanged;
}