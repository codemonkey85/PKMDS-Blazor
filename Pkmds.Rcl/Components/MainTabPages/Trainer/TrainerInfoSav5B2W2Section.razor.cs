namespace Pkmds.Rcl.Components.MainTabPages.Trainer;

public partial class TrainerInfoSav5B2W2Section
{
    [Parameter]
    [EditorRequired]
    public SAV5B2W2 SaveFile { get; set; } = null!;

    private static readonly KeyEntry[] KeyEntries =
    [
        new(KeyType5.Easy, "Easy Key", "Easy Mode"),
        new(KeyType5.Challenge, "Challenge Key", "Challenge Mode"),
        new(KeyType5.City, "City Key", "Black City / White Forest"),
        new(KeyType5.Iron, "Iron Key", "Iron Chamber"),
        new(KeyType5.Iceberg, "Iceberg Key", "Iceberg Chamber"),
    ];

    private bool AllKeysUnlocked => KeyEntries.All(key =>
        SaveFile.Keys.GetIsKeyObtained(key.Type) && SaveFile.Keys.GetIsKeyUnlocked(key.Type));

    private void SetObtained(KeyType5 key, bool value)
    {
        // Mirrors SAV_Misc5.ReadMain/SaveMain's separate obtained and unlocked
        // flags. KeySystem5 handles the save-specific XOR encoding. Keep the
        // flags independent and leave active selections and the crypto seed intact.
        if (SaveFile.Keys.GetIsKeyObtained(key) == value)
        {
            return;
        }

        SaveFile.Keys.SetIsKeyObtained(key, value);
        MarkEdited();
    }

    private void SetUnlocked(KeyType5 key, bool value)
    {
        if (SaveFile.Keys.GetIsKeyUnlocked(key) == value)
        {
            return;
        }

        SaveFile.Keys.SetIsKeyUnlocked(key, value);
        MarkEdited();
    }

    private void UnlockAllKeys()
    {
        // Equivalent to SAV_Misc5.B_AllKeys_Click: check all ten flags without
        // switching the active difficulty, city, or chamber.
        foreach (var key in KeyEntries)
        {
            SaveFile.Keys.SetIsKeyObtained(key.Type, true);
            SaveFile.Keys.SetIsKeyUnlocked(key.Type, true);
        }

        MarkEdited();
        Snackbar.Add("All keys obtained and unlocked. Export your save to use the changes in-game.", Severity.Success);
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

    private sealed record KeyEntry(KeyType5 Type, string Name, string UnlockName);
}
