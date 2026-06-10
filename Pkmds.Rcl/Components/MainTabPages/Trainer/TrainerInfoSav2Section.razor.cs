namespace Pkmds.Rcl.Components.MainTabPages.Trainer;

public partial class TrainerInfoSav2Section
{
    [Parameter]
    [EditorRequired]
    public SAV2 SaveFile { get; set; } = null!;

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
            "GS Ball event enabled. Collect the GS Ball at any Pokémon Center, then place it on the Ilex Forest shrine to encounter Celebi.",
            Severity.Success);
    }
}
