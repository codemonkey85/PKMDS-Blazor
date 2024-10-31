namespace Pkmds.Web.Components.MainTabPages;

public partial class PartyAndBoxTab : IDisposable
{
    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;
}
