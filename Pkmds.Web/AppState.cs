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
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile, IsHaXEnabled);
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
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile, IsHaXEnabled);

            // Create BoxEdit helper for the new save file
            BoxEdit = SaveFile is not null
                ? new(SaveFile)
                : null;

            if (SaveFile is null)
            {
                SaveFileName = null;
                ManicEmuSaveContext = null;
            }

            // Clear slot-A selections so a stale SelectedBoxNumber from a previous
            // save with more boxes cannot cause ArgumentOutOfRangeException in TradePane
            // when the new save has fewer boxes.
            SelectedBoxNumber = null;
            SelectedBoxSlotNumber = null;
            SelectedPartySlotNumber = null;
            PinnedBoxNumber = null;
        }
    }

    /// <inheritdoc />
    public string? SaveFileName { get; set; }

    /// <inheritdoc />
    public ManicEmuSaveHelper.ManicEmuSaveContext? ManicEmuSaveContext { get; set; }

    /// <inheritdoc />
    public SaveFile? SaveFileB
    {
        get;
        set
        {
            field = value;

            BoxEditB = field is not null
                ? new(field)
                : null;

            if (field is null)
            {
                SaveFileNameB = null;
                SelectedBoxNumberB = null;
                SelectedBoxSlotNumberB = null;
                SelectedPartySlotNumberB = null;
            }

            HasUnsavedChangesB = false;
        }
    }

    /// <inheritdoc />
    public string? SaveFileNameB { get; set; }

    /// <inheritdoc />
    public bool HasUnsavedChangesB { get; set; }

    /// <inheritdoc />
    public BoxEdit? BoxEdit { get; private set; }

    /// <inheritdoc />
    public BoxEdit? BoxEditB { get; private set; }

    /// <inheritdoc />
    public int CurrentLanguageId => SaveFile?.Language ?? (int)LanguageID.English;

    /// <inheritdoc />
    public int? SelectedBoxNumber { get; set; }

    /// <inheritdoc />
    public int? SelectedBoxSlotNumber { get; set; }

    /// <inheritdoc />
    public int? SelectedPartySlotNumber { get; set; }

    /// <inheritdoc />
    public int? SelectedBoxNumberB { get; set; }

    /// <inheritdoc />
    public int? SelectedBoxSlotNumberB { get; set; }

    /// <inheritdoc />
    public int? SelectedPartySlotNumberB { get; set; }

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

    public DateTime? AppBuildDate => Assembly.GetAssembly(typeof(Program))?.GetName().Version?.ToDateTime();

    /// <inheritdoc />
    public bool IsHaXEnabled
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            // FilteredSources.{Moves,Relearn,Species,Items} switch between
            // legal-filtered and HaX-permissive variants based on this flag;
            // rebuild so dropdowns reflect the new mode without a reload.
            // Safe when SaveFile is null — InitializeStrings skips the
            // FilteredSources rebuild in that case.
            LocalizeUtil.InitializeStrings(CurrentLanguage, SaveFile, IsHaXEnabled);
        }
    }

    /// <inheritdoc />
    public SpriteStyle SpriteStyle { get; set; }

    /// <inheritdoc />
    public int? PinnedBoxNumber { get; set; }

    /// <inheritdoc />
    public bool ShowLegalIndicator { get; set; } = true;

    /// <inheritdoc />
    public bool ShowFishyIndicator { get; set; } = true;

    /// <inheritdoc />
    public bool ShowIllegalIndicator { get; set; } = true;

    /// <inheritdoc />
    public bool HapticsEnabled { get; set; } = true;
}
