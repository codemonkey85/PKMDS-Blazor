using System.Buffers.Binary;
using System.IO.Compression;
using Pkmds.Core.Utilities;

namespace Pkmds.Tests;

public class ArchiveIntegrityTests
{
    [Theory]
    [InlineData(false, "valid", true)]
    [InlineData(true, "valid", true)]
    [InlineData(false, "crc", false)]
    [InlineData(true, "crc", false)]
    [InlineData(false, "truncated", false)]
    [InlineData(true, "truncated", false)]
    public void ExtractSave_ValidatesArchiveIntegrity(bool manic, string damage, bool expected)
    {
        var saveBytes = File.ReadAllBytes("../../../TestFiles/moon.sav");
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(manic ? "sdmc/save/main" : "main", CompressionLevel.NoCompression);
            using var stream = entry.Open();
            stream.Write(saveBytes);
        }

        var bytes = output.ToArray();
        if (damage == "crc")
        {
            // Keep both headers consistent but give the unchanged save an incorrect checksum.
            // .NET 11 must reject this instead of accepting the otherwise valid save payload.
            ReadOnlySpan<byte> endSignature = [0x50, 0x4b, 0x05, 0x06];
            var end = bytes.AsSpan().LastIndexOf(endSignature);
            var central = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(end + 16));
            bytes[14] ^= 1;
            bytes[central + 16] ^= 1;
        }
        else if (damage == "truncated")
        {
            bytes = bytes[..(bytes.Length / 2)];
        }

        if (manic)
        {
            ManicEmuSaveHelper.TryExtractSaveFromZip(bytes, "moon.sav", out var save, out var context)
                .Should().Be(expected);
            (save is not null).Should().Be(expected);
            (context is not null).Should().Be(expected);
        }
        else
        {
            SaveArchiveHelper.TryExtractGenericZip(bytes, out var save, out var context).Should().Be(expected);
            (save is not null).Should().Be(expected);
            (context is not null).Should().Be(expected);
        }
    }
}
