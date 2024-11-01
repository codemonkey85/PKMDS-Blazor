namespace Pkmds.Web.Components.MainTabPages;

public partial class TrainerInfoTab : IDisposable
{
    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private void OnGenderToggle()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        saveFile.Gender = (byte)((Gender)saveFile.Gender switch
        {
            Gender.Male => Gender.Female,
            _ => Gender.Male
        });
    }
}
