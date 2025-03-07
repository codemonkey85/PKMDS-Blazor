namespace Pkmds.Rcl.Components;

public partial class MarkingsContainer : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    private string ContainerClass => $"markings-container{(Pokemon is { Generation: 3 } ? " gen-3" : string.Empty)}";

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;
}
