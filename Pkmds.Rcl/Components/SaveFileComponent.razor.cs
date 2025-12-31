namespace Pkmds.Rcl.Components;

public partial class SaveFileComponent : IDisposable
{
    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;
}
