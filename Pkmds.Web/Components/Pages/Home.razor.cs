namespace Pkmds.Web.Components.Pages;

public partial class Home
{
    private bool IsUpdateAvailable { get; set; } = false;

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    private async Task RefreshApp() => await JSRuntime.InvokeVoidAsync("location.reload");

    [JSInvokable(nameof(ShowUpdateMessage))]
    public void ShowUpdateMessage()
    {
        // Display the alert when an update is available
        IsUpdateAvailable = true;
        StateHasChanged();
    }

    private async Task ReloadApp() =>
        await JSRuntime.InvokeVoidAsync("location.reload");
}
