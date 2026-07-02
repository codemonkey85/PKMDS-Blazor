namespace Pkmds.Core.Utilities;

/// <summary>
/// Single source of truth for where Pokémon sprite images are fetched from.
/// </summary>
/// <remarks>
/// <para>
/// The app serves sprites from <c>codemonkey85/sprites</c> — our own fork of
/// <c>PokeAPI/sprites</c> — over the jsDelivr CDN. Owning the fork means an upstream
/// takedown or restructure of <c>PokeAPI/sprites</c> can't break the app; jsDelivr is a
/// swappable speed layer on top (multi-CDN with edge caching). A scheduled GitHub Action
/// in the fork keeps it synced with upstream so new-gen sprites keep arriving.
/// </para>
/// <para>
/// <b>To change the sprite host, CDN, fork, or branch in the future, edit
/// <see cref="RepoRoot"/> only.</b> Every sprite URL in the app — HOME artwork, game-version
/// pixel art, and the placeholder — is composed from it. For example, to drop jsDelivr and go
/// direct to GitHub raw, swap <see cref="RepoRoot"/> for
/// <c>https://raw.githubusercontent.com/codemonkey85/sprites/master/sprites/pokemon/</c>.
/// </para>
/// </remarks>
public static class SpriteSource
{
    // ── Transport: the ONE place to change CDN / fork / branch ───────────────
    // Serving codemonkey85/sprites (our fork) via jsDelivr's GitHub gateway.
    // Direct-from-GitHub alternative (no CDN):
    //   "https://raw.githubusercontent.com/codemonkey85/sprites/master/sprites/pokemon/"
    private const string RepoRoot =
        "https://cdn.jsdelivr.net/gh/codemonkey85/sprites@master/sprites/pokemon/";

    // ── Derived paths — composed from RepoRoot; do not hardcode elsewhere ─────

    /// <summary>Base URL for high-res HOME artwork (<c>other/home/</c>).</summary>
    public const string HomeBaseUrl = RepoRoot + "other/home/";

    /// <summary>Base URL for game-version pixel-art sprites (<c>versions/</c>).</summary>
    public const string VersionsBaseUrl = RepoRoot + "versions/";

    /// <summary>
    /// Placeholder sprite (species 0 / "egg-like" fallback) used when a real sprite URL
    /// can't be built for a given Pokémon.
    /// </summary>
    public const string PlaceholderSpriteUrl = HomeBaseUrl + "0.png";
}
