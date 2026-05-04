namespace Pkmds.Rcl.Services;

/// <summary>
/// Centralized prompt for actions that would discard or bypass unsaved Pokémon
/// edits — slot navigation, save-file export, etc. Mirrors the existing
/// "Add to Bank" pattern in <see cref="Components.EditForms.PokemonEditForm"/>.
/// </summary>
public static class UnsavedChangesGuard
{
    /// <summary>
    /// If the edit form has unsaved Pokémon changes, prompts the user to Save,
    /// Discard, or Cancel. Returns true when it is safe to proceed (no edits, or
    /// the user chose Save/Discard); false when the user chose Cancel or the save
    /// itself failed (so the caller does not silently abandon the user's edits by
    /// continuing on to discard them).
    /// </summary>
    public static async Task<bool> ConfirmAsync(
        IAppService appService,
        IDialogService dialogService,
        string message,
        string saveText = "Save",
        string discardText = "Discard",
        string cancelText = "Cancel",
        ISnackbar? snackbar = null)
    {
        if (!appService.EditFormHasUnsavedChanges())
        {
            return true;
        }

        var result = await dialogService.ShowMessageBoxAsync(
            "Unsaved Changes",
            message,
            yesText: saveText,
            noText: discardText,
            cancelText: cancelText);

        if (result is null)
        {
            return false;
        }

        if (result is true)
        {
            // SavePokemon ultimately calls PKHeX's SetPartySlotAtIndex / SetBoxSlotAtIndex,
            // which throw ArgumentException when the runtime PKM type doesn't match the
            // save's PKMType (see #843). Surface a friendly error and cancel the action
            // so the user's edits aren't discarded by the navigation/export that would
            // have followed a successful save.
            try
            {
                appService.SavePokemon(appService.EditFormPokemon);
            }
            catch (ArgumentException ex)
            {
                snackbar?.Add(
                    $"Could not save Pokémon to slot: {ex.Message}",
                    Severity.Error);
                return false;
            }
        }

        return true;
    }
}
