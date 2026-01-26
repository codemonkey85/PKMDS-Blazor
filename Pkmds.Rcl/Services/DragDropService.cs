namespace Pkmds.Rcl.Services;

/// <summary>
/// Implementation of drag-and-drop service for managing Pokémon drag operations.
/// Maintains state about the current drag operation including source location and Pokémon data.
/// </summary>
public class DragDropService : IDragDropService
{
    /// <inheritdoc />
    public PKM? DraggedPokemon { get; set; }

    /// <inheritdoc />
    public int? DragSourceBoxNumber { get; set; }

    /// <inheritdoc />
    public int DragSourceSlotNumber { get; set; } = -1;

    /// <inheritdoc />
    public bool IsDragSourceParty { get; set; }

    /// <inheritdoc />
    public bool IsDragging => DraggedPokemon is not null;

    /// <inheritdoc />
    public void StartDrag(PKM? pokemon, int? boxNumber, int slotNumber, bool isParty)
    {
        DraggedPokemon = pokemon;
        DragSourceBoxNumber = boxNumber;
        DragSourceSlotNumber = slotNumber;
        IsDragSourceParty = isParty;
    }

    /// <inheritdoc />
    public void EndDrag()
    {
        // Keep the drag data until the drop operation completes
        // The data is needed by the drop handler to perform the move
    }

    /// <inheritdoc />
    public void ClearDrag()
    {
        DraggedPokemon = null;
        DragSourceBoxNumber = null;
        DragSourceSlotNumber = -1;
        IsDragSourceParty = false;
    }
}
