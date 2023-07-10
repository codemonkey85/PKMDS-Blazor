namespace Pkmds.Rcl.Components;

public partial class PartySlotComponent : IDisposable
{
    [Parameter, EditorRequired]
    public int SlotNumber { get; set; }

    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    private string Style => AppState.SelectedPartySlotNumber == SlotNumber
        ? "border: 4px solid orange; border-radius: 6px;"
        : string.Empty;

    protected override void OnInitialized() =>
        AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        AppState.OnAppStateChanged -= StateHasChanged;

    private void SetSelectedPokemon() =>
        AppState.SetSelectedPartyPokemon(Pokemon, SlotNumber);
}
