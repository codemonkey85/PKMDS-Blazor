namespace Pkmds.Web.Components.Pages;

// ReSharper disable once UnusedType.Global
public partial class Home : IDisposable
{
    private bool IsUpdateAvailable { get; set; }

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += StateHasChanged;
        RefreshService.OnUpdateAvailable += ShowUpdateMessage;
    }

    public void Dispose()
    {
        RefreshService.OnAppStateChanged -= StateHasChanged;
        RefreshService.OnUpdateAvailable -= ShowUpdateMessage;
    }

    private void ShowUpdateMessage()
    {
        // Display the alert when an update is available
        IsUpdateAvailable = true;
        StateHasChanged();
    }

    private async Task ReloadApp() =>
        await JSRuntime.InvokeVoidAsync("location.reload");
}
