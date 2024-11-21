namespace Pkmds.Web.Components.MainTabPages;

public partial class BagTab
{
    [Parameter, EditorRequired]
    public IReadOnlyList<InventoryPouch>? Inventory { get; set; }

    private void SaveChanges()
    {
        if (AppState?.SaveFile is null || Inventory is null)
        {
            return;
        }

        AppState.SaveFile.Inventory = Inventory;
    }

    private ComboItem GetItem(CellContext<InventoryItem> context) =>
        AppService.GetItemComboItem(context.Item.Index);

    private static void SetItem(CellContext<InventoryItem> context, ComboItem item) =>
        context.Item.Index = item.Value;

    private void DeleteItem(CellContext<InventoryItem> context)
    {
        if (Inventory is null)
        {
            return;
        }

        context.Item.Clear();
    }

    private static string GetPouchName(InventoryPouch pouch) => pouch.Type switch
    {
        InventoryType.None => "None",
        InventoryType.Items => "Items",
        InventoryType.KeyItems => "Key Items",
        InventoryType.TMHMs => "TMs/HMs",
        InventoryType.Medicine => "Medicine",
        InventoryType.Berries => "Berries",
        InventoryType.Balls => "Balls",
        InventoryType.BattleItems => "Battle Items",
        InventoryType.MailItems => "Mail Items",
        InventoryType.PCItems => "PC Items",
        InventoryType.FreeSpace => "Free Space",
        InventoryType.ZCrystals => "Z-Crystals",
        InventoryType.Candy => "Candy",
        InventoryType.Treasure => "Treasure",
        InventoryType.Ingredients => "Ingredients",
        _ => pouch.Type.ToString()
    };

    private Task<IEnumerable<ComboItem>> SearchItemNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchItemNames(searchString));
}
