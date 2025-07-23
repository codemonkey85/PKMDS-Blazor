namespace Pkmds.Web;

public record AppState : IAppState
{
    public PKM? CopiedPokemon { get; set; }

    public string CurrentLanguage
    {
        get;
        set
        {
            field = value;
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);
        }
    } = GameLanguage.DefaultLanguage;

    public SaveFile? SaveFile
    {
        get;
        set
        {
            field = value;
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);

            BoxEdit = SaveFile is not null ? new(SaveFile) : null;
        }
    }

    public BoxEdit? BoxEdit { get; internal set; }

    public int CurrentLanguageId => SaveFile?.Language ?? (int)LanguageID.English;

    public int? SelectedBoxNumber { get; set; }

    public int? SelectedBoxSlotNumber { get; set; }

    public int? SelectedPartySlotNumber { get; set; }

    public bool ShowProgressIndicator { get; set; }

    public bool SelectedSlotsAreValid =>
        SelectedBoxNumber is null && SelectedBoxSlotNumber is not null && SaveFile is SAV7b
        || SelectedBoxNumber is not null && SelectedBoxSlotNumber is not null
        || SelectedPartySlotNumber is not null;

    public string? AppVersion => Assembly.GetAssembly(typeof(Program))?.GetName().Version?.ToVersionString();
}
