namespace Pkmds.Web.Components.Pages;

public partial class Home
{
    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    private async Task RefreshApp() => await JSRuntime.InvokeVoidAsync("location.reload");
}
