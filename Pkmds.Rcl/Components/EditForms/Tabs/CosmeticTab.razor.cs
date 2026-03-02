namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class CosmeticTab : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    private string? OriginMarkSpriteFileName => Pokemon is { Format: >= 6 }
        ? ImageHelper.GetOriginMarkSpriteFileName(OriginMarkUtil.GetOriginMark(Pokemon))
        : null;

    private bool IsFavorite
    {
        get => Pokemon is IFavorite fav && fav.IsFavorite;
        set
        {
            if (Pokemon is IFavorite fav)
            {
                fav.IsFavorite = value;
            }
        }
    }

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
