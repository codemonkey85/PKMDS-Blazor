namespace Pkmds.Rcl.Components.MainTabPages;

public partial class BagTab
{
    [Parameter, EditorRequired]
    public IReadOnlyList<InventoryPouch>? Inventory { get; set; }

    private MudTabs? PouchTabs { get; set; }

    private string[] ItemList { get; set; } = [];

    private bool HasFreeSpace { get; set; }

    private bool HasFreeSpaceIndex { get; set; }

    private bool HasFavorite { get; set; }

    private bool HasNew { get; set; }

    private bool IsSortedByName { get; set; } = true; // Set as true so first sort is ascending

    private bool IsSortedByCount { get; set; } = true; // Set as true so first sort is ascending

    private bool IsSortedByIndex { get; set; } = true; // Set as true so first sort is ascending

    private bool ShouldVirtualize { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Inventory is null || AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        ItemList = [.. GameInfo.Strings.GetItemStrings(saveFile.Context, saveFile.Version)];

        for (var i = 0; i < ItemList.Length; i++)
        {
            if (string.IsNullOrEmpty(ItemList[i]))
            {
                ItemList[i] = $"(Item #{i:000})";
            }
        }

        var item0 = Inventory[0].Items[0];

        HasFreeSpace = item0 is IItemFreeSpace;
        HasFreeSpaceIndex = item0 is IItemFreeSpaceIndex;
        HasFavorite = item0 is IItemFavorite;
        HasNew = item0 is IItemNewFlag;
    }

    private void SaveChanges()
    {
        if (AppState?.SaveFile is null || Inventory is null)
        {
            return;
        }

        foreach (var pouch in Inventory)
        {
            pouch.ClearCount0();
        }

        AppState.SaveFile.Inventory = Inventory;
    }

    private ComboItem GetItem(CellContext<InventoryItem> context) =>
        AppService.GetItemComboItem(context.Item.Index);

    private static void SetItem(CellContext<InventoryItem> context, ComboItem item) =>
        context.Item.Index = item.Value;

    private void DeleteItem(CellContext<InventoryItem> context, InventoryPouch pouch)
    {
        if (Inventory is null)
        {
            return;
        }

        context.Item.Clear();
        pouch.ClearCount0();
    }

    private static string GetPouchName(InventoryPouch pouch) => pouch.Type switch
    {
        InventoryType.TMHMs => "TMs/HMs",
        InventoryType.KeyItems => "Key Items",
        InventoryType.BattleItems => "Battle Items",
        InventoryType.MailItems => "Mail Items",
        InventoryType.PCItems => "PC Items",
        InventoryType.FreeSpace => "Free Space",
        InventoryType.ZCrystals => "Z-Crystals",
        InventoryType.MegaStones => "Mega Stones",
        _ => pouch.Type.ToString()
    };

    private string[] GetStringsForPouch(ReadOnlySpan<ushort> items, bool sort = true)
    {
        var res = new string[items.Length + 1];
        for (var i = 0; i < res.Length - 1; i++)
        {
            res[i] = ItemList[items[i]];
        }

        res[items.Length] = ItemList[0];
        if (sort)
        {
            Array.Sort(res);
        }

        return res;
    }

    private void SortByName(InventoryPouch pouch) =>
        pouch.SortByName(ItemList, IsSortedByName = !IsSortedByName);

    private void SortByCount(InventoryPouch pouch) => pouch.SortByCount(IsSortedByCount = !IsSortedByCount);

    private void SortByIndex(InventoryPouch pouch) => pouch.SortByIndex(IsSortedByIndex = !IsSortedByIndex);

    private Task<IEnumerable<ComboItem>> SearchItemNames(InventoryPouch pouch, string searchString)
    {
        var itemsToSearch = GetStringsForPouch(pouch.GetAllItems());

        return Task.FromResult(ItemList
            .Select((name, index) => new ComboItem(name, index))
            .Where(x => itemsToSearch.Contains(x.Text) &&
                        x.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase)));
    }
}
