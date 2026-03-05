namespace Pkmds.Rcl.Services;

/// <summary>
/// Enum representing the type of Pokémon slot currently selected.
/// </summary>
public enum SelectedPokemonType
{
    /// <summary>No slot is selected, or the selection is invalid.</summary>
    None,

    /// <summary>A party slot is selected (slots 0-5).</summary>
    Party,

    /// <summary>A box slot is selected.</summary>
    Box
}
