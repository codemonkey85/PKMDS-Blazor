namespace Pkmds.Rcl.Components;

public partial class PartyGrid
{
    protected override void OnInitialized()
    {
        AppState.OnAppStateChanged += StateHasChanged;
        AppState.OnPartyStateChanged += StateHasChanged;
    }

    public void Dispose()
    {
        AppState.OnAppStateChanged -= StateHasChanged;
        AppState.OnPartyStateChanged -= StateHasChanged;
    }
}
