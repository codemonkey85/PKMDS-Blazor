namespace Pkmds.Rcl.Components;

public partial class BoxGrid : IDisposable
{
    [Parameter, EditorRequired]
    public int BoxNumber { get; set; }

    private string BoxGridClass =>
        AppState.SaveFile?.BoxSlotCount == 20
            ? "box-grid-20"
            : "box-grid-30";

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private void SetSelectedPokemon(PKM? pokemon, int boxNumber, int slotNumber) =>
        AppService.SetSelectedBoxPokemon(pokemon, boxNumber, slotNumber);

    private string GetClass(int slotNumber) => AppState.SelectedBoxSlotNumber == slotNumber
        ? Constants.SelectedSlotClass
        : string.Empty;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;
}
