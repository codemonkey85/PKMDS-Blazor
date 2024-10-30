namespace Pkmds.Web.Components;

public partial class SaveFileComponent : IDisposable
{
    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    private string SaveFileNameDisplayString()
    {
        const string baseTitle = "PKMDS Save Editor";

        if (AppState.SaveFile is not { } saveFile)
        {
            return baseTitle;
        }

        var sbTitle = new StringBuilder(baseTitle).Append(" - ");

        sbTitle.Append($"{saveFile.OT} ");
        
        if (saveFile.Context is not EntityContext.Gen1)
        {
            var genderDisplay = saveFile.Gender == (byte)Gender.Male ? Constants.MaleGenderUnicode : Constants.FemaleGenderUnicode;
            sbTitle.Append($"{genderDisplay} ");
        }
        
        sbTitle.Append($"({saveFile.DisplayTID.ToString(AppService.GetIdFormatString())}, ");
        
        sbTitle.Append($"{SaveFileNameDisplay.FriendlyGameName(saveFile.Version)}, ");
        
        sbTitle.Append($"{saveFile.PlayTimeString})");

        return sbTitle.ToString();
    }
}
