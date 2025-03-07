namespace Pkmds.Rcl.Components;

public partial class PokemonStorageComponent : IDisposable
{
    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;
    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    private void GoToNextBox()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        if (AppState.SaveFile.CurrentBox == AppState.SaveFile.BoxCount - 1)
        {
            AppState.SaveFile.CurrentBox = 0;
        }
        else
        {
            AppState.SaveFile.CurrentBox++;
        }

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
    }

    private void GoToPreviousBox()
    {
        if (AppState.SaveFile is null)
        {
            return;
        }

        if (AppState.SaveFile.CurrentBox == 0)
        {
            AppState.SaveFile.CurrentBox = AppState.SaveFile.BoxCount - 1;
        }
        else
        {
            AppState.SaveFile.CurrentBox--;
        }

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;
    }
}
