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

    [Parameter]
    public int? BoxNumber { get; set; }

    [Parameter]
    public bool IsPartySlot { get; set; }

    [Inject]
    private IDragDropService DragDropService { get; set; } = default!;

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

    private void HandleDragStart(DragEventArgs e)
    {
        if (Pokemon is not { Species: > 0 })
        {
            return;
        }

        DragDropService.StartDrag(Pokemon, BoxNumber, SlotNumber, IsPartySlot);
        e.DataTransfer.EffectAllowed = "move";
    }

    private void HandleDragEnd(DragEventArgs e)
    {
        DragDropService.EndDrag();
        StateHasChanged();
    }

    private void HandleDragOver(DragEventArgs e)
    {
        if (!DragDropService.IsDragging)
        {
            return;
        }

        // Prevent dropping onto the same slot
        if (DragDropService.IsDragSourceParty == IsPartySlot &&
            DragDropService.DragSourceBoxNumber == BoxNumber &&
            DragDropService.DragSourceSlotNumber == SlotNumber)
        {
            e.DataTransfer.DropEffect = "none";
            return;
        }

        e.DataTransfer.DropEffect = "move";
    }

    private void HandleDrop(DragEventArgs e)
    {
        if (!DragDropService.IsDragging)
        {
            return;
        }

        // Don't drop onto the same slot
        if (DragDropService.IsDragSourceParty == IsPartySlot &&
            DragDropService.DragSourceBoxNumber == BoxNumber &&
            DragDropService.DragSourceSlotNumber == SlotNumber)
        {
            DragDropService.ClearDrag();
            StateHasChanged();
            return;
        }

        // Move the Pokémon
        AppService.MovePokemon(
            DragDropService.DragSourceBoxNumber,
            DragDropService.DragSourceSlotNumber!.Value,
            DragDropService.IsDragSourceParty,
            BoxNumber,
            SlotNumber,
            IsPartySlot
        );

        DragDropService.ClearDrag();
        StateHasChanged();
    }

    private string GetDragClass()
    {
        if (!DragDropService.IsDragging)
        {
            return string.Empty;
        }

        // Check if this is the source slot
        if (DragDropService.IsDragSourceParty == IsPartySlot &&
            DragDropService.DragSourceBoxNumber == BoxNumber &&
            DragDropService.DragSourceSlotNumber == SlotNumber)
        {
            return "slot-dragging";
        }

        // This is a potential drop target
        return "slot-drop-target";
    }
}
