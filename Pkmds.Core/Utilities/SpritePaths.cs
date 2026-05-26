using System.Text;
using Pkmds.Core.Extensions;

namespace Pkmds.Core.Utilities;

/// <summary>
/// Builds <em>relative</em> paths into the bundled PKHeX sprite set (e.g. <c>a/a_448.png</c>,
/// <c>bi/bitem_55.png</c>) for a Pokémon, item, or mystery gift. AOT-clean and UI-independent, so
/// it can be shared by the Blazor app (<see cref="Pkmds.Core" />-external <c>ImageHelper</c> prepends
/// the <c>_content/Pkmds.Rcl/sprites/</c> web root) and the offline file-preview/thumbnail providers
/// (which resolve the same relative path against their own bundled copy of the sprites).
/// </summary>
public static class SpritePaths
{
    private const int PikachuStarterForm = 8;
    private const int EeveeStarterForm = 1;

    /// <summary>Relative fallback sprite for unknown items.</summary>
    public const string ItemFallbackFile = "bi/bitem_unk.png";

    /// <summary>Relative fallback sprite for unknown Pokémon.</summary>
    public const string PokemonFallbackFile = "a/a_unknown.png";

    // Mail item IDs for different generations.
    private static readonly int[] Gen2MailIds = [0x9E, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD];
    private static readonly int[] Gen3MailIds = [121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132];
    private static readonly int[] Gen45MailIds = [137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148];

    /// <summary>Relative sprite for a mystery gift — the item sprite for item gifts, else the species sprite.</summary>
    public static string GetMysteryGiftSprite(MysteryGift gift) => gift.IsItem
        ? GetItemSprite(gift.ItemID, gift.Context)
        : GetPokemonSprite(gift.Species, gift.Context, gift.IsEgg, gift.Form, 0, gift.Gender);

    /// <summary>Relative sprite for a Pokémon (handles forms, genders, and special cases).</summary>
    public static string GetPokemonSprite(PKM? pokemon) => pokemon is null
        ? PokemonFallbackFile
        : GetPokemonSprite(pokemon.Species, pokemon.Context, pokemon.IsEgg, pokemon.Form,
            pokemon.GetFormArgument(0), pokemon.Gender);

    /// <summary>
    /// Relative sprite for a specific species form (form-picker UIs where no full PKM exists).
    /// Does not handle eggs, gender differences, or totem forms.
    /// </summary>
    public static string GetPokemonSpriteForForm(ushort species, EntityContext context, byte form, uint? formArg = null) =>
        GetPokemonSprite(species, context, isEgg: false, form, formArg, gender: 0);

    private static string GetPokemonSprite(ushort species, EntityContext context, bool isEgg, byte form,
        uint? formArg1, byte gender) =>
        new StringBuilder("a/a_")
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

    /// <summary>Relative item sprite, selecting size/style by generation.</summary>
    public static string GetItemSprite(int item, EntityContext context) => context switch
    {
        EntityContext.Gen1 or EntityContext.Gen2 => ItemFallbackFile, // TODO: Gen I/II item sprites
        EntityContext.Gen3 => ItemFallbackFile,                       // TODO: Gen III item sprites
        EntityContext.Gen9 or EntityContext.Gen9a => $"ai/aitem_{GetItemIdString(item, context)}.png",
        _ => $"bi/bitem_{GetItemIdString(item, context)}.png"
    };

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
