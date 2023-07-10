namespace Pkmds.Rcl.Components;

public partial class BoxComponent : IDisposable
{
    [Parameter]
    public int BoxNumber { get; set; }

    private BoxEdit? BoxEdit { get; set; }

    protected override void OnInitialized()
    {
        AppState.OnAppStateChanged += StateHasChanged;
        AppState.OnBoxStateChanged += ReloadBox;
    }

    public void Dispose()
    {
        AppState.OnAppStateChanged -= StateHasChanged;
        AppState.OnBoxStateChanged -= ReloadBox;
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
        AppState.Refresh();
    }
}
