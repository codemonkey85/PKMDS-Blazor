using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Compression;

namespace Pkmds.Core.Utilities;

/// <summary>
/// Helpers for importing and exporting 3DS save files in the Manic EMU <c>.3ds.sav</c> ZIP format.
/// </summary>
/// <remarks>
/// Manic EMU exports 3DS saves as a ZIP archive. The canonical extension is
/// <c>GameTitle.3ds.sav</c> on every platform — the iOS build uses the same suffix as
/// desktop (see <c>ManicEmu/Sources/Tools/Cores/ThreeDS.swift</c> and
/// <c>ManicEmu/Sources/Tools/Others/ShareManager.swift</c> in the upstream repo).
/// We still *accept* <c>.3ds.save</c> (with a trailing <c>e</c>) defensively — users
/// sometimes rename manually, and iOS Safari has been observed mangling compound
/// extensions on blob-URL downloads — but PKMDS should never produce it when round-tripping
/// a <c>.3ds.sav</c> upload. Manic EMU's own importer matches <c>url.path.contains(".3ds.sav")</c>
/// as a substring check, so <c>.3ds.save</c> happens to re-import successfully too.
/// <para>
/// The ZIP contains the full <c>sdmc/</c> directory tree from Citra's virtual SD card,
/// e.g. <c>sdmc/Nintendo 3DS/…/title/00040000/00055d00/data/00000001/main</c>. The
/// PKHeX-compatible save bytes are stored as a single binary file entry inside that
/// structure. To round-trip a save through PKMDS:
/// </para>
/// <list type="number">
/// <item>User exports <c>.3ds.sav</c> from Manic EMU.</item>
/// <item>PKMDS detects the ZIP, finds the save entry, and loads it (see <see cref="SaveFileLoader" />).</item>
/// <item>User edits the save in PKMDS.</item>
/// <item>
/// PKMDS rebuilds the ZIP with the edited save bytes and offers it for download
/// using the same compound extension as the original so Manic EMU can import it directly.
/// </item>
/// </list>
/// <para>
/// Load-path ordering matters: PKHeX.Core ships its own <c>ZipReader</c> that recognises
/// any ZIP with a <c>main</c> or <c>SaveData.bin</c> entry inside and unwraps it invisibly.
/// If the Manic EMU ZIP detection runs <em>after</em> <c>SaveUtil.TryGetSaveFile</c>, PKHeX
/// swallows the archive, we never see it as a ZIP, <see cref="ManicEmuSaveContext" /> is never
/// set, and export silently produces raw bytes that Manic EMU can't re-import (see issue #750
/// for the regression caused by missing this ordering). Always run ZIP detection first.
/// </para>
/// <para>
/// Known-upstream caveat: early in PR #751's debugging, a sequence of broken exports
/// (pre-fix deflate-compressed 9 kB archives with the inner save replaced by the PKHeX
/// default) caused Manic EMU / Citra to reject every subsequent save as "corrupted"
/// in-game — even after switching back to byte-identical valid archives. Recovery required
/// deleting the Manic EMU app and re-importing the ROM. The present implementation
/// produces ZIPs byte-level compatible with ZIPFoundation's own output (store compression,
/// bit 11 set, <c>versionMadeBy = 0x0315</c>), so a fresh round-trip on a healthy Manic EMU
/// install is fine; the warning exists only for users who hit the issue with an older build
/// and may need the clean reinstall to clear the accumulated state.
/// </para>
/// </remarks>
public static class ManicEmuSaveHelper
{
    private const string SdmcPrefix = "sdmc/";

    // 3DS save files are at most a few MB; cap at 8 MB to guard against ZIP bombs.
    private const long MaxUncompressedEntrySize = 8 * 1024 * 1024;

    // A real Manic EMU ZIP contains only a handful of entries total; cap at 500
    // to prevent DoS from iterating a crafted archive with many non-sdmc/ entries.
    private const int MaxTotalEntries = 500;

    // A real Manic EMU ZIP contains only a handful of sdmc/ entries; cap at 100
    // to prevent DoS from a crafted ZIP with many qualifying entries.
    private const int MaxSdmcEntriesToInspect = 100;

    /// <summary>
    /// Determines whether <paramref name="data" /> looks like a ZIP archive.
    /// </summary>
    public static bool IsZip(ReadOnlySpan<byte> data) =>
        data.Length >= 4 &&
        data[0] == 0x50 && data[1] == 0x4B &&
        data[2] == 0x03 && data[3] == 0x04;

