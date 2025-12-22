namespace Pkmds.Rcl.Services;

public class DragDropService : IDragDropService
{
    public PKM? DraggedPokemon { get; set; }
    
    public int? DragSourceBoxNumber { get; set; }
    
    public int? DragSourceSlotNumber { get; set; }
    
    public bool IsDragSourceParty { get; set; }
    
    public bool IsDragging => DraggedPokemon is not null;
    
    public void StartDrag(PKM? pokemon, int? boxNumber, int slotNumber, bool isParty)
    {
        DraggedPokemon = pokemon;
        DragSourceBoxNumber = boxNumber;
        DragSourceSlotNumber = slotNumber;
        IsDragSourceParty = isParty;
    }
    
    public void EndDrag()
    {
        // Keep the drag data until the drop operation completes
    }
    
    public void ClearDrag()
    {
        DraggedPokemon = null;
        DragSourceBoxNumber = null;
        DragSourceSlotNumber = null;
        IsDragSourceParty = false;
    }
}
