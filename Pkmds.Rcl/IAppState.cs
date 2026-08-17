namespace Pkmds.Rcl;

/// <summary>
/// Represents the global application state for the PKMDS application.
/// This interface provides access to the currently loaded save file, selected Pokémon slots,
/// clipboard data, and other application-wide state information.
/// </summary>
public interface IAppState
{
    /// <summary>
    /// Gets or sets the current language code used for displaying game data.
    /// </summary>
    string CurrentLanguage { get; set; }

    /// <summary>
    /// Gets the numeric language ID corresponding to the <see cref="CurrentLanguage" />.
    /// </summary>
    int CurrentLanguageId { get; }

    /// <summary>
    /// Gets or sets the currently loaded save file from a Pokémon game.
    /// This is null when no save file is loaded.
    /// </summary>
    SaveFile? SaveFile { get; set; }

    /// <summary>
    /// Gets or sets the original file name of the loaded save file as provided by the user.
    /// This is null when no save file is loaded.
    /// </summary>
    string? SaveFileName { get; set; }

    /// <summary>
    /// Gets or sets an untouched copy of the bytes supplied when the current save was loaded.
    /// Bug reports use this instead of serializing a potentially misdetected save object.
    /// </summary>
    byte[]? OriginalSaveFileBytes { get; set; }

    /// <summary>
    /// Gets or sets the ZIP context captured when the user uploaded an archived save.
    /// Export, auto-backup, and bug-report submission all read this to round-trip the
    /// original wrapper instead of writing bare save bytes under an archive filename.
    /// Cleared when the save file is unloaded.
    /// </summary>
    SaveArchiveContext? SaveArchiveContext { get; set; }

    /// <summary>
    /// Gets or sets the secondary save file used by the cross-save Trade tab.
    /// Independent from <see cref="SaveFile" />; remains null until the user explicitly
    /// loads a second save from the Trade tab.
    /// </summary>
    SaveFile? SaveFileB { get; set; }

    /// <summary>
    /// Gets or sets the original file name of the secondary (slot-B) save file.
    /// </summary>
    string? SaveFileNameB { get; set; }

    /// <summary>
    /// Gets or sets whether the secondary (slot-B) save file has uncommitted edits
    /// from the Trade tab that have not yet been exported. Reset to false when slot B
    /// is loaded, replaced, exported, or unloaded.
    /// </summary>
    bool HasUnsavedChangesB { get; set; }

    /// <summary>
    /// Gets the box editor interface for the current save file.
    /// This provides access to box manipulation operations.
    /// </summary>
    BoxEdit? BoxEdit { get; }

    /// <summary>
    /// Gets the box editor interface for the secondary (slot-B) save file.
    /// Null when no secondary save file is loaded.
    /// </summary>
    BoxEdit? BoxEditB { get; }

    /// <summary>
    /// Gets or sets the Pokémon currently stored in the clipboard for copy/paste operations.
    /// </summary>
    PKM? CopiedPokemon { get; set; }

    /// <summary>
    /// Gets or sets the currently selected box number (0-based index).
    /// Null when no box is selected or when a party slot is selected.
    /// </summary>
    int? SelectedBoxNumber { get; set; }

    /// <summary>
    /// Gets or sets the currently selected box slot number (0-based index within a box).
    /// Null when no box slot is selected or when a party slot is selected.
    /// </summary>
    int? SelectedBoxSlotNumber { get; set; }

    /// <summary>
    /// Gets or sets the currently selected party slot number (0-based index, 0-5 for party).
    /// Null when no party slot is selected or when a box slot is selected.
    /// </summary>
    int? SelectedPartySlotNumber { get; set; }

    /// <summary>
    /// Gets or sets the slot-B currently selected box number (0-based index) on the Trade tab.
    /// </summary>
    int? SelectedBoxNumberB { get; set; }

    /// <summary>
    /// Gets or sets the slot-B currently selected box slot number (0-based index within a box).
    /// </summary>
    int? SelectedBoxSlotNumberB { get; set; }

    /// <summary>
    /// Gets or sets the slot-B currently selected party slot number (0-based index, 0-5).
    /// </summary>
    int? SelectedPartySlotNumberB { get; set; }

    /// <summary>
    /// Gets or sets whether the progress indicator should be displayed.
    /// Used to show loading spinners during long-running operations.
    /// </summary>
    bool ShowProgressIndicator { get; set; }

    /// <summary>
    /// Gets the current application version string.
    /// </summary>
    string? AppVersion { get; }

    /// <summary>
    /// Gets the build date of the application, derived from the assembly version.
    /// </summary>
    DateTime? AppBuildDate { get; }

    /// <summary>
    /// Gets the version of PKHeX.Core library being used.
    /// </summary>
    static string? PkhexVersion => Assembly.GetAssembly(typeof(PKM))?.GetName().Version?.ToString();

    /// <summary>
    /// Gets whether the currently selected slots represent a valid selection.
    /// This is true when exactly one slot (either box or party) is selected.
    /// </summary>
    bool SelectedSlotsAreValid { get; }

    /// <summary>
    /// Gets or sets whether PKHaX (Illegal Mode) is enabled.
    /// When true, legality restrictions are suppressed and certain editing limits are lifted.
    /// Persisted across sessions via localStorage.
    /// </summary>
    bool IsHaXEnabled { get; set; }

    /// <summary>
    /// Gets or sets the sprite style used for box and party slot images.
    /// Persisted across sessions via localStorage.
    /// </summary>
    SpriteStyle SpriteStyle { get; set; }

    /// <summary>
    /// Gets or sets the box number currently pinned in the secondary panel (0-based index).
    /// Null when no box is pinned. Pinning a box replaces the Pokémon editor in the right column.
    /// </summary>
    int? PinnedBoxNumber { get; set; }

    /// <summary>Whether to render the green "legal" indicator on slots.</summary>
    bool ShowLegalIndicator { get; set; }

    /// <summary>Whether to render the yellow "fishy" indicator on slots.</summary>
    bool ShowFishyIndicator { get; set; }

    /// <summary>Whether to render the red "illegal" indicator on slots.</summary>
    bool ShowIllegalIndicator { get; set; }

    /// <summary>
    /// Gets or sets whether haptic feedback is enabled for key interactions.
    /// No effect on devices without <c>navigator.vibrate</c> (notably iOS Safari).
    /// Persisted across sessions via localStorage.
    /// </summary>
    bool HapticsEnabled { get; set; }
}
