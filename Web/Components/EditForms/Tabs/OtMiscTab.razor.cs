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

        Pokemon.OriginalTrainerName = saveFile?.OT ?? string.Empty;
        Pokemon.OriginalTrainerGender = saveFile?.Gender ?? (byte)Gender.Male;

        var format = saveFile?.GetTrainerIDFormat();
        switch (format)
        {
            case TrainerIDFormat.SixteenBitSingle: // Gen 1-2
                //Pokemon.SetTrainerID16(saveFile?.TID ?? 0);
                break;
            case TrainerIDFormat.SixteenBit: // Gen 3-6
                Pokemon.TID16 = saveFile?.TID16 ?? 0;
                Pokemon.SID16 = saveFile?.SID16 ?? 0;
                break;
            case TrainerIDFormat.SixDigit: // Gen 7+
                Pokemon.SetTrainerTID7(saveFile?.TrainerTID7 ?? 0U);
                Pokemon.SetTrainerSID7(saveFile?.TrainerSID7 ?? 0U);
                break;
            default:
                break;
        }
    }
}
