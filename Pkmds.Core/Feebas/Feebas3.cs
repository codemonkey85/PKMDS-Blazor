// Adapted from PKHeXFeebasLocatorPlugin (https://github.com/Bl4ckSh4rk/PKHeXFeebasLocatorPlugin)
// Copyright (c) Bl4ckSh4rk and contributors. Licensed under GPL-3.0.
// Ported to Blazor for PKMDS-Blazor by codemonkey85, 2026.

namespace Pkmds.Core.Feebas;

/// <summary>
/// Gen 3 (Ruby/Sapphire/Emerald) Route 119 Feebas tile RNG.
/// Tile indices are 1..447 with a fixed exclusion list — see <see cref="IsAccessible"/>.
/// </summary>
public static class Feebas3
{
    /// <summary>Number of Feebas tiles seeded each cycle.</summary>
    public const int TileCount = 6;

    /// <summary>
    /// Decodes the six Feebas tile indices from the seed by stepping the standard Gen 3 LCG and
    /// taking each step's high word modulo 447 (the count of fishable river tiles on Route 119).
    /// Tile 0 is folded back to 447, and tiles 1..3 are skipped because the river starts at index 4.
    /// </summary>
    public static ushort[] GetTiles(uint seed)
    {
        short i = 0;
        ushort[] tiles = [0, 0, 0, 0, 0, 0];
        ushort tile;

        while (i < tiles.Length)
        {
            seed = 0x41C64E6D * seed + 0x3039;
            tile = (ushort)((seed >> 16) % 0x1BF);

            if (tile == 0)
            {
                tile = 447;
            }

            if (tile >= 4)
            {
                tiles[i] = tile;
                i++;
            }
        }
        return tiles;
    }

    /// <summary>
    /// Whether a Feebas tile index can actually be reached by the player. The excluded indices are
    /// either out-of-river bounds or under-the-bridge tiles that the player cannot fish from
    /// (the bridge tiles are still drawn — see <see cref="IsUnderBridge"/>).
    /// </summary>
    public static bool IsAccessible(ushort tile) => tile is not (< 4 or > 447 or 105 or 119 or 132 or 144 or 296 or 297 or 298);

    /// <summary>
    /// Tile 132 is the under-the-bridge tile; when present in the seed result, ten visible river
    /// cells are highlighted instead of one (the bridge spans them).
    /// </summary>
    public static bool IsUnderBridge(ushort tile) => tile is 132;
}
