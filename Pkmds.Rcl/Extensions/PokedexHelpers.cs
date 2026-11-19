namespace Pkmds.Rcl.Extensions;

/// <summary>
/// Shared Pokédex filtering helpers used by both PokedexTab and PokedexSpeciesGrid.
/// Centralised here to prevent the two callers drifting out of sync.
/// </summary>
internal static class PokedexHelpers
{
    /// <summary>
    /// Returns true when <paramref name="species" /> belongs to a regional dex that is
    /// available in the save's revision.
    /// <list type="bullet">
    /// <item>Rev 0 (base game)  → Paldea only.</item>
    /// <item>Rev 1 (Teal Mask) → Paldea + Kitakami.</item>
    /// <item>Rev 2+ (Indigo Disk) → Paldea + Kitakami + Blueberry.</item>
    /// </list>
    /// Iterates every form because some species (e.g. regional variants) have a
    /// regional dex number on a non-zero form only.
    /// </summary>
    internal static bool IsSpeciesInSvDex(SAV9SV sv, ushort species)
    {
        var fc = sv.Personal.GetFormEntry(species, 0).FormCount;
        for (byte f = 0; f < fc; f++)
        {
            var pi = sv.Personal.GetFormEntry(species, f);
            if (pi.DexPaldea != 0)
            {
                return true;
            }

            switch (sv.SaveRevision)
            {
                case >= 1 when pi.DexKitakami != 0:
                case >= 2 when pi.DexBlueberry != 0:
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the ordered list of regional dex definitions (names) that apply to
    /// <paramref name="saveFile" />.  The order matches the values returned by
    /// <see cref="GetRegionalIds" /> so callers can zip them together.
    /// </summary>
    internal static IReadOnlyList<RegionalDexDefinition> GetRegionalDexDefinitions(SaveFile saveFile) =>
        saveFile switch
        {
            SAV8BS => [new("Sinnoh")],
            SAV8SWSH swsh => swsh.SaveRevision switch
            {
                >= 2 => [new("Galar"), new("Isle of Armor"), new("Crown Tundra")],
                >= 1 => [new("Galar"), new("Isle of Armor")],
                _ => [new("Galar")]
            },
            SAV8LA => [new("Hisui")],
            SAV9SV sv => sv.SaveRevision switch
            {
                >= 2 => [new("Paldea"), new("Kitakami"), new("Blueberry")],
                >= 1 => [new("Paldea"), new("Kitakami")],
                _ => [new("Paldea")]
            },
            // ZA: Lumiose (DexIndex 1–232) and Hyperspace (DexIndex 233+).
            SAV9ZA => [new("Lumiose"), new("Hyperspace")],
            _ => []
        };

    /// <summary>
    /// Returns one regional dex number per entry in
    /// <see cref="GetRegionalDexDefinitions" />, in the same order.
    /// A value of 0 means the species is not in that sub-dex.
    /// </summary>
    internal static IReadOnlyList<ushort> GetRegionalIds(SaveFile saveFile, ushort species) =>
        saveFile switch
        {
            SAV8BS bdsp => [GetBdspRegionalId(bdsp, species)],
            SAV8SWSH swsh => GetSwshRegionalIds(swsh, species),
            SAV8LA => [PokedexSave8a.GetDexIndex(PokedexType8a.Hisui, species)],
            SAV9SV sv => GetSvRegionalIds(sv, species),
            SAV9ZA za => GetZaRegionalIds(za, species),
            _ => []
        };

    private static ushort GetBdspRegionalId(SAV8BS bdsp, ushort species) =>
        bdsp.Personal.GetFormEntry(species, 0) is { } pi
            ? pi.PokeDexIndex
            : (ushort)0;

    private static IReadOnlyList<ushort> GetSwshRegionalIds(SAV8SWSH swsh, ushort species)
    {
        var pi = swsh.Personal.GetFormEntry(species, 0);
        var galar = pi.PokeDexIndex;
        var armor = pi.ArmorDexIndex;
        var crown = pi.CrownDexIndex;

        return swsh.SaveRevision switch
        {
            >= 2 => [galar, armor, crown],
            >= 1 => [galar, armor],
            _ => [galar]
        };
    }

    private static IReadOnlyList<ushort> GetSvRegionalIds(SAV9SV sv, ushort species)
    {
        ushort paldea = 0, kitakami = 0, blueberry = 0;
        var fc = sv.Personal.GetFormEntry(species, 0).FormCount;
        for (byte f = 0; f < fc; f++)
        {
            if (sv.Personal.GetFormEntry(species, f) is not { } pi)
            {
                continue;
            }

            if (paldea == 0 && pi.DexPaldea != 0)
            {
                paldea = pi.DexPaldea;
            }

            if (kitakami == 0 && pi.DexKitakami != 0)
            {
                kitakami = pi.DexKitakami;
            }

            if (blueberry == 0 && pi.DexBlueberry != 0)
            {
                blueberry = pi.DexBlueberry;
            }
        }

        return sv.SaveRevision switch
        {
            >= 2 => [paldea, kitakami, blueberry],
            >= 1 => [paldea, kitakami],
            _ => [paldea]
        };
    }

    private static IReadOnlyList<ushort> GetZaRegionalIds(SAV9ZA za, ushort species)
    {
        if (za.Personal.GetFormEntry(species, 0) is not { } pi)
        {
            return [0, 0];
        }

        // DexIndex 1–232 = Lumiose dex; 233+ = Hyperspace dex.
        // Show each as 1-based within its own section.
        var lumiose = pi.IsLumioseNative
            ? pi.DexIndex
            : (ushort)0;

        var hyperspace = pi.IsHyperspaceNative
            ? (ushort)(pi.DexIndex - 232)
            : (ushort)0;

        return [lumiose, hyperspace];
    }

    /// <summary>
    /// Returns true when <paramref name="species" /> has a dex entry in the given save file.
    /// Mirrors the per-game enumeration logic used by both PokedexTab and PokedexSpeciesGrid
    /// so seen/caught counts and the species grid always represent the same set.
    /// </summary>
    internal static bool IsSpeciesInDex(SaveFile saveFile, ushort species) => saveFile switch
    {
        // LGPE: national dex limited to original 151 + Meltan (808) + Melmetal (809).
        SAV7b => species is >= 1 and <= 151 or 808 or 809,

        // SWSH: species must have a Galar / Armor / Crown regional dex index.
        SAV8SWSH swsh => swsh.Zukan.GetEntry(species, out _),

        // LA: only Hisui-native species are tracked.
        SAV8LA => PokedexSave8a.GetDexIndex(PokedexType8a.Hisui, species) != 0,

        // SV: only count dexes available in the save's revision.
        SAV9SV sv => IsSpeciesInSvDex(sv, species),

        // ZA: filters by the game's personal table (MaxSpeciesID varies by DLC revision).
        SAV9ZA za => za.Personal.IsSpeciesInGame(species),

        // All other games (Gen 1–7, BDSP): every national species up to MaxSpeciesID.
        _ => true
    };
}
