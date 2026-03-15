namespace Pkmds.Rcl.Models;

/// <summary>
///     Persistent user-configurable application settings.
///     Serialized to localStorage under the key <c>pkmds_settings</c>.
/// </summary>
public record AppSettings
{
    /// <summary>
    ///     Theme preference: <c>"light"</c>, <c>"dark"</c>, or <c>"system"</c> (default).
    ///     Also mirrored to the <c>pkmds_theme</c> key for early JS theme initialization.
    /// </summary>
    public string ThemeMode { get; init; } = "system";

    /// <summary>
    ///     Whether PKHaX (unrestricted editing) mode is enabled.
    /// </summary>
    public bool IsHaXEnabled { get; init; }

    /// <summary>
    ///     Whether verbose logging to the browser console is enabled.
    /// </summary>
    public bool IsVerboseLoggingEnabled { get; init; }

    /// <summary>
    ///     Default OT (Original Trainer) name pre-filled when creating new Pokémon.
    /// </summary>
    public string DefaultOtName { get; init; } = string.Empty;

    /// <summary>
    ///     Default Trainer ID pre-filled when creating new Pokémon.
    /// </summary>
    public uint DefaultTrainerId { get; init; }

    /// <summary>
    ///     Default Secret ID pre-filled when creating new Pokémon.
    /// </summary>
    public uint DefaultSecretId { get; init; }

    /// <summary>
    ///     Default language for new Pokémon, stored as a <see cref="PKHeX.Core.LanguageID" /> byte value.
    /// </summary>
    public LanguageID DefaultLanguageId { get; init; } = LanguageID.English;

    /// <summary>
    ///     Which CDN sprite set to display in box and party slots.
    ///     Defaults to <see cref="SpriteStyle.Home" /> (high-res HOME sprites).
    /// </summary>
    public SpriteStyle SpriteStyle { get; init; } = SpriteStyle.Home;
}
