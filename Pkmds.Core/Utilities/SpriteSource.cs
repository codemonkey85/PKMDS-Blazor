namespace Pkmds.Core.Utilities;

/// <summary>Public R2/CDN paths for Pokemon artwork and game sprites.</summary>
/// <remarks>
/// Keep the published service worker's spritePathPrefix in sync with PokemonBaseUrl.
/// Collections are immutable and pinned to the source revision. Missing upstream images
/// use bundled fallbacks. See SPRITE_HOSTING.md for publishing and rollback instructions.
/// </remarks>
public static class SpriteSource
{
    /// <summary>Base URL for the hosted Pokemon sprite collection.</summary>
    public const string PokemonBaseUrl =
        "https://sprites.pkmds.app/collections/a7a3d62bb80a654ab5e01b488659929af0077c31/pokemon/";

    /// <summary>Base URL for HOME artwork.</summary>
    public const string HomeBaseUrl = PokemonBaseUrl + "other/home/";

    /// <summary>Base URL for game-version sprites.</summary>
    public const string VersionsBaseUrl = PokemonBaseUrl + "versions/";

    /// <summary>Placeholder used when a sprite URL cannot be built.</summary>
    public const string PlaceholderSpriteUrl = HomeBaseUrl + "0.png";
}
