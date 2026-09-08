namespace Pkmds.Rcl.Services;

/// <summary>
/// Resolves save sub-versions that cannot be determined from the save data or filename alone.
/// </summary>
public static class SaveVersionResolver
{
    /// <summary>
    /// Prompts for the FireRed/LeafGreen version when PKHeX could only identify their shared
    /// save format.
    /// </summary>
    /// <returns><see langword="false" /> when the user cancels the load.</returns>
    public static async Task<bool> ResolveAmbiguousVersionAsync(
        SaveFile saveFile,
        IDialogService dialogService)
    {
        if (saveFile is not SAV3FRLG frlg || frlg.Version.IsValidSavedVersion())
        {
            return true;
        }

        var isFireRed = await dialogService.ShowMessageBoxAsync(
            "Choose game version",
            "FireRed and LeafGreen use the same save format, and the filename does not identify " +
            "which game this save came from. Which version is it?",
            yesText: "FireRed",
            noText: "LeafGreen",
            cancelText: "Cancel");

        if (!isFireRed.HasValue)
        {
            return false;
        }

        var version = isFireRed.Value ? GameVersion.FR : GameVersion.LG;
        return frlg.ResetPersonal(version);
    }
}
