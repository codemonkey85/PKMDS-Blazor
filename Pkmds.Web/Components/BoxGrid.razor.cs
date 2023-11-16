namespace Pkmds.Web.Components;

public partial class BoxGrid : IDisposable
{
    [Parameter, EditorRequired]
    public BoxEdit? BoxEdit { get; set; }

    [Parameter, EditorRequired]
    public int BoxNumber { get; set; }

    private string BoxGridClass =>
        AppState.SaveFile?.BoxSlotCount == 20 ? "box-grid-20" : "box-grid-30";

    private void SetSelectedPokemon(PKM? pokemon, int boxNumber, int slotNumber) =>
        AppService.SetSelectedBoxPokemon(pokemon, boxNumber, slotNumber);

    private string GetStyle(int slotNumber) => AppState.SelectedBoxSlotNumber == slotNumber
        ? Constants.SelectedSlotStyle
        : string.Empty;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;
}
