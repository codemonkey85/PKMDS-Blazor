namespace Pkmds.Rcl;

/// <summary>
/// Partial class for status condition icon sprite filename generation.
/// </summary>
public static partial class ImageHelper
{
    /// <summary>Gets the sprite filename for a burn status icon.</summary>
    public static string GetBurnStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickburn.png";

    /// <summary>Gets the sprite filename for a faint status icon.</summary>
    public static string GetFaintStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickfaint.png";

    /// <summary>Gets the sprite filename for a frostbite status icon.</summary>
    public static string GetFrostbiteStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickfrostbite.png";

    /// <summary>Gets the sprite filename for a paralysis status icon.</summary>
    public static string GetParalysisStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickparalyze.png";

    /// <summary>Gets the sprite filename for a poison status icon.</summary>
    public static string GetPoisonStatusSpriteFileName() =>
        $"{SpritesRoot}status/sickpoison.png";

    /// <summary>Gets the sprite filename for a sleep status icon.</summary>
    public static string GetSleepStatusSpriteFileName() =>
        $"{SpritesRoot}status/sicksleep.png";

    /// <summary>Gets the sprite filename for a toxic poison status icon.</summary>
    public static string GetToxicStatusSpriteFileName() =>
        $"{SpritesRoot}status/sicktoxic.png";
}
