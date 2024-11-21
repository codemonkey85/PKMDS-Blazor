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

    private Task<IEnumerable<ComboItem>> SearchItemNames(string searchString, CancellationToken token) =>
        Task.FromResult(AppService.SearchItemNames(searchString));
}
