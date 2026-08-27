namespace Pkmds.Rcl.Components.MainTabPages;

public partial class TradePane : RefreshAwareComponent
{
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public SaveFile? SaveFile { get; set; }

    // The paired save in the Trade tab — forwarded to each TradeSlot so it can indicate
    // which Pokémon are ineligible to transfer to the other pane.
    [Parameter]
    public SaveFile? CounterpartSaveFile { get; set; }

    [Parameter]
    public int? SelectedBox { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedBoxChanged { get; set; }

    [Parameter]
    public int? SelectedBoxSlot { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedBoxSlotChanged { get; set; }

    [Parameter]
    public int? SelectedPartySlot { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedPartySlotChanged { get; set; }

    // Fired when a Pokémon is dropped onto any slot in this pane. Propagated up to
    // TradeTab, which reads the drag source from IDragDropService and runs the transfer.
    [Parameter]
    public EventCallback<TradeSlotTarget> OnSlotDrop { get; set; }

    private async Task SelectBox(int boxNum)
    {
        SelectedBox = boxNum;
        SelectedBoxSlot = null;
        await SelectedBoxChanged.InvokeAsync(boxNum);
        await SelectedBoxSlotChanged.InvokeAsync(null);
    }

    private async Task SelectBoxSlot(int boxNum, int slotNum)
    {
        SelectedBox = boxNum;
        SelectedBoxSlot = slotNum;
        SelectedPartySlot = null;
        await SelectedBoxChanged.InvokeAsync(boxNum);
        await SelectedBoxSlotChanged.InvokeAsync(slotNum);
        await SelectedPartySlotChanged.InvokeAsync(null);
    }

    private async Task SelectParty(int slotNum)
    {
        SelectedPartySlot = slotNum;
        SelectedBoxSlot = null;
        await SelectedPartySlotChanged.InvokeAsync(slotNum);
        await SelectedBoxSlotChanged.InvokeAsync(null);
    }

    private static string BoxGridClass(SaveFile saveFile) =>
        saveFile.BoxSlotCount == 20
            ? "grid grid-cols-4 gap-1 w-full max-w-80 mx-auto"
            : "grid grid-cols-6 gap-1 w-full mx-auto";

    // Bypass GameInfo.FilteredSources (pinned to whichever save PKHeX last initialized
    // against) by going through the raw GameStrings table. Always use the app's current
    // language — we don't want info-panel labels showing in the save's *game* language
    // (e.g. Japanese names for a JP Blue save when the user's app is English).
    private static GameStrings AppStrings => GameInfo.GetStrings(GameInfo.CurrentLanguage);

    private static int GetBoxPokemonCount(SaveFile saveFile, int boxNumber)
    {
        var count = 0;
        for (var i = 0; i < saveFile.BoxSlotCount; i++)
        {
            if (saveFile.GetBoxSlotAtIndex(boxNumber, i) is { Species: > 0 })
            {
                count++;
            }
        }
        return count;
    }

    internal PKM? SelectedPokemon
    {
        get
        {
            if (SaveFile is not { } sav)
            {
                return null;
            }

            if (SelectedPartySlot is { } partySlot && partySlot < sav.PartyCount)
            {
                return sav.TryGetPartySlot(partySlot);
            }

            if (SelectedBox is { } boxNum && SelectedBoxSlot is { } boxSlot)
            {
                return sav.GetBoxSlotAtIndex(boxNum, boxSlot);
            }

            return null;
        }
    }
}
