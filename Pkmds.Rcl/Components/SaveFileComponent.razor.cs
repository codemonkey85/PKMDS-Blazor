namespace Pkmds.Rcl.Components;

public partial class SaveFileComponent : RefreshAwareComponent
{
    private int activeTabIndex;

    internal static bool HasEditableInventory(SaveFile saveFile) => saveFile.Inventory.Pouches.Count != 0;

    internal static string GetBattleRevolutionProfileLabel(SAV4BR saveFile, int profile)
    {
        var name = profile == saveFile.CurrentSlot
            ? saveFile.CurrentOT
            : saveFile.SaveNames[profile];
        return string.IsNullOrWhiteSpace(name)
            ? $"Profile {profile + 1} (empty)"
            : $"Profile {profile + 1}: {name}";
    }

    private async Task OnBattleRevolutionProfileChangedAsync(SAV4BR saveFile, int profile)
    {
        if (saveFile.CurrentSlot == profile ||
            !await UnsavedChangesGuard.ConfirmAsync(
                AppService,
                DialogService,
                "This Pokémon has unsaved changes. Save or discard those changes before switching Battle Revolution profiles.",
                snackbar: Snackbar))
        {
            return;
        }

        saveFile.CurrentSlot = profile;
        AppState.BoxEdit?.LoadBox(saveFile.CurrentBox);
        AppService.ClearSelection();
        RefreshService.RefreshBoxAndPartyState();
    }

    private void JumpToPartyBox() => activeTabIndex = 0;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RefreshServiceField!.OnRequestJumpToPartyBox += HandleRequestJumpToPartyBox;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RefreshServiceField!.OnRequestJumpToPartyBox -= HandleRequestJumpToPartyBox;
        }
    }

    private void HandleRequestJumpToPartyBox()
    {
        JumpToPartyBox();
        _ = InvokeAsync(StateHasChanged);
    }
}
