// Adapted from PKHeXFeebasLocatorPlugin (https://github.com/Bl4ckSh4rk/PKHeXFeebasLocatorPlugin)
// Copyright (c) Bl4ckSh4rk and contributors. Licensed under GPL-3.0.
// Ported to Blazor for PKMDS-Blazor by codemonkey85, 2026.

namespace Pkmds.Core.Feebas;

/// <summary>
/// Reads and writes the Feebas tile RNG seed across the games that gate Feebas behind a tile rotation:
/// Ruby/Sapphire, Emerald, Diamond/Pearl/Platinum, and Brilliant Diamond / Shining Pearl.
/// </summary>
public static class FeebasSeedAccessor
{
    private const int SeedOffsetRubySapphire = 0x2DD6;
    private const int SeedOffsetEmerald = 0x2E6A;
    private const int SeedOffsetDiamondPearl = 0x53C8;
    private const int SeedOffsetPlatinum = 0x5664;
    private const int SeedWorkBrilliantShining = 436;

    /// <summary>
    /// True when the Feebas tile mechanic applies to the given save file. Notably FR/LG and HGSS
    /// are excluded — Feebas is not gated by the tile RNG in those games.
    /// </summary>
    public static bool IsSupported(SaveFile sav) => sav is SAV3RS or SAV3E or SAV4Sinnoh or SAV8BS;

    /// <summary>Reads the current Feebas seed from the save file, or null if unsupported.</summary>
    public static uint? TryReadSeed(SaveFile sav) => sav switch
    {
        SAV3RS s3rs => BitConverter.ToUInt16(s3rs.Large[SeedOffsetRubySapphire..]),
        SAV3E s3e => BitConverter.ToUInt16(s3e.Large[SeedOffsetEmerald..]),
        SAV4DP s4dp => BitConverter.ToUInt32(s4dp.General[SeedOffsetDiamondPearl..]),
        SAV4Pt s4pt => BitConverter.ToUInt32(s4pt.General[SeedOffsetPlatinum..]),
        SAV8BS s8 => (uint)s8.GetWork(SeedWorkBrilliantShining),
        _ => null,
    };

    /// <summary>
    /// Writes a new Feebas seed into the save and marks it edited. Returns false if the save
    /// type is unsupported.
    /// </summary>
    public static bool TryWriteSeed(SaveFile sav, uint seed)
    {
        switch (sav)
        {
            case SAV3RS s3rs:
                BitConverter.GetBytes((ushort)seed).CopyTo(s3rs.Large[SeedOffsetRubySapphire..]);
                break;
            case SAV3E s3e:
                BitConverter.GetBytes((ushort)seed).CopyTo(s3e.Large[SeedOffsetEmerald..]);
                break;
            case SAV4DP s4dp:
                BitConverter.GetBytes(seed).CopyTo(s4dp.General[SeedOffsetDiamondPearl..]);
                break;
            case SAV4Pt s4pt:
                BitConverter.GetBytes(seed).CopyTo(s4pt.General[SeedOffsetPlatinum..]);
                break;
            case SAV8BS s8:
                s8.SetWork(SeedWorkBrilliantShining, (int)seed);
                break;
            default:
                return false;
        }
        sav.State.Edited = true;
        return true;
    }

    /// <summary>
    /// Computes the Feebas tiles for the current save state. Returns null when the save is
    /// unsupported or its seed cannot be read.
    /// </summary>
    public static ushort[]? GetTiles(SaveFile sav)
    {
        var seed = TryReadSeed(sav);
        if (seed is null)
        {
            return null;
        }

        return sav.Generation == 3
            ? Feebas3.GetTiles(seed.Value)
            : Feebas4.GetTiles(seed.Value);
    }
}
