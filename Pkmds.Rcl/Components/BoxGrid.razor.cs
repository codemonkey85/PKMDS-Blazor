namespace Pkmds.Rcl.Components;

public partial class BoxGrid : IDisposable
{
    [Parameter, EditorRequired]
    public int BoxNumber { get; set; }

    private string BoxGridClass =>
        AppState.SaveFile?.BoxSlotCount == 20
            ? "w-80 h-[400px] grid grid-cols-4 grid-rows-5"
            : "grid grid-cols-6 gap-1 w-full aspect-[6/5] mx-auto";

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
