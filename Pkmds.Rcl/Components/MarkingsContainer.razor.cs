namespace Pkmds.Rcl.Components;

public partial class MarkingsContainer : IDisposable
{
    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    private string ContainerClass => $"grid grid-rows-2 pt-[3px] justify-items-center items-center{(Pokemon is { Generation: 3 } ? " grid-cols-4 gap-0" : " grid-cols-3 gap-[25px]")}";

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;
}
