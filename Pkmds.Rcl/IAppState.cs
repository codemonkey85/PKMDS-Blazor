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
    /// Gets the numeric language ID corresponding to the <see cref="CurrentLanguage"/>.
    /// </summary>
    int CurrentLanguageId { get; }

    /// <summary>
    /// Gets or sets the currently loaded save file from a Pokémon game.
    /// This is null when no save file is loaded.
    /// </summary>
    SaveFile? SaveFile { get; set; }

    /// <summary>
    /// Gets the box editor interface for the current save file.
    /// This provides access to box manipulation operations.
    /// </summary>
    BoxEdit? BoxEdit { get; }

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
    /// Gets or sets whether the progress indicator should be displayed.
    /// Used to show loading spinners during long-running operations.
    /// </summary>
    bool ShowProgressIndicator { get; set; }

    /// <summary>
    /// Gets the current application version string.
    /// </summary>
    string? AppVersion { get; }

    /// <summary>
    /// Gets the version of PKHeX.Core library being used.
    /// </summary>
    static string? PkhexVersion => Assembly.GetAssembly(typeof(PKM))?.GetName().Version?.ToString();

    /// <summary>
    /// Gets whether the currently selected slots represent a valid selection.
    /// This is true when exactly one slot (either box or party) is selected.
    /// </summary>
    bool SelectedSlotsAreValid { get; }
}
