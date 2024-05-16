namespace Pkmds.Web.Components;

public partial class PokemonSlotComponent
{
    [Parameter, EditorRequired]
    public int SlotNumber { get; set; }

    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnSlotClick { get; set; }

    [Parameter, EditorRequired]
    public Func<string>? GetStyleFunction { get; set; }

    private async Task HandleClick() =>
        await OnSlotClick.InvokeAsync();

    private string GetStyle() =>
        GetStyleFunction?.Invoke() ?? string.Empty;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;
}
