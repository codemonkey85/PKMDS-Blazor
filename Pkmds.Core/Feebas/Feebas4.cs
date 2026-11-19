// Adapted from PKHeXFeebasLocatorPlugin (https://github.com/Bl4ckSh4rk/PKHeXFeebasLocatorPlugin)
// Copyright (c) Bl4ckSh4rk and contributors. Licensed under GPL-3.0.
// Ported to Blazor for PKMDS-Blazor by codemonkey85, 2026.

namespace Pkmds.Core.Feebas;

/// <summary>
/// Gen 4 / BDSP Mt. Coronet B1F Feebas tile RNG. The 32-bit seed is chunked into four bytes,
/// each mapped onto its own quadrant of the 4 × 132-tile fishing grid (132 × 4 = 528 total tiles).
/// </summary>
public static class Feebas4
{
    /// <summary>Number of Feebas tiles seeded each cycle.</summary>
    public const int TileCount = 4;

    /// <summary>
    /// Decodes the four Feebas tile indices. Each byte of the seed picks one tile within its
    /// 132-tile quadrant (<c>0x84 == 132</c>), then the quadrant offset is added back in.
    /// </summary>
    public static ushort[] GetTiles(uint seed)
    {
        ushort[] tiles = [0, 0, 0, 0];

        for (short i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (ushort)((seed >> 24 - 8 * i & 0xFF) % 0x84 + 0x84 * i);
        }
        return tiles;
    }

    /// <summary>Whether a Feebas tile index is within the Mt. Coronet B1F fishing grid.</summary>
    public static bool IsAccessible(ushort tile) => tile is not (< 0 or > 528);
}
