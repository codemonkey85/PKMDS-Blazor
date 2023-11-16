namespace Pkmds.Web;

public record AppState : IAppState
{
    public AppState()
    {
        LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);
    }

    private string currentLanguage = GameLanguage.DefaultLanguage;
    private SaveFile? saveFile;

    public string CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            currentLanguage = value;
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);
        }
    }

    public SaveFile? SaveFile
    {
        get => saveFile;
        set
        {
            saveFile = value;
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);
        }
    }

    public int CurrentLanguageId => SaveFile?.Language ?? (int)LanguageID.English;

    public int? SelectedBoxNumber { get; set; }

    public int? SelectedBoxSlotNumber { get; set; }

    public int? SelectedPartySlotNumber { get; set; }

    public bool ShowProgressIndicator { get; set; }
}
