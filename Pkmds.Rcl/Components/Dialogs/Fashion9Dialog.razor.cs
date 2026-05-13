namespace Pkmds.Rcl.Components.Dialogs;

public partial class Fashion9Dialog
{
    [Parameter]
    [EditorRequired]
    public SaveFile? SaveFile { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    private List<(string Label, SCBlock Block, List<FashionItemModel> Items)> _svSections = [];
    private List<(string Label, SCBlock Block, List<FashionItem9aModel> Items)> _zaClothingSections = [];
    private List<(string Label, SCBlock Block, List<FashionItemModel> Items)> _zaHairSections = [];

    private static readonly (uint Key, string Label)[] SvSectionDefs =
    [
        (SaveBlockAccessor9SV.KFashionUnlockedEyewear, "Eyewear"),
        (SaveBlockAccessor9SV.KFashionUnlockedGloves, "Gloves"),
        (SaveBlockAccessor9SV.KFashionUnlockedBag, "Bag"),
        (SaveBlockAccessor9SV.KFashionUnlockedFootwear, "Footwear"),
        (SaveBlockAccessor9SV.KFashionUnlockedHeadwear, "Headwear"),
        (SaveBlockAccessor9SV.KFashionUnlockedLegwear, "Legwear"),
        (SaveBlockAccessor9SV.KFashionUnlockedClothing, "Clothing"),
        (SaveBlockAccessor9SV.KFashionUnlockedPhoneCase, "Phone Case"),
    ];

    private static readonly (uint Key, string Label)[] ZaClothingSectionDefs =
    [
        (SaveBlockAccessor9ZA.KFashionTops, "Tops"),
        (SaveBlockAccessor9ZA.KFashionBottoms, "Bottoms"),
        (SaveBlockAccessor9ZA.KFashionAllInOne, "All-in-One"),
        (SaveBlockAccessor9ZA.KFashionHeadwear, "Headwear"),
        (SaveBlockAccessor9ZA.KFashionEyewear, "Eyewear"),
        (SaveBlockAccessor9ZA.KFashionGloves, "Gloves"),
        (SaveBlockAccessor9ZA.KFashionLegwear, "Legwear"),
        (SaveBlockAccessor9ZA.KFashionFootwear, "Footwear"),
        (SaveBlockAccessor9ZA.KFashionSatchels, "Satchels"),
        (SaveBlockAccessor9ZA.KFashionEarrings, "Earrings"),
    ];

    private static readonly (uint Key, string Label)[] ZaHairSectionDefs =
    [
        (SaveBlockAccessor9ZA.KHairMake00StyleHair, "Hair Style"),
        (SaveBlockAccessor9ZA.KHairMake01StyleBangs, "Bangs"),
        (SaveBlockAccessor9ZA.KHairMake02ColorHair, "Hair Color (Base)"),
        (SaveBlockAccessor9ZA.KHairMake03ColorHair, "Hair Color (Blocking)"),
        (SaveBlockAccessor9ZA.KHairMake04ColorHair, "Hair Color (Balayage)"),
        (SaveBlockAccessor9ZA.KHairMake05StyleEyebrow, "Eyebrow Style"),
        (SaveBlockAccessor9ZA.KHairMake06ColorEyebrow, "Eyebrow Color"),
        (SaveBlockAccessor9ZA.KHairMake07StyleEyes, "Eye Style"),
        (SaveBlockAccessor9ZA.KHairMake08ColorEyes, "Eye Color"),
        (SaveBlockAccessor9ZA.KHairMake09StyleEyelash, "Eyelash Style"),
        (SaveBlockAccessor9ZA.KHairMake10ColorEyelash, "Eyelash Color"),
        (SaveBlockAccessor9ZA.KHairMake11Lips, "Lips"),
        (SaveBlockAccessor9ZA.KHairMake12BeautyMark, "Beauty Mark"),
        (SaveBlockAccessor9ZA.KHairMake13Freckles, "Freckles"),
        (SaveBlockAccessor9ZA.KHairMake14DarkCircles, "Dark Circles"),
    ];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadData();
    }

