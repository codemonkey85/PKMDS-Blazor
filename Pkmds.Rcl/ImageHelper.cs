using static Pkmds.Core.Utilities.GameInfoUtilities;

namespace Pkmds.Rcl;

/// <summary>
/// Helper for file paths to bundled sprite images served from <c>_content/Pkmds.Rcl/sprites/</c>.
/// </summary>
/// <remarks>
/// The Pokémon / item / mystery-gift path logic lives in
/// <see cref="Pkmds.Core.Utilities.SpritePaths" /> (shared with the offline file-preview and
/// thumbnail providers, which resolve the same relative paths against their own bundled sprites);
/// this class just prepends the web root. App-only sprites (balls, types, bag pouches, move
/// categories, gender, TM/HM) stay here. PokeAPI CDN URL builders live in
/// <see cref="Pkmds.Core.Utilities.PokeApiSpriteUrls" />.
/// </remarks>
public static partial class ImageHelper
{
    private const string SpritesRoot = "_content/Pkmds.Rcl/sprites/";

    /// <summary>Fallback image path for unknown items.</summary>
    public const string ItemFallbackImageFileName = $"{SpritesRoot}{SpritePaths.ItemFallbackFile}";

    /// <summary>Fallback image path for unknown Pokémon.</summary>
    public const string PokemonFallbackImageFileName = $"{SpritesRoot}{SpritePaths.PokemonFallbackFile}";

    // Type byte → lowercase type name, matching PKHeX's text_Types_en.txt (0-indexed).
    private static readonly string[] TypeNames =
    [
        "normal", "fighting", "flying", "poison", "ground", "rock", "bug", "ghost", "steel",
        "fire", "water", "grass", "electric", "psychic", "ice", "dragon", "dark", "fairy"
    ];

    // Only four HM type sprites exist: normal, fighting, flying, water.
    private static readonly IReadOnlySet<string> HmTypeNames =
        new HashSet<string>(StringComparer.Ordinal) { "normal", "fighting", "flying", "water" };

    /// <summary>Gets the sprite filename for a Mystery Gift (either Pokémon or item).</summary>
    public static string GetMysteryGiftSpriteFileName(MysteryGift gift) =>
        $"{SpritesRoot}{SpritePaths.GetMysteryGiftSprite(gift)}";

    /// <summary>Gets the sprite filename for a Pokémon, handling all forms, genders, and special cases.</summary>
    public static string GetPokemonSpriteFilename(PKM? pokemon) =>
        $"{SpritesRoot}{SpritePaths.GetPokemonSprite(pokemon)}";

    /// <summary>
    /// Gets the bundled sprite filename for a specific species form, for use in form-picker UIs
    /// where a full PKM is not available. Does not handle eggs, gender differences, or totem forms.
    /// </summary>
    public static string GetPokemonSpriteFilenameForForm(ushort species, EntityContext context, byte form,
        uint? formArg = null) =>
        $"{SpritesRoot}{SpritePaths.GetPokemonSpriteForForm(species, context, form, formArg)}";

    /// <summary>Gets the sprite filename for an item, selecting size/style by generation.</summary>
    public static string GetItemSpriteFilename(int item, EntityContext context) =>
        $"{SpritesRoot}{SpritePaths.GetItemSprite(item, context)}";

    /// <summary>Gets the sprite filename for a Poké Ball.</summary>
    /// <param name="ball">The ball ID.</param>
    public static string GetBallSpriteFilename(int ball) =>
        $"{SpritesRoot}b/_ball{ball}.png";

    /// <summary>Gets the sprite filename for a type gem (used in type displays).</summary>
    public static string GetTypeGemSpriteFileName(byte type) =>
        $"{SpritesRoot}t/g/gem_{type:00}.png";

    /// <summary>Gets the sprite filename for a square type icon.</summary>
    public static string GetTypeSquareSpriteFileName(byte type) =>
        $"{SpritesRoot}t/s/type_icon_{type:00}.png";

    /// <summary>Gets the sprite filename for a square type icon.</summary>
    public static string GetTypeSquareSpriteFileName(int type) =>
        $"{SpritesRoot}t/s/type_icon_{type:00}.png";

    /// <summary>Gets the sprite filename for a wide type icon.</summary>
    public static string GetTypeWideSpriteFileName(byte type) =>
        $"{SpritesRoot}t/w/type_wide_{type:00}.png";

    /// <summary>Gets the sprite filename for a bag pouch icon.</summary>
    public static string GetBagPouchSpriteFileName(InventoryType type) =>
        $"{SpritesRoot}bag/Bag_{GetBagPouchSpriteName(type)}.png";

    /// <summary>Maps inventory types to bag pouch sprite names.</summary>
    private static string GetBagPouchSpriteName(InventoryType type) => type switch
    {
        InventoryType.Balls => "Balls",
        InventoryType.BattleItems => "Battle",
        InventoryType.Berries => "Berries",
        InventoryType.Candy => "Candy",
        InventoryType.FreeSpace => "Free",
        InventoryType.Ingredients => "Ingredient",
        InventoryType.Items => "Items",
        InventoryType.KeyItems => "Key",
        InventoryType.MailItems => "Mail",
        InventoryType.Medicine => "Medicine",
        InventoryType.PCItems => "PCItems",
        InventoryType.TMHMs => "Tech",
        InventoryType.Treasure => "Treasure",
        InventoryType.ZCrystals => "Z",
        InventoryType.MegaStones => "Mega",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>Gets the sprite filename for a move category icon (Physical/Special/Status).</summary>
    public static string GetMoveCategorySpriteFileName(MoveCategory moveCategory) =>
        moveCategory switch
        {
            MoveCategory.Status => $"{SpritesRoot}move/Status.png",
            MoveCategory.Physical => $"{SpritesRoot}move/Physical.png",
            MoveCategory.Special => $"{SpritesRoot}move/Special.png",
            _ => throw new ArgumentOutOfRangeException(nameof(moveCategory), moveCategory, null)
        };

    /// <summary>Gets the sprite filename for a gender icon (Male=0, Female=1, Genderless=2).</summary>
    public static string GetGenderSpriteFileName(Gender gender) =>
        $"{SpritesRoot}ac/gender_{(int)gender}.png";

    /// <summary>
    /// Gets the CSS class to apply to a Pokémon slot based on whether it contains a valid Pokémon.
    /// </summary>
    public static string GetSpriteCssClass(PKM? pkm) => (pkm?.Species).IsValidSpecies()
        ? " slot-fill"
        : string.Empty;

    /// <summary>
    /// Returns the TM disc sprite filename for the given PKHeX move-type byte.
    /// Uses type-colored disc sprites (tm-fire.png, tm-water.png, etc.) from wwwroot/sprites/tm/.
    /// Falls back to bitem_tm.png for Stellar type or any unknown type.
    /// </summary>
    public static string GetTypedTmSpriteFilename(byte moveType) =>
        moveType < TypeNames.Length
            ? $"{SpritesRoot}tm/tm-{TypeNames[moveType]}.png"
            : $"{SpritesRoot}bi/bitem_tm.png";

    /// <summary>
    /// Returns the HM disc sprite filename for the given PKHeX move-type byte, or null if no
    /// type-colored HM sprite exists for that type (caller should fall back to the item sprite).
    /// </summary>
    public static string? GetTypedHmSpriteFilename(byte moveType) =>
        moveType < TypeNames.Length && HmTypeNames.Contains(TypeNames[moveType])
            ? $"{SpritesRoot}hm/hm-{TypeNames[moveType]}.png"
            : null;
}
