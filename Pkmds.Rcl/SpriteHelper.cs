using Pkmds.Rcl.Extensions;

namespace Pkmds.Rcl;

public static class SpriteHelper
{
    private const string SpritesRoot = "_content/Pkmds.Rcl/sprites/";
    private const int PikachuStarterForm = 8;
    private const int EeveeStarterForm = 1;

    public const string ItemFallbackImageFileName = $"{SpritesRoot}bi/bitem_unk.png";
    public const string PokemonFallbackImageFileName = $"{SpritesRoot}a/a_unknown.png";

    private static readonly int[] Gen2MailIds = [0x9E, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD];
    private static readonly int[] Gen3MailIds = [121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132];
    private static readonly int[] Gen45MailIds = [137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148];

    public static string GetMysteryGiftSpriteFileName(MysteryGift gift) => gift.IsItem
        ? GetItemSpriteFilename(gift.ItemID, gift.Context)
        : GetPokemonSpriteFilename(gift.Species, gift.Context, gift.IsEgg, gift.Form, 0, gift.Gender);

    public static string GetPokemonSpriteFilename(PKM? pokemon) => pokemon is null
        ? PokemonFallbackImageFileName
        : GetPokemonSpriteFilename(pokemon.Species, pokemon.Context, pokemon.IsEgg, pokemon.Form,
            pokemon.GetFormArgument(0), pokemon.Gender);

    private static string GetPokemonSpriteFilename(ushort species, EntityContext context, bool isEgg, byte form,
        uint? formArg1, byte gender) =>
        new StringBuilder($"{SpritesRoot}a/a_")
            .Append((species, context, isEgg, form, formArg1, gender) switch
            {
                { context: EntityContext.Gen7b } and ({ species: (ushort)Species.Pikachu, form: PikachuStarterForm }
                    or { species: (ushort)Species.Eevee, form: EeveeStarterForm }) => $"{species}-{form}p",
                { species: (ushort)Species.Manaphy, isEgg: true } => "490-e",
                { isEgg: true } => "egg",
                {
                        species: (ushort)Species.Frillish or (ushort)Species.Jellicent, gender: (byte)Gender.Female
                    } => $"{species}f",
                { species: (ushort)Species.Alcremie } => $"{species}-{form}-{formArg1}",
                { form: > 0 } when FormInfo.HasTotemForm(species) && FormInfo.IsTotemForm(species, form) =>
                    $"{species}-{FormInfo.GetTotemBaseForm(species, form)}",
                { form: > 0 } => species switch
                {
                    (ushort)Species.Rockruff => species.ToString(),
                    (ushort)Species.Sinistea or (ushort)Species.Polteageist => species.ToString(),
                    (ushort)Species.Scatterbug or (ushort)Species.Spewpa => species.ToString(),
                    (ushort)Species.Urshifu => species.ToString(),
                    (ushort)Species.Dudunsparce => species.ToString(),
                    _ => $"{species}-{form}"
                },
                { species: var speciesId } when speciesId.IsValidSpecies() =>
                    species.ToString(),
                _ => "unknown"
            })
            .Append(".png")
            .ToString();

    public static string GetBallSpriteFilename(int ball) =>
        $"{SpritesRoot}b/_ball{ball}.png";

    public static string GetItemSpriteFilename(int item, EntityContext context) => context switch
    {
        EntityContext.Gen1 or EntityContext.Gen2 => ItemFallbackImageFileName, // TODO: Fix Gen I and II item sprites
        EntityContext.Gen3 => ItemFallbackImageFileName, // TODO: Fix Gen III item sprites
        EntityContext.Gen9 or EntityContext.Gen9a => GetArtworkItemSpriteFilename(item, context),
        _ => GetBigItemSpriteFilename(item, context)
    };

    private static string GetBigItemSpriteFilename(int item, EntityContext context) =>
        $"{SpritesRoot}bi/bitem_{GetItemIdString(item, context)}.png";

    private static string GetArtworkItemSpriteFilename(int item, EntityContext context) =>
        $"{SpritesRoot}ai/aitem_{GetItemIdString(item, context)}.png";

    public static string GetTypeGemSpriteFileName(byte type) =>
        $"{SpritesRoot}t/g/gem_{type:00}.png";

    public static string GetTypeSquareSpriteFileName(byte type) =>
        $"{SpritesRoot}t/s/type_icon_{type:00}.png";

    public static string GetTypeSquareSpriteFileName(int type) =>
        $"{SpritesRoot}t/s/type_icon_{type:00}.png";

    public static string GetTypeWideSpriteFileName(byte type) =>
        $"{SpritesRoot}t/w/type_wide_{type:00}.png";

    public static string GetBagPouchSpriteFileName(InventoryType type) =>
        $"{SpritesRoot}bag/Bag_{GetBagPouchSpriteName(type)}.png";

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

    // TODO: Implement
    // ReSharper disable once UnusedParameter.Global
    public static string GetMoveCategorySpriteFileName(int categoryId) =>
        string.Empty;

    public static string GetSpriteCssClass(PKM? pkm) => (pkm?.Species).IsValidSpecies()
        ? " slot-fill"
        : string.Empty;

    private static bool IsItemMail(int item, EntityContext context) => context switch
    {
        EntityContext.Gen2 when Gen2MailIds.Contains(item) => true,
        EntityContext.Gen3 when Gen3MailIds.Contains(item) => true,
        EntityContext.Gen4 or EntityContext.Gen5 when Gen45MailIds.Contains(item) => true,
        _ => false
    };

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
