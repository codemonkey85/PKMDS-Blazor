namespace Pkmds.Rcl.Components.EditForms.Tabs;

public partial class OtMiscTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private void FillFromGame()
    {
        if (Pokemon is null || AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        Pokemon.OriginalTrainerName = saveFile.OT;
        Pokemon.OriginalTrainerGender = saveFile.Gender;

        var format = saveFile.GetTrainerIDFormat();
        switch (format)
        {
            case TrainerIDFormat.SixteenBitSingle: // Gen 1-2
                //Pokemon.SetTrainerID16(saveFile.TID);
                break;
            case TrainerIDFormat.SixteenBit: // Gen 3-6
                Pokemon.TID16 = saveFile.TID16;
                Pokemon.SID16 = saveFile.SID16;
                break;
            case TrainerIDFormat.SixDigit: // Gen 7+
                Pokemon.SetTrainerTID7(saveFile.TrainerTID7);
                Pokemon.SetTrainerSID7(saveFile.TrainerSID7);
                break;
        }
    }

    private void OnGenderToggle(Gender newGender)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.OriginalTrainerGender = (byte)newGender;
    }

    private void SetPokemonEc(uint newEc)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.EncryptionConstant = newEc;

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

    private void SetPokemonEc(string newEcHex)
    {
        if (Pokemon is null || !uint.TryParse(newEcHex, NumberStyles.HexNumber, null, out var parsedEc))
        {
            return;
        }

        Pokemon.EncryptionConstant = parsedEc;

        AppService.LoadPokemonStats(Pokemon);
        RefreshService.Refresh();
    }

    private void SetPokemonHomeTracker(string newPidHex)
    {
        if (Pokemon is not IHomeTrack homeTrack || !uint.TryParse(newPidHex, NumberStyles.HexNumber, null, out var parsedPid))
        {
            return;
        }

        homeTrack.Tracker = parsedPid;
    }
}
