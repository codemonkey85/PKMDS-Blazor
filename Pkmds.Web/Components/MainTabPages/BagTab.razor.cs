namespace Pkmds.Web.Components.MainTabPages;

public partial class BagTab
{
    [Parameter, EditorRequired]
    public IReadOnlyList<InventoryPouch>? Inventory { get; set; }

    private string[] Itemlist { get; set; } = [];

    private bool HasFreeSpace { get; set; }

    private bool HasFreeSpaceIndex { get; set; }

    private bool HasFavorite { get; set; }

    private bool HasNew { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Inventory is null || AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        Itemlist = [.. GameInfo.Strings.GetItemStrings(saveFile.Context, saveFile.Version)];

        for (var i = 0; i < Itemlist.Length; i++)
        {
            if (string.IsNullOrEmpty(Itemlist[i]))
            {
                Itemlist[i] = $"(Item #{i:000})";
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

    private string[] GetStringsForPouch(ReadOnlySpan<ushort> items, bool sort = true)
    {
        string[] res = new string[items.Length + 1];
        for (int i = 0; i < res.Length - 1; i++)
            res[i] = Itemlist[items[i]];
        res[items.Length] = Itemlist[0];
        if (sort)
            Array.Sort(res);
        return res;
    }


    private Task<IEnumerable<ComboItem>> SearchItemNames(InventoryPouch pouch, string searchString)
    {
        var itemsToSearch = GetStringsForPouch(pouch.GetAllItems());

        return Task.FromResult(Itemlist
            .Select((name, index) => new ComboItem(name, index))
            .Where(x => itemsToSearch.Contains(x.Text) && x.Text.Contains(searchString, StringComparison.OrdinalIgnoreCase)));
    }
}
