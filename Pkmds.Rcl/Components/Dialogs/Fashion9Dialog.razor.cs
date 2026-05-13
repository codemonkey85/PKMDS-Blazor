namespace Pkmds.Rcl.Components.Dialogs;

public partial class Fashion9Dialog
{
    [Parameter]
    [EditorRequired]
    public SaveFile? SaveFile { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    private List<FashionSection<FashionItemModel>> _svSections = [];
    private List<FashionSection<FashionItem9aModel>> _zaClothingSections = [];
    private List<FashionSection<FashionItemModel>> _zaHairSections = [];

    // Tracks the active MudTabPanel index across SV / ZA-clothing / ZA-hair so we can
    // lazy-load only the section currently visible. Eagerly loading all 25 ZA tabs at
    // dialog open blocks the WASM main thread long enough for Chrome to offer to kill
    // the page.
    private int _activePanelIndex;

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
        BuildDescriptors();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await EnsureActiveSectionLoadedAsync();
    }

    // Builds the per-tab descriptors with `Items = null`; actual data parsing happens
    // lazily when the user activates each tab.
    private void BuildDescriptors()
    {
        _svSections.Clear();
        _zaClothingSections.Clear();
        _zaHairSections.Clear();
        _activePanelIndex = 0;

        switch (SaveFile)
        {
            case SAV9SV sv:
                foreach (var (key, label) in SvSectionDefs)
                    _svSections.Add(new FashionSection<FashionItemModel>(label, sv.Blocks.GetBlock(key)));
                break;
            case SAV9ZA za:
                foreach (var (key, label) in ZaClothingSectionDefs)
                    _zaClothingSections.Add(new FashionSection<FashionItem9aModel>(label, za.Blocks.GetBlock(key)));
                foreach (var (key, label) in ZaHairSectionDefs)
                    _zaHairSections.Add(new FashionSection<FashionItemModel>(label, za.Blocks.GetBlock(key)));
                break;
        }
    }

    private async Task OnActivePanelChanged(int newIndex)
    {
        _activePanelIndex = newIndex;
        await EnsureActiveSectionLoadedAsync();
    }

    // Parses the active section if not already loaded. Task.Delay(1) (not Task.Yield —
    // which doesn't release the JS macrotask in Blazor WASM, see batch-legalize lessons)
    // lets the spinner paint before the synchronous parse runs, then re-renders.
    private async Task EnsureActiveSectionLoadedAsync()
    {
        if (SaveFile is SAV9SV)
        {
            if (_activePanelIndex < _svSections.Count && _svSections[_activePanelIndex].Items is null)
            {
                await Task.Delay(1);
                _svSections[_activePanelIndex].Items = ParseSv(_svSections[_activePanelIndex].Block);
                StateHasChanged();
            }
        }
        else if (SaveFile is SAV9ZA)
        {
            var clothingCount = _zaClothingSections.Count;
            if (_activePanelIndex < clothingCount)
            {
                var section = _zaClothingSections[_activePanelIndex];
                if (section.Items is null)
                {
                    await Task.Delay(1);
                    section.Items = ParseZaClothing(section.Block);
                    StateHasChanged();
                }
            }
            else
            {
                var hairIndex = _activePanelIndex - clothingCount;
                if (hairIndex >= 0 && hairIndex < _zaHairSections.Count)
                {
                    var section = _zaHairSections[hairIndex];
                    if (section.Items is null)
                    {
                        await Task.Delay(1);
                        section.Items = ParseZaHair(section.Block);
                        StateHasChanged();
                    }
                }
            }
        }
    }

    // Show every slot (including ones still set to the None sentinel) so the user can
    // edit any row — matches PKHeX WinForms' SAV_Fashion9 behavior.
    private static List<FashionItemModel> ParseSv(SCBlock block) =>
        [.. FashionItem9.GetArray(block.Data)
            .Select((item, i) => new FashionItemModel { Index = i, Value = item.Value, IsNew = item.IsNew })];

    private static List<FashionItem9aModel> ParseZaClothing(SCBlock block) =>
        [.. FashionItem9a.GetArray(block.Data)
            .Select((item, i) => new FashionItem9aModel
            {
                Index = i,
                Value = item.Value,
                IsNew = item.IsNew,
                IsNewShop = item.IsNewShop,
                IsNewGroup = item.IsNewGroup,
                IsEquipped = item.IsEquipped,
                IsOwned = item.IsOwned,
            })];

    private static List<FashionItemModel> ParseZaHair(SCBlock block) =>
        [.. HairMakeItem9a.GetArray(block.Data)
            .Select((item, i) => new FashionItemModel { Index = i, Value = item.Value, IsNew = item.IsNew })];

    private static void SetAllOwned(List<FashionItem9aModel> items)
    {
        foreach (var item in items)
            item.IsOwned = true;
    }

    // Force-loads every clothing tab so "Set All Owned (All Tabs)" applies across all
    // ten sections, not just the ones the user has visited.
    private void SetAllOwnedAllTabs()
    {
        foreach (var section in _zaClothingSections)
        {
            section.Items ??= ParseZaClothing(section.Block);
            SetAllOwned(section.Items);
        }
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

    // Writes only sections whose items list has been materialized — unloaded sections
    // can't have been modified, so their underlying block bytes are already correct.
    private void SaveSvData()
    {
        foreach (var section in _svSections)
        {
            if (section.Items is null)
                continue;
            var raw = FashionItem9.GetArray(section.Block.Data);
            foreach (var m in section.Items)
            {
                raw[m.Index].Value = m.Value;
                raw[m.Index].IsNew = m.IsNew;
            }
            FashionItem9.SetArray(raw, section.Block.Data);
        }
    }

    private void SaveZaData()
    {
        foreach (var section in _zaClothingSections)
        {
            if (section.Items is null)
                continue;
            var raw = FashionItem9a.GetArray(section.Block.Data);
            foreach (var m in section.Items)
            {
                raw[m.Index].Value = m.Value;
                raw[m.Index].IsNew = m.IsNew;
                raw[m.Index].IsNewShop = m.IsNewShop;
                raw[m.Index].IsNewGroup = m.IsNewGroup;
                raw[m.Index].IsEquipped = m.IsEquipped;
                raw[m.Index].IsOwned = m.IsOwned;
            }
            FashionItem9a.SetArray(raw, section.Block.Data);
        }

        foreach (var section in _zaHairSections)
        {
            if (section.Items is null)
                continue;
            var raw = HairMakeItem9a.GetArray(section.Block.Data);
            foreach (var m in section.Items)
            {
                raw[m.Index].Value = m.Value;
                raw[m.Index].IsNew = m.IsNew;
            }
            HairMakeItem9a.SetArray(raw, section.Block.Data);
        }
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());

    public sealed class FashionSection<T>(string label, SCBlock block)
    {
        public string Label { get; } = label;
        public SCBlock Block { get; } = block;
        public List<T>? Items { get; set; }
    }

    public class FashionItemModel
    {
        public int Index { get; set; }
        public uint Value { get; set; }
        public bool IsNew { get; set; }

        // FashionItem9.None / FashionItem9a.None / HairMakeItem9a.None all = ushort.MaxValue.
        // A slot at this sentinel is "empty" — every flag on it is meaningless, so the UI
        // renders the ID as "—" and disables the per-row checkboxes.
        public bool IsEmpty => Value == ushort.MaxValue;
    }

    public sealed class FashionItem9aModel : FashionItemModel
    {
        public bool IsNewShop { get; set; }
        public bool IsNewGroup { get; set; }
        public bool IsEquipped { get; set; }
        public bool IsOwned { get; set; }
    }
}
