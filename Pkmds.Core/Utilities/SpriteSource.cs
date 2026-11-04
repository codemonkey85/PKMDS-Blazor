namespace Pkmds.Core.Utilities;

/// <summary>Public R2/CDN paths for Pokemon artwork and game sprites.</summary>
/// <remarks>
/// Keep the published service worker's spritePathPrefix in sync with PokemonBaseUrl.
/// The initial UAT deployment contains samples; missing images use bundled fallbacks.
/// See SPRITE_HOSTING.md for the sample list and remaining rollout work.
/// </remarks>
public static class SpriteSource
{
    /// <summary>Base URL for the hosted Pokemon sprite collection.</summary>
    public const string PokemonBaseUrl = "https://sprites.pkmds.app/pokemon/";

    /// <summary>Base URL for HOME artwork.</summary>
    public const string HomeBaseUrl = PokemonBaseUrl + "other/home/";

    /// <summary>Base URL for game-version sprites.</summary>
    public const string VersionsBaseUrl = PokemonBaseUrl + "versions/";

    /// <summary>Placeholder used when a sprite URL cannot be built.</summary>
    public const string PlaceholderSpriteUrl = HomeBaseUrl + "0.png";
}
