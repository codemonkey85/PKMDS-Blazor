namespace Pkmds.Rcl.Components.MainTabPages;

public partial class PartyAndBoxTab : IDisposable
{
    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;
    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;
}