    /// <summary>
    /// Tries to find a PKHeX-loadable save file inside a Manic EMU <c>.3ds.sav</c> ZIP.
    /// </summary>
    /// <param name="zipBytes">Raw bytes of the <c>.3ds.sav</c> ZIP archive.</param>
    /// <param name="fileName">
    /// Original filename of the ZIP (forwarded to <see cref="SaveUtil.TryGetSaveFile" /> for
    /// format detection); may be <see langword="null" />.
    /// </param>
    /// <param name="saveFile">
    /// The parsed <see cref="SaveFile" /> instance, ready to use directly.
    /// </param>
    /// <param name="context">
    /// Metadata needed to rebuild the ZIP on export.  Pass this back to
    /// <see cref="RebuildZip" /> once the user has finished editing.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if a recognisable save was found inside the ZIP;
    /// <see langword="false" /> otherwise.
    /// </returns>
    public static bool TryExtractSaveFromZip(
        byte[] zipBytes,
        string? fileName,
        [NotNullWhen(true)] out SaveFile? saveFile,
        [NotNullWhen(true)] out ManicEmuSaveContext? context)
    {
        saveFile = null;
        context = null;

        if (!IsZip(zipBytes))
        {
            return false;
        }

        try
        {
            using var zipStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            if (archive.Entries.Count > MaxTotalEntries)
            {
                return false;
            }

            var inspected = 0;
            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith(SdmcPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (++inspected > MaxSdmcEntriesToInspect)
                {
                    break;
                }

                if (entry.Length is 0 or > MaxUncompressedEntrySize)
                {
                    continue;
                }

                // Copy with a hard byte limit to guard against ZIP bombs where
                // entry.Length metadata is falsified.
                using var entryStream = new MemoryStream((int)entry.Length);
                var tooLarge = false;
                using (var src = entry.Open())
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    while ((read = src.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        totalRead += read;
                        if (totalRead > MaxUncompressedEntrySize)
                        {
                            tooLarge = true;
                            break;
                        }

                        entryStream.Write(buffer, 0, read);
                    }
                }

                if (tooLarge)
                {
                    continue;
                }

                var entryBytes = entryStream.ToArray();

                if (!SaveFileLoader.TryLoadRawSave(entryBytes, fileName, out var sf))
                {
                    continue;
                }

                saveFile = sf;
                context = new ManicEmuSaveContext(zipBytes, entry.FullName);
                return true;
            }
        }
        catch (InvalidDataException)
        {
            // Malformed ZIP data — fall through.
        }
        catch (IOException)
        {
            // ZIP read error — fall through.
        }
        catch (NotSupportedException)
        {
            // Unsupported ZIP feature — fall through.
        }

        return false;
    }

