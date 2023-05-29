namespace Pkmds.Rcl.Components;

public partial class BoxSlotComponent : IDisposable
{
    [Parameter, EditorRequired]
    public int BoxNumber { get; set; }

    [Parameter, EditorRequired]
    public int SlotNumber { get; set; }

    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    private string Style => AppState.SelectedSlotNumber == SlotNumber
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
            AppState.SelectedBoxNumber = null;
            AppState.SelectedSlotNumber = null;
            AppState.EditFormPokemon = null;
        }
        else
        {
            AppState.SelectedBoxNumber = BoxNumber;
            AppState.SelectedSlotNumber = SlotNumber;
            AppState.EditFormPokemon = Pokemon;
        }
        AppState.Refresh();
    }
}
