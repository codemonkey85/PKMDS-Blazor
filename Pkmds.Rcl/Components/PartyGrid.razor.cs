namespace Pkmds.Rcl.Components;

public partial class PartyGrid
{
    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += StateHasChanged;
        RefreshService.OnPartyStateChanged += StateHasChanged;
    }

    public void Dispose()
    {
        RefreshService.OnAppStateChanged -= StateHasChanged;
        RefreshService.OnPartyStateChanged -= StateHasChanged;
    }
}
