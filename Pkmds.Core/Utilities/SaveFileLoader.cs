using System.Diagnostics.CodeAnalysis;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Pkmds.Core.Utilities;

/// <summary>
/// Unified entry point for loading user-supplied save data. Captures ZIP wrappers before
/// delegating to PKHeX for raw-save detection so exports can preserve the original format.
/// </summary>
/// <remarks>
/// The ordering here is load-bearing. PKHeX.Core's built-in <see cref="ZipReader" />
/// (registered in <see cref="SaveUtil.CustomSaveReaders" />) recognises any ZIP with a
/// <c>main</c> or <c>SaveData.bin</c> entry and unwraps it invisibly — including Manic EMU
/// archives. If we call <see cref="SaveUtil.TryGetSaveFile(Memory{byte}, out SaveFile?, string?)" />
/// first on a ZIP we get back a valid <see cref="SaveFile" />, but the surrounding archive
/// metadata is silently lost. Export then writes bare save bytes under a <c>.zip</c> filename,
/// which emulators reject as corrupt (issues #750 and #1131-#1133).
/// </remarks>
public static class SaveFileLoader
{
    /// <summary>
    /// Attempts to load <paramref name="data" /> as either a ZIP archive or a raw save.
    /// </summary>
    /// <param name="data">Raw upload bytes.</param>
    /// <param name="fileName">Original filename of the upload (may be <see langword="null" />).</param>
    /// <param name="saveFile">
    /// The parsed <see cref="SaveFile" /> instance on success.
    /// </param>
    /// <param name="archiveContext">
    /// Non-<see langword="null" /> only when the upload was a supported ZIP. Pass this back to
    /// <see cref="SaveArchiveHelper.RebuildZip" /> on export to round-trip correctly.
    /// </param>
    /// <returns><see langword="true" /> on successful load; <see langword="false" /> otherwise.</returns>
    public static bool TryLoad(
        byte[] data,
        string? fileName,
        [NotNullWhen(true)] out SaveFile? saveFile,
        out SaveArchiveContext? archiveContext)
    {
        archiveContext = null;

        // Manic EMU detection must run before SaveUtil.TryGetSaveFile because PKHeX's ZipReader
        // would otherwise unwrap the archive invisibly, stripping the context we need for re-export.
        if (ManicEmuSaveHelper.IsZip(data))
        {
            if (ManicEmuSaveHelper.TryExtractSaveFromZip(data, fileName, out saveFile, out var manicContext))
            {
                archiveContext = new SaveArchiveContext(
                    manicContext.OriginalZipBytes,
                    manicContext.SaveEntryPath,
                    SaveArchiveKind.ManicEmu);
                return true;
            }

            if (SaveArchiveHelper.TryExtractGenericZip(data, out saveFile, out var genericContext))
            {
                archiveContext = genericContext;
                return true;
            }
        }

        return TryLoadRawSave(data, fileName, out saveFile);
    }

    internal static bool TryLoadRawSave(
        byte[] data,
        string? fileName,
        [NotNullWhen(true)] out SaveFile? saveFile)
    {
        // Gen 4 and Gen 5 raw saves share the same 512 KiB size. PKHeX currently checks the
        // Gen 4 signatures first, so unrelated Gen 5 extdata can occasionally satisfy an HGSS
        // footer signature and win before PKHeX examines the valid Gen 5 checksum footer
        // (issue #1127). Prefer a checksum-valid Gen 5 footer before delegating to the
        // normal detector. This mirrors PKHeX's own footer validation; it does not repair or
        // guess at damaged data.
        if (TryLoadGen5ByFooter(data, out saveFile))
        {
            return true;
        }

        return SaveUtil.TryGetSaveFile(data, out saveFile, fileName);
    }

    private static bool TryLoadGen5ByFooter(
        byte[] data,
        [NotNullWhen(true)] out SaveFile? saveFile)
    {
        saveFile = null;
        if (data.Length != SaveUtil.SIZE_G5RAW)
        {
            return false;
        }

        if (HasValidGen5Footer(data, SaveUtil.SIZE_G5BW, 0x8C))
        {
            saveFile = new SAV5BW(data);
            return true;
        }

        if (HasValidGen5Footer(data, SaveUtil.SIZE_G5B2W2, 0x94))
        {
            saveFile = new SAV5B2W2(data);
            return true;
        }

        return false;
    }

    private static bool HasValidGen5Footer(ReadOnlySpan<byte> data, int mainSize, int infoLength)
    {
        var footer = data.Slice(mainSize - 0x100, infoLength + 0x10);
        var stored = ReadUInt16LittleEndian(footer[^2..]);
        var actual = Checksums.CRC16_CCITT(footer[..infoLength]);
        return stored == actual;
    }
}
