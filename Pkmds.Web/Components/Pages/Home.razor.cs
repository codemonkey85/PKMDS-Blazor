namespace Pkmds.Web.Components.Pages;

public partial class Home
{
    private bool IsUpdateAvailable { get; set; } = false;

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += StateHasChanged;
        RefreshService.OnUpdateAvailable += ShowUpdateMessage;
    }

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    public void ShowUpdateMessage()
    {
        // Display the alert when an update is available
        IsUpdateAvailable = true;
        StateHasChanged();
    }

    private async Task ReloadApp() =>
        await JSRuntime.InvokeVoidAsync("location.reload");
}
