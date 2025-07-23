namespace Pkmds.Rcl.Components;

public partial class BoxComponent : IDisposable
{
    [Parameter]
    public int BoxNumber { get; set; }

    public void Dispose()
    {
        RefreshService.OnAppStateChanged -= StateHasChanged;
        RefreshService.OnBoxStateChanged -= ReloadBox;
    }

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += StateHasChanged;
        RefreshService.OnBoxStateChanged += ReloadBox;
    }

    protected override void OnParametersSet()
    {
        ReloadBox();
    }

    private void ReloadBox()
    {
        if (AppState.SaveFile is null || AppState.BoxEdit is null)
        {
            return;
        }

        AppState.BoxEdit.LoadBox(AppState.SaveFile.CurrentBox);

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.Refresh();
    }
}
