namespace Pkmds.Rcl;

/// <summary>
///     Partial class for overlay icon sprite filename generation (alpha, dynamax, held item, party slot, rare, starter,
///     team indicators).
/// </summary>
public static partial class ImageHelper
{
    /// <summary>Gets the sprite filename for the egg overlay icon shown in a slot when a Pokémon is an egg.</summary>
    public static string GetEggOverlaySpriteFileName(ushort species) =>
        species == (ushort)Species.Manaphy
            ? $"{SpritesRoot}a/a_490-e.png"
            : $"{SpritesRoot}a/a_egg.png";

    /// <summary>Gets the sprite filename for an alpha indicator (Pokémon Legends: Arceus).</summary>
    public static string GetAlphaIndicatorSpriteFileName(bool isAlt = false) =>
        $"{SpritesRoot}overlay/alpha{(isAlt ? "_alt" : string.Empty)}.png";

    /// <summary>Gets the sprite filename for a Dynamax indicator (Gigantamax).</summary>
    public static string GetDynamaxIndicatorSpriteFileName() =>
        $"{SpritesRoot}overlay/dyna.png";

    /// <summary>Gets the sprite filename for a held item indicator.</summary>
    public static string GetHeldItemIndicatorSpriteFileName() =>
        $"{SpritesRoot}overlay/helditem.png";

    /// <summary>Gets the sprite filename for a locked Pokémon overlay.</summary>
    public static string GetLockedOverlaySpriteFileName() =>
        $"{SpritesRoot}overlay/locked.png";

    /// <summary>Gets the sprite filename for a party slot indicator (slots 1-6).</summary>
    public static string GetPartySlotIndicatorSpriteFileName(int slotNumber) =>
        $"{SpritesRoot}overlay/party{slotNumber}.png";

    /// <summary>Gets the sprite filename for a rare/shiny indicator overlay.</summary>
    public static string GetRareIndicatorSpriteFileName(bool isAlt = false) =>
        $"{SpritesRoot}overlay/rare_icon{(isAlt ? "_alt" : string.Empty)}.png";

    /// <summary>Gets the sprite filename for a rare indicator overlay (second variant).</summary>
    public static string GetRareIconSecondSpriteFileName(bool isAlt = false) =>
        $"{SpritesRoot}overlay/rare_icon_2{(isAlt ? "_alt" : string.Empty)}.png";

    /// <summary>Gets the sprite filename for a starter Pokémon indicator.</summary>
    public static string GetStarterIndicatorSpriteFileName() =>
        $"{SpritesRoot}overlay/starter.png";

    /// <summary>Gets the sprite filename for a team indicator overlay.</summary>
    public static string GetTeamIndicatorSpriteFileName() =>
        $"{SpritesRoot}overlay/team.png";
}
