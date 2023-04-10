namespace Pkmds.Rcl.Components;

public partial class BoxSlotComponent : IDisposable
{
    [Parameter]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public int BoxSlot { get; set; }

    private string GetStyle()
    {
        var styleBuilder = new StringBuilder();
        styleBuilder.Append(AppState.SelectedBoxSlot == BoxSlot
            ? "border: 4px solid orange; border-radius: 6px;"
            : string.Empty);
        return styleBuilder.ToString();
    }

    protected override void OnInitialized() => AppState.OnAppStateChanged += StateHasChanged;

    public void Dispose() => AppState.OnAppStateChanged -= StateHasChanged;

    private void SetPreviewPokemon()
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
