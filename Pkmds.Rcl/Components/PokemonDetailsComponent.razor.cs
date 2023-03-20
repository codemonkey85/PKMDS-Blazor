namespace Pkmds.Rcl.Components;

public partial class PokemonDetailsComponent : IDisposable
{
    private static readonly string[] genderForms = { "", "F", "" };

    protected override void OnInitialized() => AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() => AppState.OnAppStateChanged -= StateHasChanged;
}
