namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class CosmeticTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private string GetScaleRating(IScaledSize3 scaledSize3) => Pokemon switch
    {
        PK9 => PokeSizeDetailedUtil.GetSizeRating(scaledSize3.Scale).ToString(),
        _ => PokeSizeUtil.GetSizeRating(scaledSize3.Scale).ToString()
    };
}
