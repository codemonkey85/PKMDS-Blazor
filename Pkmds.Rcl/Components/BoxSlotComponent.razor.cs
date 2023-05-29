namespace Pkmds.Rcl.Components;

public partial class BoxSlotComponent : IDisposable
{
    [Parameter]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public int BoxSlot { get; set; }

    private string Style => AppState.SelectedBoxSlot == BoxSlot
        ? "border: 4px solid orange; border-radius: 6px;"
        : string.Empty;

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        AppState.OnAppStateChanged -= StateHasChanged;

    private void SetSelectedPokemon()
    {
        if (Pokemon is not { Species: > 0 })
        {
            AppState.SelectedPokemon = null;
            AppState.SelectedBoxSlot = null;
        }
        else
        {
            AppState.SelectedPokemon = Pokemon;
            AppState.SelectedBoxSlot = BoxSlot;
        }
        AppState.Refresh();
    }
}
