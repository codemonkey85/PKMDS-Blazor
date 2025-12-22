namespace Pkmds.Rcl.Services;

public interface IDragDropService
{
    PKM? DraggedPokemon { get; set; }
    
    int? DragSourceBoxNumber { get; set; }
    
    int? DragSourceSlotNumber { get; set; }
    
    bool IsDragSourceParty { get; set; }
    
    bool IsDragging { get; }
    
    void StartDrag(PKM? pokemon, int? boxNumber, int slotNumber, bool isParty);
    
    void EndDrag();
    
    void ClearDrag();
}
