namespace Pkmds.Rcl.Components.MainTabPages;

public partial class BagTab
{
    [Parameter]
    [EditorRequired]
    public PlayerBag? Inventory { get; set; }

    private MudTabs? PouchTabs { get; set; }

    private string[] ItemList { get; set; } = [];

    private Dictionary<int, ComboItem> ItemComboCache { get; set; } = [];

    private List<ComboItem> SortedItemComboList { get; set; } = [];

    private Dictionary<InventoryType, HashSet<string>> PouchValidItemsCache { get; set; } = [];

    private static readonly ComboItem FallbackComboItem = new(string.Empty, 0);

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

        var item0 = Inventory.Pouches[0].Items[0];

        HasFreeSpace = item0 is IItemFreeSpace;
        HasFreeSpaceIndex = item0 is IItemFreeSpaceIndex;
        HasFavorite = item0 is IItemFavorite;
        HasNew = item0 is IItemNewFlag;

        // Build caches for improved performance
        BuildItemComboCache();
        BuildPouchValidItemsCache();
    }

    private void BuildItemComboCache()
    {
        var items = GameInfo.FilteredSources.Items
            .DistinctBy(item => item.Value)
            .ToList();

        ItemComboCache = items.ToDictionary(item => item.Value, item => item);
        SortedItemComboList = [.. items.OrderBy(item => item.Text)];
    }

    private void BuildPouchValidItemsCache()
    {
        if (Inventory is null)
        {
            return;
        }

        PouchValidItemsCache.Clear();
        foreach (var pouch in Inventory.Pouches)
        {
            var validItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pouchItems = pouch.GetAllItems();
            foreach (var itemIndex in pouchItems)
            {
                if (itemIndex < ItemList.Length)
                {
                    validItems.Add(ItemList[itemIndex]);
                }
            }
            // Always include "None" item
            validItems.Add(ItemList[0]);
            PouchValidItemsCache[pouch.Type] = validItems;
        }
    }

    private void SaveChanges()
    {
        if (AppState?.SaveFile is not { } saveFile || Inventory is null)
        {
            return;
        }

        foreach (var pouch in Inventory.Pouches)
        {
            pouch.ClearCount0();
        }

        Inventory.CopyTo(saveFile); // Persist pouch edits back to the save data
    }

    private ComboItem GetItem(CellContext<InventoryItem> context) =>
        ItemComboCache.GetValueOrDefault(context.Item.Index) ?? FallbackComboItem;

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
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return Task.FromResult(Enumerable.Empty<ComboItem>());
        }

        // Use cached valid items for this pouch
        if (!PouchValidItemsCache.TryGetValue(pouch.Type, out var validItems))
        {
            return Task.FromResult(Enumerable.Empty<ComboItem>());
        }

        // Use pre-sorted list to avoid sorting on every search
        var results = SortedItemComboList
            .Where(item => validItems.Contains(item.Text) &&
                          item.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(results);
    }
}
