using PKHeX.Core;
using Pkmds.Core.Utilities;

namespace Pkmds.Preview;

/// <summary>
/// Picks the single most representative bundled sprite (a relative path from
/// <see cref="SpritePaths" />) for a file — the thumbnail counterpart to
/// <see cref="HtmlRenderer.RenderFile" />. Shared by the Windows/macOS/iOS thumbnail providers.
/// </summary>
public static class FileSprite
{
    /// <summary>
    /// Returns the relative bundled-sprite path that best represents <paramref name="data" />:
    /// an entity's species, a save's lead party Pokémon, or a gift's species/item — falling back
    /// to the placeholder sprite when nothing is recognized.
    /// </summary>
    /// <param name="data">Raw file bytes.</param>
    /// <param name="fileExtension">Extension, with or without a leading dot.</param>
    public static string GetRelativeSpritePath(byte[] data, string fileExtension)
    {
        var ext = fileExtension.Length > 0 && fileExtension[0] != '.'
            ? "." + fileExtension
            : fileExtension;

        // WonderCard3 isn't a DataMysteryGift and doesn't expose the gifted species/item, but it
        // does expose Type (Pokémon / Item / Link) — pick the matching placeholder.
        if (ext.Equals(".wc3", StringComparison.OrdinalIgnoreCase))
            return GetWonderCard3Fallback(data);

        if (SaveUtil.TryGetSaveFile(data, out var sav))
            return GetSaveSprite(sav);
        if (MysteryGift.GetMysteryGift(data, ext.AsSpan()) is { } gift)
            return SpritePaths.GetMysteryGiftSprite(gift);
        if (EntityFormat.GetFromBytes(data) is { } pkm)
            return SpritePaths.GetPokemonSprite(pkm);
        return SpritePaths.PokemonFallbackFile;
    }

    // WonderCard3 (Gen 3) only exposes Type (0 = Pokémon, 1 = Item, 2 = Link Stats), not the
    // gifted content — so return the Pokémon or item placeholder accordingly.
    private static string GetWonderCard3Fallback(byte[] data)
    {
        const int extrasSize = WonderCard3Extra.SIZE * 2;
        const int eventSize = MysteryEvent3.SIZE;
        var cardSize = data.Length switch
        {
            WonderCard3.SIZE_JAP => WonderCard3.SIZE_JAP,
            WonderCard3.SIZE_JAP + extrasSize => WonderCard3.SIZE_JAP,
            WonderCard3.SIZE_JAP + extrasSize + eventSize => WonderCard3.SIZE_JAP,
            WonderCard3.SIZE => WonderCard3.SIZE,
            WonderCard3.SIZE + extrasSize => WonderCard3.SIZE,
            WonderCard3.SIZE + extrasSize + eventSize => WonderCard3.SIZE,
            _ => -1,
        };
        if (cardSize < 0)
            return SpritePaths.PokemonFallbackFile;
        try
        {
            var card = new WonderCard3(new Memory<byte>(data, 0, cardSize));
            return card.Type == 1 ? SpritePaths.ItemFallbackFile : SpritePaths.PokemonFallbackFile;
        }
        catch
        {
            return SpritePaths.PokemonFallbackFile;
        }
    }

    // Lead party Pokémon → first non-empty slot in the first box → placeholder.
    private static string GetSaveSprite(SaveFile sav)
    {
        if (sav.HasParty && sav.PartyCount > 0)
            return SpritePaths.GetPokemonSprite(sav.GetPartySlotAtIndex(0));

        if (sav.BoxCount > 0)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                var pk = sav.GetBoxSlotAtIndex(0, slot);
                if (pk.Species != 0)
                    return SpritePaths.GetPokemonSprite(pk);
            }
        }
        return SpritePaths.PokemonFallbackFile;
    }
}
