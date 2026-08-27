using System.Text.RegularExpressions;

namespace Pkmds.Core.Utilities;

/// <summary>
/// Classifies the most likely origin of a loaded save — emulator, Virtual Console cart dump,
/// Manic EMU archive, etc. The output is a short human-readable label intended for bug-report
/// diagnostics, so triagers can tell at a glance whether a Gen 1 save came from a physical
/// cartridge or a VC dump (which have very different legality rules) without having to guess
/// from the filename.
/// </summary>
public static partial class SaveSourceDetector
{
    [GeneratedRegex(@"^sav\d+\.dat$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex VirtualConsoleFileName();

    /// <summary>
    /// Returns a concise label for the save's origin. Decision order is most-specific first:
    /// explicit Manic EMU archive context → known filename conventions (DeSmuME .dsv,
    /// <c>sav*.dat</c> VC dumps) → SAV-type-specific platform/VC signals
    /// (<see cref="SAV7b" />, <see cref="SAV1.IsVirtualConsole" />) → generation-based fallback.
    /// Returns <c>"Unknown"</c> if no signal matches.
    /// </summary>
    public static string Detect(SaveFile saveFile, string? fileName, bool isManicEmuArchive)
    {
        if (isManicEmuArchive)
        {
            return "Manic EMU .3ds.sav archive";
        }

        var leafName = string.IsNullOrWhiteSpace(fileName) ? string.Empty : Path.GetFileName(fileName);
        var ext = Path.GetExtension(leafName);

        // DeSmuME / melonDS save with trailer — distinctive enough to call out by extension alone.
        if (string.Equals(ext, ".dsv", StringComparison.OrdinalIgnoreCase))
        {
            return "DeSmuME / melonDS (.dsv)";
        }

        // NO$GBA bundles its own cart and battery saves under .sav; no unique marker at our layer,
        // so we fall through to generation-based detection below.

        // Manic EMU ".3ds.sav" that didn't carry through a ManicEmuSaveContext (archive detection
        // failed for some reason — malformed sdmc/ layout etc.). Still useful to flag.
        if (leafName.EndsWith(".3ds.sav", StringComparison.OrdinalIgnoreCase) ||
            leafName.EndsWith(".3ds.save", StringComparison.OrdinalIgnoreCase))
        {
            return "Manic EMU-style .3ds.sav (archive detection bypassed)";
        }

        // VC Gen 1/2 dumps conventionally carry the "sav<N>.dat" name PKHeX itself uses to gate
        // IsVirtualConsole. Match the regex directly so we can report it even if PKHeX's
        // IsVirtualConsole read is false for any reason.
        if (VirtualConsoleFileName().IsMatch(leafName))
        {
            return "Virtual Console (sav*.dat)";
        }

        // SAV-type specific detection (LGPE platform and IsVirtualConsole on Gen 1/2).
        var sourceFromType = saveFile switch
        {
            SAV7b => "Switch save (raw)",
            SAV1 { IsVirtualConsole: true } => "Gen 1 Virtual Console",
            SAV1 => "Gen 1 physical cartridge",
            SAV2 { IsVirtualConsole: true } => "Gen 2 Virtual Console",
            SAV2 => "Gen 2 physical cartridge",
            _ => null,
        };

        if (sourceFromType is not null)
        {
            return sourceFromType;
        }

        // Generation fallback — not definitive about source but at least narrows the platform.
        return saveFile.Generation switch
        {
            3 => "GBA save (raw or emulator)",
            4 or 5 => "DS save (raw or emulator)",
            6 or 7 => "3DS save (bare, likely JKSM / Checkpoint / emulator dump)",
            8 or 9 => "Switch save (raw)",
            _ => "Unknown",
        };
    }
}
