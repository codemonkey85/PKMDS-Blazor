namespace Pkmds.Web.Components;

public partial class BoxComponent : IDisposable
{
    [Parameter]
    public int BoxNumber { get; set; }

    private BoxEdit? BoxEdit { get; set; }

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

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
        ReloadBox();
    }

    private void ReloadBox()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        BoxEdit = new BoxEdit(AppState.SaveFile);
        BoxEdit.LoadBox(BoxNumber);
        RefreshService.Refresh();
    }
}

