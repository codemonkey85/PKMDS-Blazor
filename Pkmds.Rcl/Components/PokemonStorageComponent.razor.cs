namespace Pkmds.Rcl.Components;

public partial class PokemonStorageComponent : IDisposable
{
    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    private void GoToNextBox()
    {
        if (AppState.SaveFile is null || AppState.BoxEdit is null)
        {
            return;
        }

        AppState.SaveFile.CurrentBox = AppState.BoxEdit.MoveRight();

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.RefreshBoxState();
    }

    private void GoToPreviousBox()
    {
        if (AppState.SaveFile is null || AppState.BoxEdit is null)
        {
            return;
        }

        AppState.SaveFile.CurrentBox = AppState.BoxEdit.MoveLeft();

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.RefreshBoxState();
    }

    private void OnBoxChanged(int newBox) 
    {
        if (AppState.SaveFile is null || AppState.BoxEdit is null)
        {
            return;
        }

        AppState.BoxEdit.LoadBox(newBox);
        AppState.SaveFile.CurrentBox = newBox;

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.RefreshBoxState();
    }
}
