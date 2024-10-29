namespace Pkmds.Web.Components;

public partial class LetsGoBoxComponent
{
    public BoxEdit? BoxEdit { get; set; }

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += StateHasChanged;
        RefreshService.OnBoxStateChanged += ReloadBox;
    }

    public void Dispose()
    {
        RefreshService.OnAppStateChanged -= StateHasChanged;
        RefreshService.OnBoxStateChanged -= ReloadBox;
    }

    protected override void OnParametersSet()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        ReloadBox();
    }

    private void ReloadBox()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        BoxEdit = new BoxEdit(AppState.SaveFile);
        RefreshService.Refresh();
    }
}
