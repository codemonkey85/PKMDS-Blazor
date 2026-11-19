namespace Pkmds.Rcl.Components.MainTabPages;

public partial class BagTab
{
    private static readonly ComboItem FallbackComboItem = new(string.Empty, 0);

    [Parameter]
    [EditorRequired]
    public PlayerBag? Inventory { get; set; }

    private MudTabs? PouchTabs { get; set; }

    private string[] ItemList { get; set; } = [];

    private Dictionary<int, ComboItem> ItemComboCache { get; set; } = [];

    private List<ComboItem> SortedItemComboList { get; set; } = [];

    private Dictionary<InventoryType, HashSet<string>> PouchValidItemsCache { get; } = [];

    private bool HasFreeSpace { get; set; }

    private bool HasFreeSpaceIndex { get; set; }

    private bool HasFavorite { get; set; }

    private bool HasNew { get; set; }

    private bool IsSortedByName { get; set; } = true; // Set as true so first sort is ascending

    private bool IsSortedByCount { get; set; } = true; // Set as true so first sort is ascending

    private bool IsSortedByIndex { get; set; } = true; // Set as true so first sort is ascending

    private bool ShowEmptySlots { get; set; }

    private PlayerBag? PreviousInventory { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (Inventory is null || AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        if (ReferenceEquals(Inventory, PreviousInventory))
        {
            return;
        }

        PreviousInventory = Inventory;

        ItemList = [.. GameInfo.Strings.GetItemStrings(saveFile.Context, saveFile.Version)];

        for (var i = 0; i < ItemList.Length; i++)
        {
            if (string.IsNullOrEmpty(ItemList[i]))
            {
                ItemList[i] = $"(Item #{i:000})";
            }
        }

        // Append the taught move to TM/HM/TR item names — "TM41" → "TM41 (Softboiled)" etc.
        // Uses DescriptionService's tm-data.json / hm-data.json per game version.
        await EnrichTmItemNamesAsync(saveFile.Version);

        var item0 = Inventory.Pouches[0].Items[0];

        HasFreeSpace = item0 is IItemFreeSpace;
        HasFreeSpaceIndex = item0 is IItemFreeSpaceIndex;
        HasFavorite = item0 is IItemFavorite;
        HasNew = item0 is IItemNewFlag;

        // Build caches for improved performance
        BuildItemComboCache();
        BuildPouchValidItemsCache();
    }

    /// <summary>
    /// Rewrites TM/HM/TR entries in <see cref="ItemList" /> to include the name of the
    /// move they teach for the current game version: "TM41" → "TM41 (Softboiled)" in
    /// Gen 1, "TM001" → "TM001 (Take Down)" in SV, "HM01" → "HM01 (Cut)", etc.
    /// Leaves items whose number can't be resolved for this game untouched.
    /// </summary>
    private async Task EnrichTmItemNamesAsync(GameVersion version)
    {
        for (var i = 0; i < ItemList.Length; i++)
        {
            var name = ItemList[i];
            if (string.IsNullOrEmpty(name) || name[0] == '(')
            {
                continue;
            }

            var prefix = name.Split(' ')[0];
            if (prefix.Length < 3)
            {
                continue;
            }

            string? moveName = null;
            if (prefix.StartsWith("HM", StringComparison.OrdinalIgnoreCase))
            {
                var hmNumber = prefix[2..];
                if (!hmNumber.All(char.IsDigit))
                {
                    continue;
                }

                moveName = await DescriptionService.GetHmMoveNameAsync($"HM{hmNumber}", version);
            }
            else if (prefix.StartsWith("TR", StringComparison.OrdinalIgnoreCase))
            {
                var trNumber = prefix[2..];
                if (!trNumber.All(char.IsDigit))
                {
                    continue;
                }

                moveName = await DescriptionService.GetTmMoveNameAsync($"TR{trNumber}", version);
            }
            else if (prefix.StartsWith("TM", StringComparison.OrdinalIgnoreCase))
            {
                var tmNumber = prefix[2..];
                if (!tmNumber.All(char.IsDigit))
                {
                    continue;
                }

                moveName = await DescriptionService.GetTmMoveNameAsync(tmNumber, version);
                if (moveName is null && tmNumber.Length < 3)
                {
                    // SV uses 3-digit keys ("001"–"099"); retry with zero-padding.
                    moveName = await DescriptionService.GetTmMoveNameAsync(tmNumber.PadLeft(3, '0'), version);
                }
            }

            if (moveName is not null)
            {
                // tm-data.json spellings (Bulbapedia-sourced) sometimes differ from PKHeX's
                // ("Softboiled" vs "Soft-Boiled", "ThunderPunch" vs "Thunder Punch"). Prefer
                // the canonical PKHeX name so the list matches the tooltip.
                ItemList[i] = $"{prefix} ({GameInfoUtilities.GetCanonicalMoveName(moveName)})";
            }
        }
    }

    private void BuildItemComboCache()
    {
        // Build from ItemList to match data source used in BuildPouchValidItemsCache
        // This ensures text consistency between cache and search filters
        var items = ItemList
            .Select((name, index) => new ComboItem(name, index))
            .ToList();

        ItemComboCache = items.ToDictionary(item => item.Value, item => item);
        SortedItemComboList = [.. items.OrderBy(item => item.Text, StringComparer.Ordinal)];
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
            if (ItemList.Length > 0)
            {
                validItems.Add(ItemList[0]);
            }

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
        ItemComboCache.GetValueOrDefault(context.Item.Index)
        ?? ItemComboCache.GetValueOrDefault(0)
        ?? FallbackComboItem;

    // ROM hacks can reference item IDs outside the vanilla item table; guard against out-of-range indices.
    private string GetItemName(int index) =>
        index >= 0 && index < ItemList.Length
            ? ItemList[index]
            : $"(Item #{index:000})";

    private static void SetItem(CellContext<InventoryItem> context, ComboItem? item) =>
        context.Item.Index = item?.Value ?? 0;

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

    private void SortByName(InventoryPouch pouch) =>
        pouch.SortByName(ItemList, IsSortedByName = !IsSortedByName);

    private void SortByCount(InventoryPouch pouch) => pouch.SortByCount(IsSortedByCount = !IsSortedByCount);

    private void SortByIndex(InventoryPouch pouch) => pouch.SortByIndex(IsSortedByIndex = !IsSortedByIndex);

    private IEnumerable<ComboItem> GetPouchItems(InventoryPouch pouch)
    {
        // In HaX mode, show all items regardless of pouch type
        if (AppState.IsHaXEnabled)
        {
            return SortedItemComboList;
        }

        return PouchValidItemsCache.TryGetValue(pouch.Type, out var validItems)
            ? SortedItemComboList.Where(item => validItems.Contains(item.Text))
            : [];
    }
}
