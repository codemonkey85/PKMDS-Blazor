namespace Pkmds.Web.Components.EditForms.Tabs;

public partial class OtMiscTab : IDisposable
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

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
            default:
                break;
        }
    }

    private void OnGenderToggle()
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.OriginalTrainerGender = (byte)((Gender)Pokemon.OriginalTrainerGender switch
        {
            Gender.Male => Gender.Female,
            _ => Gender.Male
        });
    }
}
