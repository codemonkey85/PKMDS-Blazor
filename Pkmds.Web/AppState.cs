namespace Pkmds.Web;

/// <summary>
/// Blazor WebAssembly implementation of the application state.
/// This record holds the global state for the PKMDS application including the current save file,
/// selected slots, clipboard data, and UI preferences.
/// </summary>
public record AppState : IAppState
{
    /// <inheritdoc />
    public PKM? CopiedPokemon { get; set; }

    /// <inheritdoc />
    public string CurrentLanguage
    {
        get;
        set
        {
            field = value;
            // Re-initialize localized strings when language changes
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);
        }
    } = GameLanguage.DefaultLanguage;

    /// <inheritdoc />
    public SaveFile? SaveFile
    {
        get;
        set
        {
            field = value;
            // Re-initialize localized strings when save file changes
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile);

            // Create BoxEdit helper for the new save file
            BoxEdit = SaveFile is not null
                ? new(SaveFile)
                : null;
        }
    }

    /// <inheritdoc />
    public BoxEdit? BoxEdit { get; private set; }

    /// <inheritdoc />
    public int CurrentLanguageId => SaveFile?.Language ?? (int)LanguageID.English;

    /// <inheritdoc />
    public int? SelectedBoxNumber { get; set; }

    /// <inheritdoc />
    public int? SelectedBoxSlotNumber { get; set; }

    /// <inheritdoc />
    public int? SelectedPartySlotNumber { get; set; }

    /// <inheritdoc />
    public bool ShowProgressIndicator { get; set; }

    /// <inheritdoc />
    public bool SelectedSlotsAreValid =>
        // Let's Go storage slot (no box number, but has box slot)
        SelectedBoxNumber is null && SelectedBoxSlotNumber is not null && SaveFile is SAV7b
        // Regular box slot
        || SelectedBoxNumber is not null && SelectedBoxSlotNumber is not null
        // Party slot
        || SelectedPartySlotNumber is not null;

    /// <inheritdoc />
    public string? AppVersion => Assembly.GetAssembly(typeof(Program))?.GetName().Version?.ToVersionString();
}
