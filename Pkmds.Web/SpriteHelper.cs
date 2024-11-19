namespace Pkmds.Web;

public static class SpriteHelper
{
    private const string SpritesRoot = "sprites/";
    private const int PikachuStarterForm = 8;
    private const int EeveeStarterForm = 1;

    public const string ItemFallbackImageFileName = $"{SpritesRoot}bi/bitem_unk.png";
    public const string PokemonFallbackImageFileName = $"{SpritesRoot}a/a_unknown.png";

    public static string GetPokemonSpriteFilename(PKM? pokemon) =>
        new StringBuilder($"{SpritesRoot}a/a_")
        .Append(pokemon switch
        {
            null => "unknown",
            { Context: EntityContext.Gen7b } and ({ Species: (ushort)Species.Pikachu, Form: PikachuStarterForm }
                or { Species: (ushort)Species.Eevee, Form: EeveeStarterForm }) => $"{pokemon.Species}-{pokemon.Form}p",
            { Species: (ushort)Species.Manaphy, IsEgg: true } => "490-e",
            { IsEgg: true } => "egg",
            { Species: (ushort)Species.Frillish or (ushort)Species.Jellicent, Gender: (byte)Gender.Female } => $"{pokemon.Species}f",
            { Species: (ushort)Species.Alcremie } => $"{pokemon.Species}-{pokemon.Form}-{pokemon.GetFormArgument(0)}",
            { Form: var form, Species: var species } when form > 0 && FormInfo.HasTotemForm(species) && FormInfo.IsTotemForm(species, form) => $"{species}-{FormInfo.GetTotemBaseForm(species, form)}",
            { Form: > 0 } => pokemon.Species switch
            {
                (ushort)Species.Scatterbug or (ushort)Species.Spewpa => pokemon.Species.ToString(),
                (ushort)Species.Urshifu => pokemon.Species.ToString(),
                _ => $"{pokemon.Species}-{pokemon.Form}",
            },
            { Species: > (ushort)Species.None and < (ushort)Species.MAX_COUNT } =>
                pokemon.Species.ToString(),
            _ => "unknown",
        })
        .Append(".png")
        .ToString();

    public static string GetBallSpriteFilename(int ball) =>
        $"{SpritesRoot}b/_ball{ball}.png";

    public static string GetItemSpriteFilename(int item, EntityContext context) => context switch
    {
        EntityContext.Gen1 or EntityContext.Gen2 => ItemFallbackImageFileName, // TODO: Fix Gen I and II item sprites
        EntityContext.Gen3 => ItemFallbackImageFileName, // TODO: Fix Gen III item sprites
        EntityContext.Gen9 => GetArtworkItemSpriteFilename(item, context),
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

    // TODO: Implement
    public static string GetMoveCategorySpriteFileName(int categoryId) =>
        string.Empty;

    public static string GetSpriteCssClass(PKM? pkm) =>
        $"d-flex align-items-center justify-center {(pkm is { Species: > (ushort)Species.None } ? "slot-fill" : string.Empty)}";

    private static readonly int[] Gen2MailIds = [0x9E, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD];
    private static readonly int[] Gen3MailIds = [121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132];
    private static readonly int[] Gen45MailIds = [137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148];

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
            _ => IsItemMail(item, context) ? "unk" : item.ToString(),
        };
}
