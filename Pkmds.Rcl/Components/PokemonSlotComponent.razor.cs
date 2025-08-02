namespace Pkmds.Rcl.Components;

public partial class PokemonSlotComponent : IDisposable
{
    [Parameter, EditorRequired]
    public int SlotNumber { get; set; }

    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnSlotClick { get; set; }

    [Parameter, EditorRequired]
    public Func<string>? GetClassFunction { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private async Task HandleClick() =>
        await OnSlotClick.InvokeAsync();

    private string GetClass() =>
        GetClassFunction?.Invoke() ?? string.Empty;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private string GetPokemonTitle() => Pokemon is { Species: > 0 }
        ? AppService.GetPokemonSpeciesName(Pokemon.Species) ?? "Unknown"
        : "Unknown";
}
