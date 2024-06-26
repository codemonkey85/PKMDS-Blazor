namespace Pkmds.Web.Client.Components.Pages;

public partial class Home : IDisposable
{
    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;
}
