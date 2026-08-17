using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace Pkmds.Core.Utilities;

public enum SaveArchiveKind
{
    GenericZip,
    ManicEmu
}

/// <summary>
/// Captures the ZIP wrapper around a loaded save so exports can replace the inner save without
/// changing the file format the emulator expects.
/// </summary>
public sealed record SaveArchiveContext(
    byte[] OriginalZipBytes,
    string SaveEntryPath,
    SaveArchiveKind Kind)
{
    public bool IsManicEmu => Kind == SaveArchiveKind.ManicEmu;
}

/// <summary>
/// Loads and rebuilds ordinary ZIP-wrapped saves. Manic EMU archives keep using their dedicated
/// writer because that emulator requires ZIPFoundation-compatible headers and store compression.
/// </summary>
public static class SaveArchiveHelper
{
    private const long MaxUncompressedEntrySize = 8 * 1024 * 1024;
    private const int MaxTotalEntries = 500;

    public static bool TryExtractGenericZip(
        byte[] zipBytes,
        [NotNullWhen(true)] out SaveFile? saveFile,
        [NotNullWhen(true)] out SaveArchiveContext? context)
    {
        saveFile = null;
        context = null;

        if (!ManicEmuSaveHelper.IsZip(zipBytes))
        {
            return false;
        }

        try
        {
            using var zipStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            if (archive.Entries.Count is 0 or > MaxTotalEntries)
            {
                return false;
            }

            foreach (var entry in GetCandidateEntries(archive))
            {
                if (entry.Length is 0 or > MaxUncompressedEntrySize || !SaveUtil.IsSizeValid(entry.Length))
                {
                    continue;
                }

                var entryBytes = ReadEntryWithLimit(entry);
                if (entryBytes is null ||
                    !SaveFileLoader.TryLoadRawSave(entryBytes, entry.FullName, out var parsedSave))
                {
                    continue;
                }

                saveFile = parsedSave;
                context = new SaveArchiveContext(zipBytes, entry.FullName, SaveArchiveKind.GenericZip);
                return true;
            }
        }
        catch (InvalidDataException)
        {
            // Malformed ZIP data is not a supported save archive.
        }
        catch (IOException)
        {
            // Truncated or otherwise unreadable ZIP data.
        }
        catch (NotSupportedException)
        {
            // Unsupported ZIP feature.
        }

        return false;
    }

    public static byte[] RebuildZip(SaveArchiveContext context, byte[] newSaveBytes)
    {
        if (context.IsManicEmu)
        {
            var manicContext = new ManicEmuSaveHelper.ManicEmuSaveContext(
                context.OriginalZipBytes,
                context.SaveEntryPath);
            return ManicEmuSaveHelper.RebuildZip(manicContext, newSaveBytes);
        }

        using var resultStream = new MemoryStream();
        resultStream.Write(context.OriginalZipBytes);
        resultStream.Position = 0;

        using (var archive = new ZipArchive(resultStream, ZipArchiveMode.Update, leaveOpen: true))
        {
            if (archive.Entries.Count > MaxTotalEntries)
            {
                throw new InvalidDataException($"Save archive contains more than {MaxTotalEntries} entries.");
            }

            var entry = archive.GetEntry(context.SaveEntryPath)
                        ?? throw new InvalidDataException(
                            $"Save entry '{context.SaveEntryPath}' is missing from the archive.");
            using var entryStream = entry.Open();
            entryStream.SetLength(0);
            entryStream.Write(newSaveBytes);
        }

        return resultStream.ToArray();
    }

    private static IEnumerable<ZipArchiveEntry> GetCandidateEntries(ZipArchive archive)
    {
        if (archive.Entries.Count == 1)
        {
            yield return archive.Entries[0];
            yield break;
        }

        foreach (var entry in archive.Entries)
        {
            if (entry.Name.Equals("main", StringComparison.OrdinalIgnoreCase) ||
                entry.Name.Equals("SaveData.bin", StringComparison.OrdinalIgnoreCase))
            {
                yield return entry;
            }
        }
    }

    private static byte[]? ReadEntryWithLimit(ZipArchiveEntry entry)
    {
        using var destination = new MemoryStream((int)entry.Length);
        using var source = entry.Open();
        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalRead += read;
            if (totalRead > MaxUncompressedEntrySize)
            {
                return null;
            }

            destination.Write(buffer, 0, read);
        }

        return destination.ToArray();
    }
}
