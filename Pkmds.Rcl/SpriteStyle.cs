namespace Pkmds.Rcl;

/// <summary>
///     Determines which CDN sprite set is used when displaying Pokémon in box and party slots.
/// </summary>
public enum SpriteStyle
{
    /// <summary>
    ///     High-resolution 512×512 HOME sprites from the PokeAPI home/ directory (default).
    /// </summary>
    Home,

    /// <summary>
    ///     Pixel-art sprites from the game-version-specific PokeAPI versions/ directory,
    ///     matched to the currently loaded save file's game.
    /// </summary>
    Game
}
