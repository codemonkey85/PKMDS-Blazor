namespace Pkmds.Rcl.Services;

/// <summary>
/// Service for managing drag-and-drop operations for Pokémon between slots.
/// Tracks the source of a drag operation and provides state for drop targets.
/// </summary>
public interface IDragDropService
{
    /// <summary>
    /// Gets or sets the Pokémon currently being dragged.
    /// </summary>
    PKM? DraggedPokemon { get; set; }

    /// <summary>
    /// Gets or sets the source box number where the drag started.
    /// Null for party slots or Let's Go storage.
    /// </summary>
    int? DragSourceBoxNumber { get; set; }

    /// <summary>
    /// Gets or sets the source slot number where the drag started.
    /// </summary>
    int DragSourceSlotNumber { get; set; }

    /// <summary>
    /// Gets or sets whether the drag source is a party slot.
    /// </summary>
    bool IsDragSourceParty { get; set; }

    /// <summary>
    /// Gets whether a drag operation is currently in progress.
    /// </summary>
    bool IsDragging { get; }

    /// <summary>
    /// Starts a drag operation with the specified Pokémon and source information.
    /// </summary>
    /// <param name="pokemon">The Pokémon being dragged.</param>
    /// <param name="boxNumber">The source box number (null for party or Let's Go storage).</param>
    /// <param name="slotNumber">The source slot number.</param>
    /// <param name="isParty">Whether the source is a party slot.</param>
    void StartDrag(PKM? pokemon, int? boxNumber, int slotNumber, bool isParty);

    /// <summary>
    /// Ends the drag operation, indicating a successful drop.
    /// </summary>
    void EndDrag();

    /// <summary>
    /// Cancels the drag operation without performing a drop.
    /// </summary>
    void ClearDrag();
}
