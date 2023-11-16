namespace Pkmds.Web.Components;

public partial class MarkingsContainer
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    private string ContainerClass => $"markings-container{(Pokemon is { Generation: 3 } ? " gen-3" : string.Empty)}";

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;
}
