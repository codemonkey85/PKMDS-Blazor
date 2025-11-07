namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class CosmeticTab : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private string GetScaleRating(IScaledSize3 scaledSize3) => Pokemon switch
    {
        PK9 => GetString(PokeSizeDetailedUtil.GetSizeRating(scaledSize3.Scale)),
        _ => GetString(PokeSizeUtil.GetSizeRating(scaledSize3.Scale))
    };

    private static string GetString(PokeSize size) => size is PokeSize.AV
        ? "M"
        : size.ToString();

    private static string GetString(PokeSizeDetailed size) => size is PokeSizeDetailed.AV
        ? "M"
        : size.ToString();
}
