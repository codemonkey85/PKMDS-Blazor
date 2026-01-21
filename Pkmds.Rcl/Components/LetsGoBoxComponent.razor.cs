namespace Pkmds.Rcl.Components;

public partial class LetsGoBoxComponent : RefreshAwareComponent
{
    protected override RefreshEvents SubscribeTo => RefreshEvents.AppState | RefreshEvents.BoxState;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RefreshService.OnBoxStateChanged += ReloadBox;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RefreshService.OnBoxStateChanged -= ReloadBox;
        }
    }

    protected override void OnParametersSet() => ReloadBox();

    private void ReloadBox()
    {
        if (AppState.SaveFile is null || AppState.BoxEdit is null)
        {
            return;
        }

        AppState.BoxEdit.LoadBox(AppState.BoxEdit.CurrentBox);

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        StateHasChanged();
    }
}