    private void LoadData()
    {
        _svSections.Clear();
        _zaClothingSections.Clear();
        _zaHairSections.Clear();

        switch (SaveFile)
        {
            case SAV9SV sv:
                LoadSvData(sv);
                break;
            case SAV9ZA za:
                LoadZaData(za);
                break;
        }
    }

    private void LoadSvData(SAV9SV sv)
    {
        foreach (var (key, label) in SvSectionDefs)
        {
            var block = sv.Blocks.GetBlock(key);
            var raw = FashionItem9.GetArray(block.Data);
            var items = raw
                .Select((item, i) => new FashionItemModel { Index = i, Value = item.Value, IsNew = item.IsNew })
                .Where(m => m.Value != FashionItem9.None)
                .ToList();
            _svSections.Add((label, block, items));
        }
    }

    private void LoadZaData(SAV9ZA za)
    {
        foreach (var (key, label) in ZaClothingSectionDefs)
        {
            var block = za.Blocks.GetBlock(key);
            var raw = FashionItem9a.GetArray(block.Data);
            var items = raw
                .Select((item, i) => new FashionItem9aModel
                {
                    Index = i,
                    Value = item.Value,
                    IsNew = item.IsNew,
                    IsNewShop = item.IsNewShop,
                    IsNewGroup = item.IsNewGroup,
                    IsEquipped = item.IsEquipped,
                    IsOwned = item.IsOwned,
                })
                .Where(m => m.Value != FashionItem9a.None)
                .ToList();
            _zaClothingSections.Add((label, block, items));
        }

        foreach (var (key, label) in ZaHairSectionDefs)
        {
            var block = za.Blocks.GetBlock(key);
            var raw = HairMakeItem9a.GetArray(block.Data);
            var items = raw
                .Select((item, i) => new FashionItemModel { Index = i, Value = item.Value, IsNew = item.IsNew })
                .Where(m => m.Value != HairMakeItem9a.None)
                .ToList();
            _zaHairSections.Add((label, block, items));
        }
    }

    private static void SetAllOwned(List<FashionItem9aModel> items)
    {
        foreach (var item in items)
            item.IsOwned = true;
    }

    private void SetAllOwnedAllTabs()
    {
        foreach (var (_, _, items) in _zaClothingSections)
            SetAllOwned(items);
        StateHasChanged();
    }

    private void Save()
    {
        switch (SaveFile)
        {
            case SAV9SV:
                SaveSvData();
                break;
            case SAV9ZA:
                SaveZaData();
                break;
        }

        RefreshService.Refresh();
        Haptics.Confirm();
        MudDialog?.Close(DialogResult.Ok(true));
    }

    private void SaveSvData()
    {
        foreach (var (_, block, items) in _svSections)
        {
            var raw = FashionItem9.GetArray(block.Data);
            foreach (var m in items)
            {
                raw[m.Index].Value = m.Value;
                raw[m.Index].IsNew = m.IsNew;
            }
            FashionItem9.SetArray(raw, block.Data);
        }
    }

    private void SaveZaData()
    {
        foreach (var (_, block, items) in _zaClothingSections)
        {
            var raw = FashionItem9a.GetArray(block.Data);
            foreach (var m in items)
            {
                raw[m.Index].Value = m.Value;
                raw[m.Index].IsNew = m.IsNew;
                raw[m.Index].IsNewShop = m.IsNewShop;
                raw[m.Index].IsNewGroup = m.IsNewGroup;
                raw[m.Index].IsEquipped = m.IsEquipped;
                raw[m.Index].IsOwned = m.IsOwned;
            }
            FashionItem9a.SetArray(raw, block.Data);
        }

        foreach (var (_, block, items) in _zaHairSections)
        {
            var raw = HairMakeItem9a.GetArray(block.Data);
            foreach (var m in items)
            {
                raw[m.Index].Value = m.Value;
                raw[m.Index].IsNew = m.IsNew;
            }
            HairMakeItem9a.SetArray(raw, block.Data);
        }
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());

    public class FashionItemModel
    {
        public int Index { get; set; }
        public uint Value { get; set; }
        public bool IsNew { get; set; }
    }

    public sealed class FashionItem9aModel : FashionItemModel
    {
        public bool IsNewShop { get; set; }
        public bool IsNewGroup { get; set; }
        public bool IsEquipped { get; set; }
        public bool IsOwned { get; set; }
    }
}
