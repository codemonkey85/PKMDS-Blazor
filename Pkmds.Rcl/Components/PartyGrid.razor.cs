namespace Pkmds.Rcl.Components;

public partial class PartyGrid
{
    private static int GridHeight => (56 + 4) * 3;

    private static int GridWidth => (68 + 4) * 2;

    private static string GridStyle => $"width: {GridWidth}px; height: {GridHeight}px;";

    protected override void OnInitialized()
    {
        AppState.OnAppStateChanged += StateHasChanged;
        AppState.OnPartyStateChanged += StateHasChanged;
    }

    public void Dispose()
    {
        AppState.OnAppStateChanged -= StateHasChanged;
        AppState.OnPartyStateChanged -= StateHasChanged;
    }
}
