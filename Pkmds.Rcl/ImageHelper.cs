namespace Pkmds.Rcl;

/// <summary>
/// Helper class for generating file paths to Pokémon and item sprite images.
/// Handles sprite selection based on species, form, gender, context, and other attributes.
/// </summary>
public static partial class ImageHelper
{
    private const string SpritesRoot = "_content/Pkmds.Rcl/sprites/";
    private const int PikachuStarterForm = 8;
    private const int EeveeStarterForm = 1;

    /// <summary>Fallback image path for unknown items.</summary>
    public const string ItemFallbackImageFileName = $"{SpritesRoot}bi/bitem_unk.png";

    /// <summary>Fallback image path for unknown Pokémon.</summary>
    public const string PokemonFallbackImageFileName = $"{SpritesRoot}a/a_unknown.png";

    // Mail item IDs for different generations
    private static readonly int[] Gen2MailIds = [0x9E, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD];
    private static readonly int[] Gen3MailIds = [121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132];
    private static readonly int[] Gen45MailIds = [137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148];

    /// <summary>
    /// Gets the sprite filename for a Mystery Gift (either Pokémon or item).
    /// </summary>
    public static string GetMysteryGiftSpriteFileName(MysteryGift gift) => gift.IsItem
        ? GetItemSpriteFilename(gift.ItemID, gift.Context)
        : GetPokemonSpriteFilename(gift.Species, gift.Context, gift.IsEgg, gift.Form, 0, gift.Gender);

    /// <summary>
    /// Gets the sprite filename for a Pokémon, handling all forms, genders, and special cases.
    /// </summary>
    public static string GetPokemonSpriteFilename(PKM? pokemon) => pokemon is null
        ? PokemonFallbackImageFileName
        : GetPokemonSpriteFilename(pokemon.Species, pokemon.Context, pokemon.IsEgg, pokemon.Form,
            pokemon.GetFormArgument(0), pokemon.Gender);

    /// <summary>
    /// Internal method to construct the Pokémon sprite filename based on various attributes.
    /// Handles special cases like starter Pikachu/Eevee, eggs, gender differences, Alcremie variations, etc.
    /// </summary>
    private static string GetPokemonSpriteFilename(ushort species, EntityContext context, bool isEgg, byte form,
        uint? formArg1, byte gender) =>
        new StringBuilder($"{SpritesRoot}a/a_")
            .Append((species, context, isEgg, form, formArg1, gender) switch
            {
                // Let's Go starter forms with partner ribbon
                { context: EntityContext.Gen7b } and ({ species: (ushort)Species.Pikachu, form: PikachuStarterForm }
                    or { species: (ushort)Species.Eevee, form: EeveeStarterForm }) => $"{species}-{form}p",
                // Frillish and Jellicent have gender differences
                {
                        species: (ushort)Species.Frillish or (ushort)Species.Jellicent, gender: (byte)Gender.Female
                    } => $"{species}f",
                // Alcremie has form and decoration variations
                { species: (ushort)Species.Alcremie } => $"{species}-{form}-{formArg1}",
                // Handle Totem forms by mapping to base form
                { form: > 0 } when FormInfo.HasTotemForm(species) && FormInfo.IsTotemForm(species, form) =>
                    $"{species}-{FormInfo.GetTotemBaseForm(species, form)}",
                // Species with forms that should use base sprite
                { form: > 0 } => species switch
                {
                    (ushort)Species.Rockruff => species.ToString(),
                    (ushort)Species.Sinistea or (ushort)Species.Polteageist => species.ToString(),
                    (ushort)Species.Scatterbug or (ushort)Species.Spewpa => species.ToString(),
                    (ushort)Species.Urshifu => species.ToString(),
                    (ushort)Species.Dudunsparce => species.ToString(),
                    _ => $"{species}-{form}"
                },
                // Valid species with form 0
                { species: var speciesId } when speciesId.IsValidSpecies() =>
                    species.ToString(),
                // Fallback for invalid species
                _ => "unknown"
            })
            .Append(".png")
            .ToString();

    /// <summary>
    /// Gets the sprite filename for a Poké Ball.
    /// </summary>
    /// <param name="ball">The ball ID.</param>
    public static string GetBallSpriteFilename(int ball) =>
        $"{SpritesRoot}b/_ball{ball}.png";

    /// <summary>
    /// Gets the sprite filename for an item, selecting appropriate size/style based on generation.
    /// </summary>
    public static string GetItemSpriteFilename(int item, EntityContext context) => context switch
    {
        EntityContext.Gen1 or EntityContext.Gen2 => ItemFallbackImageFileName, // TODO: Fix Gen I and II item sprites
        EntityContext.Gen3 => ItemFallbackImageFileName, // TODO: Fix Gen III item sprites
        EntityContext.Gen9 or EntityContext.Gen9a => GetArtworkItemSpriteFilename(item, context),
        _ => GetBigItemSpriteFilename(item, context)
    };

    /// <summary>Gets the big item sprite filename (used for Gen 4-8).</summary>
    private static string GetBigItemSpriteFilename(int item, EntityContext context) =>
        $"{SpritesRoot}bi/bitem_{GetItemIdString(item, context)}.png";

    /// <summary>Gets the artwork item sprite filename (used for Gen 9).</summary>
    private static string GetArtworkItemSpriteFilename(int item, EntityContext context) =>
        $"{SpritesRoot}ai/aitem_{GetItemIdString(item, context)}.png";

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

    /// <summary>
    /// Gets the sprite filename for a move category icon (Physical/Special/Status).
    /// </summary>
    /// <remarks>TODO: Not yet implemented.</remarks>
    // ReSharper disable once UnusedParameter.Global
    public static string GetMoveCategorySpriteFileName(int categoryId) =>
        string.Empty;

    /// <summary>
    /// Gets the CSS class to apply to a Pokémon slot based on whether it contains a valid Pokémon.
    /// </summary>
    public static string GetSpriteCssClass(PKM? pkm) => (pkm?.Species).IsValidSpecies()
        ? " slot-fill"
        : string.Empty;


    /// <summary>Determines if an item is a mail item based on its ID and context.</summary>
    private static bool IsItemMail(int item, EntityContext context) => context switch
    {
        EntityContext.Gen2 when Gen2MailIds.Contains(item) => true,
        EntityContext.Gen3 when Gen3MailIds.Contains(item) => true,
        EntityContext.Gen4 or EntityContext.Gen5 when Gen45MailIds.Contains(item) => true,
        _ => false
    };

    /// <summary>
    /// Converts an item ID to its string representation for sprite filenames.
    /// Handles lumped items (TMs, TRs) and mail items specially.
    /// </summary>
    private static string GetItemIdString(int item, EntityContext context) =>
        HeldItemLumpUtil.GetIsLump(item, context) switch
        {
            HeldItemLumpImage.TechnicalMachine => "tm",
            HeldItemLumpImage.TechnicalRecord => "tr",
            _ => IsItemMail(item, context)
                ? "unk"
                : item.ToString()
        };
}
