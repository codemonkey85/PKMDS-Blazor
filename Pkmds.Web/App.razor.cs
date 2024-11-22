namespace Pkmds.Web;

public partial class App()
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await JSRuntime.InvokeVoidAsync("addUpdateListener");
    }

    [JSInvokable(nameof(ShowUpdateMessage))]
    public void ShowUpdateMessage()
    {
        AppState.UpdateAvailable = true;
        RefreshService.Refresh();
    }
}
