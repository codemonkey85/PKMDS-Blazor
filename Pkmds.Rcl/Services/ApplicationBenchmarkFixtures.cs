using System.IO.Compression;

namespace Pkmds.Rcl.Services;

/// <summary>Synthetic, deterministic inputs; no user files or storage are involved.</summary>
internal static class ApplicationBenchmarkFixtures
{
    internal const string Tag = "BENCH";
    internal const string Source = "synthetic-benchmark";
    internal static readonly DateTimeOffset ImportedAt = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    internal static readonly byte[] Metadata = Encoding.UTF8.GetBytes("PKMDS synthetic archive fixture\n");

    internal static PKM[] CreatePokemon(int count)
    {
        ushort[] species = [(ushort)Species.Pikachu, (ushort)Species.Eevee, (ushort)Species.Charizard];
        var result = new PKM[count];
        for (var i = 0; i < count; i++)
        {
            var pkm = new PK9
            {
                Species = species[i % species.Length],
                EncryptionConstant = 0x12340000u + (uint)i,
                PID = 0x43210000u + (uint)i,
                Nickname = $"BENCH{i:D4}",
                OriginalTrainerName = Tag,
                Version = GameVersion.SL,
                Language = (int)LanguageID.English,
            };
            pkm.RefreshChecksum();
            result[i] = pkm;
        }
        return result;
    }

    internal static byte[][] GetStoredBytes(PKM[] pokemon) => pokemon.Select(pkm =>
    {
        var bytes = new byte[pkm.SIZE_STORED];
        pkm.WriteDecryptedDataStored(bytes);
        return bytes;
    }).ToArray();

    internal static byte[] CreateBankImport(PKM[] pokemon, byte[][] storedBytes) =>
        JsonSerializer.SerializeToUtf8Bytes(pokemon.Select((pkm, i) => new BankService.RawEntry
        {
            Id = i + 1,
            BytesBase64 = Convert.ToBase64String(storedBytes[i]),
            AddedAt = ImportedAt.ToString("O", CultureInfo.InvariantCulture),
            Meta = new BankService.RawMeta
            {
                Species = pkm.Species,
                IsShiny = pkm.IsShiny,
                Nickname = pkm.Nickname,
                Ext = pkm.Extension,
                Tag = Tag,
                SourceSave = Source,
            },
        }).ToArray(), BankService.BankJsonContext.Default.RawEntryArray);

    internal static byte[] CreateSave(uint money)
    {
        var save = new SAV7SM
        {
            OT = Tag,
            Version = GameVersion.SN,
            Language = (int)LanguageID.English,
            Money = money,
        };
        // PKHeX.Core 26.10.18's blank constructor leaves the BEEF footer marker
        // unset; SaveUtil requires it to recognize a serialized Gen 7 save.
        // Initialize our synthetic fixture, preserving the production loader.
        "FEEB"u8.CopyTo(save.Data[^0x1F0..]);
        return save.Write().ToArray();
    }

    internal static byte[] CreateArchive(byte[] save, bool manic)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var path = manic ? "sdmc/Nintendo 3DS/bench/data/00000001/main" : "saves/main";
            WriteEntry(archive, path, save, manic ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
            WriteEntry(archive, "metadata.txt", Metadata, CompressionLevel.NoCompression);
        }
        var bytes = output.ToArray();
        // Exercise the production Manic writer/header conventions, not just a generic ZIP.
        return manic
            ? ManicEmuSaveHelper.RebuildZip(new ManicEmuSaveHelper.ManicEmuSaveContext(bytes,
                "sdmc/Nintendo 3DS/bench/data/00000001/main"), save)
            : bytes;
    }

    private static void WriteEntry(ZipArchive archive, string path, byte[] bytes, CompressionLevel compression)
    {
        var entry = archive.CreateEntry(path, compression);
        entry.LastWriteTime = ImportedAt;
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    internal static void VerifyArchive(byte[] bytes, byte[] expectedSave, SaveArchiveKind kind, uint money)
    {
        if (!SaveFileLoader.TryLoad(bytes, "benchmark.3ds.sav", out var save, out var context) ||
            save is not SAV7SM || save.Money != money || save.OT != Tag || context?.Kind != kind)
        {
            throw new InvalidOperationException("Synthetic archive did not reload with its save and wrapper intact.");
        }
        using var input = new MemoryStream(bytes);
        using var archive = new ZipArchive(input, ZipArchiveMode.Read);
        if (archive.Entries.Count != 2) throw new InvalidOperationException("Archive entries changed.");
        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open();
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            var expected = entry.FullName == context.SaveEntryPath ? expectedSave : Metadata;
            if ((entry.FullName != context.SaveEntryPath && entry.FullName != "metadata.txt") ||
                entry.LastWriteTime.DateTime != ImportedAt.DateTime || !copy.ToArray().AsSpan().SequenceEqual(expected))
            {
                // ZIP stores a local clock value, without a timezone/UTC offset.
                throw new InvalidOperationException($"Archive {kind} entry '{entry.FullName}' changed: timestamp {entry.LastWriteTime:O}; expected clock {ImportedAt.DateTime:O}; bytes equal: {copy.ToArray().AsSpan().SequenceEqual(expected)}.");
            }
        }
    }
}
