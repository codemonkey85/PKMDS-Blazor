namespace Pkmds.Rcl.Components;

public partial class BoxSlotComponent : IDisposable
{
    [Parameter]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public int BoxSlot { get; set; }

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

    private string GetCssClass()
    {
        var cssClassBuilder = new StringBuilder();

        if (Pokemon is not { Species: > 0 })
        {
            cssClassBuilder.Append("empty ");
        };

        if (AppState.SelectedBoxSlot == BoxSlot)
        {
            cssClassBuilder.Append("selected ");
        }

        return cssClassBuilder.ToString().Trim();
    }
}
