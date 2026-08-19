using System.IO.Compression;
using Pkmds.Core.Utilities;

namespace Pkmds.Tests;

/// <summary>
/// Tests for <see cref="ManicEmuSaveHelper" /> ZIP detection, extraction, and rebuild logic.
/// </summary>
public class ManicEmuSaveHelperTests
{
    private const string TestFilesPath = "../../../TestFiles";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an in-memory ZIP with one entry at <paramref name="entryPath" /> containing
    /// <paramref name="entryBytes" />.
    /// </summary>
    private static byte[] BuildZip(string entryPath, byte[] entryBytes, string? extraEntryPath = null, byte[]? extraEntryBytes = null)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            {
                stream.Write(entryBytes, 0, entryBytes.Length);
            }

            if (extraEntryPath is not null && extraEntryBytes is not null)
            {
                var extra = archive.CreateEntry(extraEntryPath, CompressionLevel.Optimal);
                using var extraStream = extra.Open();
                extraStream.Write(extraEntryBytes, 0, extraEntryBytes.Length);
            }
        }

        return ms.ToArray();
    }

    /// <summary>Builds an in-memory ZIP with <paramref name="count" /> entries all under a non-<c>sdmc/</c> path.</summary>
    private static byte[] BuildZipWithManyNonSdmcEntries(int count)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < count; i++)
            {
                var entry = archive.CreateEntry($"other/dir{i}/file.bin", CompressionLevel.Optimal);
                using var stream = entry.Open();
                stream.Write([0xAA], 0, 1);
            }
        }

        return ms.ToArray();
    }

    /// <summary>Builds an in-memory ZIP with <paramref name="count" /> entries all under <c>sdmc/</c>.</summary>
    private static byte[] BuildZipWithManySdmcEntries(int count)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < count; i++)
            {
                var entry = archive.CreateEntry($"sdmc/dir{i}/file.bin", CompressionLevel.Optimal);
                using var stream = entry.Open();
                stream.Write([0xAA], 0, 1);
            }
        }

        return ms.ToArray();
    }

    // ── IsZip ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsZip_ZipMagicBytes_ReturnsTrue()
    {
        var zipBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 };
        ManicEmuSaveHelper.IsZip(zipBytes).Should().BeTrue();
    }

    [Fact]
    public void IsZip_NonZipBytes_ReturnsFalse()
    {
        var notZip = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        ManicEmuSaveHelper.IsZip(notZip).Should().BeFalse();
    }

    [Fact]
    public void IsZip_EmptySpan_ReturnsFalse() => ManicEmuSaveHelper.IsZip(ReadOnlySpan<byte>.Empty).Should().BeFalse();

    // ── TryExtractSaveFromZip ─────────────────────────────────────────────

    [Fact]
    public void TryExtractSaveFromZip_NonZipData_ReturnsFalse()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        ManicEmuSaveHelper.TryExtractSaveFromZip(data, null, out var saveFile, out var ctx).Should().BeFalse();
        saveFile.Should().BeNull();
        ctx.Should().BeNull();
    }

    [Fact]
    public void TryExtractSaveFromZip_ZipWithNoSdmcEntries_ReturnsFalse()
    {
        // ZIP contains an entry NOT under sdmc/ — should be ignored
        var zipBytes = BuildZip("other/path/save.bin", [0xDE, 0xAD, 0xBE, 0xEF]);
        ManicEmuSaveHelper.TryExtractSaveFromZip(zipBytes, null, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryExtractSaveFromZip_ZipWithOversizedEntry_SkipsEntry()
    {
        // Entry is under sdmc/ but is larger than the 8 MB uncompressed cap — should be skipped.
        var oversizedBytes = new byte[8 * 1024 * 1024 + 1];
        var zipBytes = BuildZip("sdmc/Nintendo 3DS/save/save.bin", oversizedBytes);

        ManicEmuSaveHelper.TryExtractSaveFromZip(zipBytes, null, out var saveFile, out var ctx).Should().BeFalse();
        saveFile.Should().BeNull();
        ctx.Should().BeNull();
    }

    [Fact]
    public void TryExtractSaveFromZip_ValidZipWithRealSave_ReturnsSaveFile()
    {
        // Arrange — wrap a known-good 3DS save (Gen 7) in a Manic EMU-style ZIP
        const string saveEntryPath = "sdmc/Nintendo 3DS/00000000000000000000000000000000/00000000000000000000000000000000/title/00040000/00175e00/data/00000001/main";

        var saveBytes = File.ReadAllBytes(Path.Combine(TestFilesPath, "moon.sav"));
        var zipBytes = BuildZip(saveEntryPath, saveBytes);

        // Act
        var result = ManicEmuSaveHelper.TryExtractSaveFromZip(zipBytes, "moon.sav", out var saveFile, out var ctx);

        // Assert
        result.Should().BeTrue();
        saveFile.Should().NotBeNull();
        saveFile.Should().BeOfType<SAV7SM>();
        ctx.Should().NotBeNull();
        ctx.SaveEntryPath.Should().Be(saveEntryPath);
    }

    [Fact]
    public void TryExtractSaveFromZip_ZipWithTooManyTotalEntries_ReturnsFalse()
    {
        // A ZIP with more than MaxTotalEntries (500) non-sdmc/ entries — the total-entry guard
        // should reject it before the loop even begins, preventing DoS via iteration cost alone.
        var zipBytes = BuildZipWithManyNonSdmcEntries(501);
        ManicEmuSaveHelper.TryExtractSaveFromZip(zipBytes, null, out var saveFile, out var ctx).Should().BeFalse();
        saveFile.Should().BeNull();
        ctx.Should().BeNull();
    }

    [Fact]
    public void TryExtractSaveFromZip_ZipWithTooManySdmcEntries_ReturnsFalse()
    {
        // A ZIP with more than MaxSdmcEntriesToInspect (100) sdmc/ entries — none are valid saves,
        // but the important thing is the loop is capped and doesn't inspect them all.
        var zipBytes = BuildZipWithManySdmcEntries(101);
        ManicEmuSaveHelper.TryExtractSaveFromZip(zipBytes, null, out var saveFile, out var ctx).Should().BeFalse();
        saveFile.Should().BeNull();
        ctx.Should().BeNull();
    }

    // ── RebuildZip ────────────────────────────────────────────────────────

    [Fact]
    public void RebuildZip_ReplacesOnlySaveEntry()
    {
        // Arrange — ZIP with two entries: the save and an extra file
        const string saveEntryPath = "sdmc/Nintendo 3DS/save/main";
        const string extraEntryPath = "sdmc/Nintendo 3DS/extra/data.bin";

        var originalSaveBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var extraBytes = new byte[] { 0xAA, 0xBB, 0xCC };
        var zipBytes = BuildZip(saveEntryPath, originalSaveBytes, extraEntryPath, extraBytes);

        var context = new ManicEmuSaveHelper.ManicEmuSaveContext(zipBytes, saveEntryPath);
        var newSaveBytes = new byte[] { 0x10, 0x20, 0x30, 0x40 };

        // Act
        var rebuilt = ManicEmuSaveHelper.RebuildZip(context, newSaveBytes);

        // Assert — open the rebuilt ZIP and verify entries
        using var ms = new MemoryStream(rebuilt);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        archive.Entries.Should().HaveCount(2);

        var saveEntry = archive.GetEntry(saveEntryPath);
        saveEntry.Should().NotBeNull();
        using var saveStream = saveEntry.Open();
        var readBack = new byte[newSaveBytes.Length];
        saveStream.ReadExactly(readBack);
        readBack.Should().Equal(newSaveBytes);

        var extraEntry = archive.GetEntry(extraEntryPath);
        extraEntry.Should().NotBeNull();
        using var extraStream = extraEntry.Open();
        var extraReadBack = new byte[extraBytes.Length];
        extraStream.ReadExactly(extraReadBack);
        extraReadBack.Should().Equal(extraBytes);
    }

    // ── GetExportFileName ─────────────────────────────────────────────────

    private static readonly DateTime FixedTimestamp = new(2026, 04, 20, 17, 46, 12, DateTimeKind.Utc);
    private const string TimestampSuffix = "-20260420T174612";

    [Fact]
    public void GetExportFileName_NullOriginalName_ReturnsSavSuffixWithTimestamp()
    {
        var (name, ext) = ManicEmuSaveHelper.GetExportFileName(null, FixedTimestamp);
        ext.Should().Be(".3ds.sav");
        name.Should().Be($"save{TimestampSuffix}.3ds.sav");
    }

    [Theory]
    [InlineData("AlphaSapphire.3ds.sav", "AlphaSapphire", ".3ds.sav")]
    [InlineData("ALPHASAPPHIRE.3DS.SAV", "ALPHASAPPHIRE", ".3ds.sav")] // case-insensitive strip
    [InlineData(".3ds.sav", "save", ".3ds.sav")] // empty stem fallback
    public void GetExportFileName_DotSav_PreservesSavExtension(string input, string expectedStem, string expectedExt)
    {
        var (name, ext) = ManicEmuSaveHelper.GetExportFileName(input, FixedTimestamp);
        name.Should().Be($"{expectedStem}{TimestampSuffix}{expectedExt}");
        ext.Should().Be(expectedExt);
    }

    [Theory]
    [InlineData("AlphaSapphire.3ds.save", "AlphaSapphire", ".3ds.save")]
    [InlineData("ALPHASAPPHIRE.3DS.SAVE", "ALPHASAPPHIRE", ".3ds.save")] // case-insensitive strip
    [InlineData(".3ds.save", "save", ".3ds.save")] // empty stem fallback
    public void GetExportFileName_DotSave_PreservesSaveExtension(string input, string expectedStem, string expectedExt)
    {
        var (name, ext) = ManicEmuSaveHelper.GetExportFileName(input, FixedTimestamp);
        name.Should().Be($"{expectedStem}{TimestampSuffix}{expectedExt}");
        ext.Should().Be(expectedExt);
    }

    [Fact]
    public void GetExportFileName_UnknownExtension_DefaultsToSavWithTimestamp()
    {
        // An unknown extension should strip the last extension and default to .3ds.sav.
        var (name, ext) = ManicEmuSaveHelper.GetExportFileName("game.unknown", FixedTimestamp);
        ext.Should().Be(".3ds.sav");
        name.Should().Be($"game{TimestampSuffix}.3ds.sav");
    }

    [Theory]
    [InlineData("AlphaSapphire-20260420T174612.3ds.sav", "AlphaSapphire")]
    [InlineData("AlphaSapphire-20260101T000000-20260420T174612.3ds.sav", "AlphaSapphire-20260101T000000")]
    public void GetExportFileName_PreviousTimestampSuffix_IsReplacedNotAccumulated(string input, string expectedStem)
    {
        // Round-tripping an already-timestamped filename through the export must not accumulate
        // timestamps (would produce ever-longer filenames across successive round-trips).
        var (name, _) = ManicEmuSaveHelper.GetExportFileName(input, FixedTimestamp);
        name.Should().Be($"{expectedStem}{TimestampSuffix}.3ds.sav");
    }

    [Fact]
    public void GetExportFileName_SuffixThatLooksLikeTimestampButIsnt_IsPreserved()
    {
        // A hyphen + 15 characters that happen to look like a timestamp but aren't (e.g. any
        // non-digit, or a wrong separator) should not be mistakenly stripped.
        var (name, _) = ManicEmuSaveHelper.GetExportFileName("AlphaSapphire-1234567X123456.3ds.sav", FixedTimestamp);
        name.Should().Be($"AlphaSapphire-1234567X123456{TimestampSuffix}.3ds.sav");
    }

    [Fact]
    public void RebuildZip_OversizedNonSaveEntry_ThrowsInvalidDataException()
    {
        // Arrange — ZIP with a valid save entry plus an extra entry over 8 MB
        const string saveEntryPath = "sdmc/Nintendo 3DS/save/main";
        const string extraEntryPath = "sdmc/Nintendo 3DS/extra/large.bin";

        var originalSaveBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var oversizedBytes = new byte[8 * 1024 * 1024 + 1];
        var zipBytes = BuildZip(saveEntryPath, originalSaveBytes, extraEntryPath, oversizedBytes);

        var context = new ManicEmuSaveHelper.ManicEmuSaveContext(zipBytes, saveEntryPath);
        var newSaveBytes = new byte[] { 0x10, 0x20, 0x30, 0x40 };

        // Act & Assert — the oversized non-save entry should cause an InvalidDataException
        var act = () => ManicEmuSaveHelper.RebuildZip(context, newSaveBytes);
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void RebuildZip_RoundTrip_ProducesLoadableSave()
    {
        // Arrange — wrap moon.sav in a ZIP, extract it, rebuild with new bytes, re-extract
        const string saveEntryPath = "sdmc/Nintendo 3DS/00000000000000000000000000000000/00000000000000000000000000000000/title/00040000/00175e00/data/00000001/main";

        var saveBytes = File.ReadAllBytes(Path.Combine(TestFilesPath, "moon.sav"));
        var zipBytes = BuildZip(saveEntryPath, saveBytes);

        ManicEmuSaveHelper.TryExtractSaveFromZip(zipBytes, "moon.sav", out var saveFile, out var ctx).Should().BeTrue();

        // Re-export the save bytes via PKHeX and rebuild the ZIP
        var exportedBytes = saveFile!.Write().ToArray();
        var rebuilt = ManicEmuSaveHelper.RebuildZip(ctx!, exportedBytes);

        // Assert — re-extract from the rebuilt ZIP and confirm it's a valid, loadable save
        ManicEmuSaveHelper.TryExtractSaveFromZip(rebuilt, "moon.sav", out var reExtracted, out _).Should().BeTrue();
        reExtracted.Should().NotBeNull();
        reExtracted.Should().BeOfType<SAV7SM>();
        reExtracted.Write().Length.Should().Be(saveBytes.Length);
    }
}
