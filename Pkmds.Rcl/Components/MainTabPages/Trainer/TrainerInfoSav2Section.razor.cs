namespace Pkmds.Rcl.Components.MainTabPages.Trainer;

public partial class TrainerInfoSav2Section
{
    [Parameter]
    [EditorRequired]
    public SAV2 SaveFile { get; set; } = null!;

    private async Task ResetRtc()
    {
        // Mirrors PKHeX's SAVEditor Gen 2 RTC reset prompt: show the reset password
        // (non-Japanese saves only — Japanese GSC don't surface one) then confirm.
        var message = SaveFile.Japanese
            ? "Would you like to reset the in-game clock (RTC)?"
            : $"RTC Reset Password: {SaveFile.ResetKey:00000}{Environment.NewLine}{Environment.NewLine}" +
              "Would you like to reset the in-game clock (RTC)?";

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Reset Clock",
            message,
            yesText: "Reset",
            cancelText: "Cancel");

        if (confirmed is not true)
        {
            return;
        }

        // Sets the "Time Not Set" flag directly (no property setter), so mark the save
        // edited explicitly. The game re-prompts for the clock on next boot.
        SaveFile.ResetRTC();
        SaveFile.State.Edited = true;

        Snackbar.Add(
            "The in-game clock has been flagged for reset. The game will prompt you to set the time the next time you load this save.",
            Severity.Success);
    }

    private void EnableGSBallEvent()
    {
        if (SaveFile.IsEnabledGSBallMobileEvent)
        {
            Snackbar.Add("The GS Ball event is already enabled.", Severity.Info);
            return;
        }

        // Writes the GS Ball "available" magic value directly to the Crystal save
        // region (no property setter), so mark the save edited explicitly.
        SaveFile.EnableGSBallMobileEvent();
        SaveFile.State.Edited = true;

        Snackbar.Add(
            "GS Ball event enabled. Collect the GS Ball from the woman at the Goldenrod City Pokémon Center, " +
            "take it to Kurt in Azalea Town, then place it on the Ilex Forest shrine to encounter Celebi.",
            Severity.Success);
    }
}
