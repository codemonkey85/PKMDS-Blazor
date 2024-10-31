namespace Pkmds.Web.Components.MainTabPages;

public partial class TrainerInfoTab
{
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