    /// <summary>
    /// Rebuilds the original <c>.3ds.sav</c> ZIP archive, replacing the save file entry
    /// with <paramref name="newSaveBytes" />. Entries are written with
    /// <see cref="CompressionLevel.NoCompression" /> (store method) to match what Manic EMU's
    /// own <c>ShareManager.create3DSGameSave</c> produces via ZIPFoundation; the general-purpose
    /// bit 11 (UTF-8 path encoding) is also set on every entry in a post-write pass, again
    /// matching ZIPFoundation's writer (<c>Archive+Helpers.writeLocalFileHeader</c>). Without
    /// that flag ZIPFoundation on iOS has been observed rejecting the archive even though
    /// all paths are pure ASCII — see issue #751 follow-up.
    /// </summary>
    /// <param name="context">The context returned by <see cref="TryExtractSaveFromZip" />.</param>
    /// <param name="newSaveBytes">The edited save data to embed.</param>
    /// <returns>Raw bytes of the rebuilt <c>.3ds.sav</c> ZIP.</returns>
    public static byte[] RebuildZip(ManicEmuSaveContext context, byte[] newSaveBytes)
    {
        using var resultStream = new MemoryStream();

        using (var newArchive = new ZipArchive(resultStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var origStream = new MemoryStream(context.OriginalZipBytes);
            using var origArchive = new ZipArchive(origStream, ZipArchiveMode.Read);

            foreach (var entry in origArchive.Entries)
            {
                var newEntry = newArchive.CreateEntry(entry.FullName, CompressionLevel.NoCompression);
                newEntry.LastWriteTime = entry.LastWriteTime;

                using var dest = newEntry.Open();

                if (string.Equals(entry.FullName, context.SaveEntryPath, StringComparison.OrdinalIgnoreCase))
                {
                    dest.Write(newSaveBytes, 0, newSaveBytes.Length);
                }
                else
                {
                    // Guarded copy — same limit as TryExtractSaveFromZip to prevent OOM
                    // from a crafted ZIP whose non-save entries are unexpectedly large.
                    using var src = entry.Open();
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    while ((read = src.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        totalRead += read;
                        if (totalRead > MaxUncompressedEntrySize)
                        {
                            throw new InvalidDataException(
                                $"Non-save entry '{entry.FullName}' exceeds the {MaxUncompressedEntrySize / (1024 * 1024)} MB size limit.");
                        }

                        dest.Write(buffer, 0, read);
                    }
                }
            }
        }

        var bytes = resultStream.ToArray();
        NormalizeZipHeadersForZipFoundation(bytes);
        return bytes;
    }

    /// <summary>
    /// Rewrites a few header fields in the rebuilt ZIP so our output matches what ZIPFoundation
    /// (the library Manic EMU uses on iOS) would have written:
    /// <list type="bullet">
    ///   <item>Sets general-purpose bit 11 (UTF-8 path encoding) on every local and central
    ///   directory file header. .NET's <see cref="ZipArchive" /> only sets bit 11 when a path
    ///   contains non-ASCII characters, but ZIPFoundation sets it unconditionally (see
    ///   <c>Archive+Helpers.writeLocalFileHeader</c>).</item>
    ///   <item>Bumps <c>versionMadeBy</c> from the .NET default (2.0) to ZIPFoundation's
    ///   2.1 (<c>0x0315</c>) on every central directory header, matching the original Manic
    ///   EMU archive exactly in the non-data region.</item>
    /// </list>
    /// </summary>
    private static void NormalizeZipHeadersForZipFoundation(byte[] zipBytes)
    {
        const int lfhSignature = 0x04034B50;  // "PK\x03\x04"
        const int cdfhSignature = 0x02014B50; // "PK\x01\x02"
        const ushort utf8Flag = 0x0800;
        const ushort zipFoundationVersionMadeBy = 0x0315; // Unix (0x03) + ZIP spec 2.1 (0x15)

        var eocdOffset = FindEndOfCentralDirectory(zipBytes);
        if (eocdOffset < 0)
        {
            return;
        }

        var cdOffset = BitConverter.ToInt32(zipBytes, eocdOffset + 16);
        var cdSize = BitConverter.ToInt32(zipBytes, eocdOffset + 12);
        var entryCount = BitConverter.ToUInt16(zipBytes, eocdOffset + 10);

        // Patch every local file header's flags (offset 6 from LFH start).
        var pos = 0;
        for (var i = 0; i < entryCount && pos + 30 <= cdOffset; i++)
        {
            if (BitConverter.ToInt32(zipBytes, pos) != lfhSignature)
            {
                break;
            }

            SetFlagBits(zipBytes, pos + 6, utf8Flag);

            var nameLen = BitConverter.ToUInt16(zipBytes, pos + 26);
            var extraLen = BitConverter.ToUInt16(zipBytes, pos + 28);
            var compSize = BitConverter.ToUInt32(zipBytes, pos + 18);
            pos += 30 + nameLen + extraLen + (int)compSize;
        }

        // Patch every central directory file header's flags (offset 8) and versionMadeBy
        // (offset 4).
        pos = cdOffset;
        for (var i = 0; i < entryCount && pos + 46 <= cdOffset + cdSize; i++)
        {
            if (BitConverter.ToInt32(zipBytes, pos) != cdfhSignature)
            {
                break;
            }

            SetFlagBits(zipBytes, pos + 8, utf8Flag);
            WriteUInt16(zipBytes, pos + 4, zipFoundationVersionMadeBy);

            var nameLen = BitConverter.ToUInt16(zipBytes, pos + 28);
            var extraLen = BitConverter.ToUInt16(zipBytes, pos + 30);
            var commentLen = BitConverter.ToUInt16(zipBytes, pos + 32);
            pos += 46 + nameLen + extraLen + commentLen;
        }
    }

    private static int FindEndOfCentralDirectory(byte[] zipBytes)
    {
        // EOCD signature is PK\x05\x06; scan backward from the end since the record has a
        // variable-length comment field suffix.
        const int eocdSignature = 0x06054B50;
        const int eocdMinSize = 22;
        const int eocdMaxCommentLen = 65535;

        var searchStart = Math.Max(0, zipBytes.Length - eocdMinSize - eocdMaxCommentLen);
        for (var i = zipBytes.Length - eocdMinSize; i >= searchStart; i--)
        {
            if (BitConverter.ToInt32(zipBytes, i) == eocdSignature)
            {
                return i;
            }
        }

        return -1;
    }

    private static void SetFlagBits(byte[] zipBytes, int flagsOffset, ushort mask)
    {
        var current = BitConverter.ToUInt16(zipBytes, flagsOffset);
        var updated = (ushort)(current | mask);
        zipBytes[flagsOffset] = (byte)(updated & 0xFF);
        zipBytes[flagsOffset + 1] = (byte)((updated >> 8) & 0xFF);
    }

    private static void WriteUInt16(byte[] zipBytes, int offset, ushort value)
    {
        zipBytes[offset] = (byte)(value & 0xFF);
        zipBytes[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    /// <summary>
    /// Computes the export filename and compound extension for a Manic EMU save archive,
    /// preserving the original compound extension for round-trip compatibility and appending a
    /// UTC timestamp suffix to the stem to prevent iOS's "filename-already-exists" auto-rename
    /// (which inserts a space — e.g. <c>.3ds 2.sav</c> — that breaks Manic EMU's
    /// <c>url.path.contains(".3ds.sav")</c> substring check on re-import).
    /// </summary>
    /// <param name="originalName">
    /// Original filename of the loaded archive (may be <see langword="null" />).
    /// </param>
    /// <param name="timestamp">
    /// Timestamp to embed in the export name. Defaults to <see cref="DateTime.UtcNow" />.
    /// An existing <c>-yyyyMMddTHHmmss</c> suffix on the stem is stripped first, so
    /// the name doesn't accumulate timestamps across multiple round-trips.
    /// </param>
    /// <returns>
    /// A tuple of the full export filename and its compound extension. Normally the canonical
    /// <c>.3ds.sav</c> suffix, e.g. <c>("AlphaSapphire-20260420T174612.3ds.sav", ".3ds.sav")</c>.
    /// If the user uploaded a file whose name already ends in <c>.3ds.save</c> (manual rename
    /// or iOS-side extension mangling), we echo that back so the round-trip is bit-for-bit
    /// transparent.
    /// </returns>
    public static (string ExportName, string CompoundExtension) GetExportFileName(string? originalName, DateTime? timestamp = null)
    {
        const string savExt = ".3ds.sav";
        const string saveExt = ".3ds.save";

        var suffix = "-" + (timestamp ?? DateTime.UtcNow).ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);

        if (originalName is null)
        {
            return ("save" + suffix + savExt, savExt);
        }

        // Check .3ds.save first — it's the more specific suffix (ends in .3ds.sav also matches it).
        if (originalName.EndsWith(saveExt, StringComparison.OrdinalIgnoreCase))
        {
            var stem = StripExistingTimestampSuffix(originalName[..^saveExt.Length]);
            return ((stem.Length > 0 ? stem : "save") + suffix + saveExt, saveExt);
        }

        if (originalName.EndsWith(savExt, StringComparison.OrdinalIgnoreCase))
        {
            var stem = StripExistingTimestampSuffix(originalName[..^savExt.Length]);
            return ((stem.Length > 0 ? stem : "save") + suffix + savExt, savExt);
        }

        // Unknown/no compound extension — strip the last extension and default to .3ds.sav.
        var fallbackStem = StripExistingTimestampSuffix(Path.GetFileNameWithoutExtension(originalName));
        return ((fallbackStem.Length > 0 ? fallbackStem : "save") + suffix + savExt, savExt);
    }

    /// <summary>
    /// Removes a trailing <c>-yyyyMMddTHHmmss</c> timestamp suffix from <paramref name="stem" />
    /// if one is present, so repeated round-trips don't accumulate timestamps. Uses a strict
    /// length + digit check to avoid stripping unrelated user suffixes that happen to end in
    /// hyphens or digits.
    /// </summary>
    private static string StripExistingTimestampSuffix(string stem)
    {
        // Format: ...-yyyyMMddTHHmmss (1 + 8 + 1 + 6 = 16 chars)
        const int suffixLength = 16;
        if (stem.Length < suffixLength + 1 || stem[^suffixLength] != '-' || stem[^7] != 'T')
        {
            return stem;
        }

        for (var i = stem.Length - suffixLength + 1; i < stem.Length; i++)
        {
            if (i == stem.Length - 7)
            {
                continue; // 'T' separator, already checked
            }

            if (!char.IsDigit(stem[i]))
            {
                return stem;
            }
        }

        return stem[..^suffixLength];
    }

    /// <summary>
    /// Metadata required to rebuild a <c>.3ds.sav</c> / <c>.3ds.save</c> ZIP after the save has been edited.
    /// </summary>
    public sealed record ManicEmuSaveContext(byte[] OriginalZipBytes, string SaveEntryPath);
}
