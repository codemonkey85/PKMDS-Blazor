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

    [JSInvokable]
    public void ShowUpdateMessage() => AppState.UpdateAvailable = true;
}
