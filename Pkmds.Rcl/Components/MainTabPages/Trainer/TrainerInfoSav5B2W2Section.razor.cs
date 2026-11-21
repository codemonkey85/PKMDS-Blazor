namespace Pkmds.Rcl.Components.MainTabPages.Trainer;

public partial class TrainerInfoSav5B2W2Section
{
    [Parameter]
    [EditorRequired]
    public SAV5B2W2 SaveFile { get; set; } = null!;

    private void UnlockChallengeMode()
    {
        // Mirrors SAV_Misc5.ReadMain/SaveMain's separate obtained and unlocked
        // flags. KeySystem5 handles the save-specific XOR encoding; leave its
        // crypto seed and other keys intact. Unlocking does not select a mode.
        SaveFile.Keys.SetIsKeyObtained(KeyType5.Challenge, true);
        SaveFile.Keys.SetIsKeyUnlocked(KeyType5.Challenge, true);
        MarkEdited();
        Snackbar.Add("Challenge Mode unlocked. Select Challenge to activate it, then export your save.", Severity.Success);
    }

    private void SetDifficulty(Difficulty5 difficulty)
    {
        var available = difficulty switch
        {
            Difficulty5.Normal => true,
            Difficulty5.Easy => SaveFile.Keys.GetIsKeyUnlocked(KeyType5.Easy),
            Difficulty5.Challenge => SaveFile.Keys.GetIsKeyUnlocked(KeyType5.Challenge),
            _ => false,
        };
        if (!available || SaveFile.Keys.ActiveDifficulty == difficulty)
        {
            return;
        }

        SaveFile.Keys.ActiveDifficulty = difficulty;
        MarkEdited();
    }

    private void MarkEdited()
    {
        SaveFile.State.Edited = true;
        RefreshService.Refresh();
    }
}
