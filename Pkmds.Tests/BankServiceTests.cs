namespace Pkmds.Tests;

/// <summary>
/// Unit tests for <see cref="BankService" /> using bUnit's JS-interop mocks.
/// <see cref="BankService.RawEntry" /> and <see cref="BankService.RawMeta" /> are
/// <c>internal</c> and accessible here via <c>InternalsVisibleTo</c> in Pkmds.Rcl.csproj.
/// The bank JS contract passes UTF-8 bytes across the IJS boundary (trim-safe);
/// these tests mirror that by serializing entries with the service's own source-gen
/// context before handing them to the mock.
/// </summary>
public class BankServiceTests
{
    private const string TestFilesPath = "../../../TestFiles";

    private static byte[] GetStoredData(PKM pkm)
    {
        var data = new byte[pkm.SIZE_STORED];
        pkm.WriteDecryptedDataStored(data);
        return data;
    }

    /// <summary>
    /// Creates a <see cref="BankService" /> backed by bUnit's JS-interop mock with
    /// <paramref name="bankEntries" /> pre-loaded as the <c>exportAll</c>
    /// result. Legacy levels simulate a missing byte export, then missing JSON export.
    /// </summary>
    private static (BankService Service, BunitContext Ctx) CreateService(
        BankService.RawEntry[]? bankEntries = null, int legacyLevel = 0)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var json = JsonSerializer.Serialize(
            bankEntries ?? [],
            BankService.BankJsonContext.Default.RawEntryArray);

        var module = ctx.JSInterop.SetupModule("./js/bank.js");
        if (legacyLevel == 0)
        {
            module.Setup<byte[]>("exportAll").SetResult(System.Text.Encoding.UTF8.GetBytes(json));
        }
        else
        {
            module.Setup<byte[]>("exportAll").SetException(new JSException("exportAll is not a function"));
            if (legacyLevel == 1)
            {
                module.Setup<string>("getAllPokemonJson").SetResult(json);
            }
            else
            {
                module.Setup<string>("getAllPokemonJson").SetException(new JSException("getAllPokemonJson is not a function"));
                using var document = JsonDocument.Parse(json);
                module.Setup<JsonElement>("getAllPokemon").SetResult(document.RootElement.Clone());
            }
        }

        var jsRuntime = ctx.Services.GetRequiredService<IJSRuntime>();
        var service = new BankService(jsRuntime);
        return (service, ctx);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetAllAsync_ReadPaths_PreservePokemonBytesAndMetadata(int legacyLevel)
    {
        PKM[] pokemon = [new PK1 { Species = 25 }, new PK5 { Species = 133, Version = GameVersion.B }, new PK9 { Species = 6, Version = GameVersion.SL }];
        var stored = pokemon.Select(GetStoredData).ToArray();
        var addedAt = new DateTimeOffset(2020, 2, 3, 4, 5, 6, TimeSpan.FromHours(-5));
        var raw = pokemon.Select((pk, i) => new BankService.RawEntry
        {
            Id = 10 + i,
            BytesBase64 = Convert.ToBase64String(stored[i]),
            AddedAt = addedAt.ToString("O"),
            Meta = new() { Ext = pk.Extension, Tag = "évent 🎮", SourceSave = "Trainer's save" }
        }).ToArray();
        var (service, context) = CreateService(raw, legacyLevel);
        using var _ = context;
        await using var disposable = service;

        var entries = await service.GetAllAsync();

        entries.Select(entry => entry.Id).Should().Equal(10, 11, 12);
        for (var i = 0; i < entries.Count; i++)
        {
            GetStoredData(entries[i].Pokemon).Should().Equal(stored[i]);
            entries[i].Pokemon.GetType().Should().Be(pokemon[i].GetType());
            entries[i].Tag.Should().Be(raw[i].Meta.Tag);
            entries[i].SourceSave.Should().Be(raw[i].Meta.SourceSave);
            entries[i].AddedAt.Should().Be(addedAt);
            entries[i].AddedAt.Offset.Should().Be(addedAt.Offset);
            raw[i].BytesBase64.Should().Be(Convert.ToBase64String(stored[i]));
        }
    }

    [Fact]
    public async Task GetAllAsync_StorageFailure_PropagatesWithoutLegacyReadOrDelete()
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule("./js/bank.js");
        module.Setup<byte[]>("exportAll").SetException(new JSException("IndexedDB transaction failed."));
        await using var service = new BankService(context.Services.GetRequiredService<IJSRuntime>());

        var read = () => service.GetAllAsync();

        await read.Should().ThrowAsync<JSException>().WithMessage("IndexedDB transaction failed.");
        module.Invocations.Should().NotContain(invocation => invocation.Identifier == "getAllPokemonJson" || invocation.Identifier == "deletePokemon");
    }

    [Fact]
    public async Task GetAllAsync_MalformedJson_PropagatesWithoutDeletingRecords()
    {
        using var context = new BunitContext();
        var module = context.JSInterop.SetupModule("./js/bank.js");
        module.Setup<byte[]>("exportAll").SetResult("not-json"u8.ToArray());
        await using var service = new BankService(context.Services.GetRequiredService<IJSRuntime>());

        var read = () => service.GetAllAsync();

        await read.Should().ThrowAsync<JsonException>();
        module.Invocations.Should().NotContain(invocation => invocation.Identifier == "getAllPokemonJson" || invocation.Identifier == "deletePokemon");
    }

    [Fact]
    public async Task GetAllAsync_EmptyBank_ReturnsEmptyList()
    {
        var (service, ctx) = CreateService();

        var result = await service.GetAllAsync();

        result.Should().BeEmpty();

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task IsDuplicateAsync_EmptyBank_ReturnsFalse()
    {
        var (service, ctx) = CreateService();

        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5"));
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();

        var result = await service.IsDuplicateAsync(pkm!);

        result.Should().BeFalse();

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task PartitionDuplicatesAsync_EmptyBank_AllReturnedAsUnique()
    {
        var (service, ctx) = CreateService();

        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5"));
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();

        var (unique, duplicates) = await service.PartitionDuplicatesAsync([pkm!]);

        unique.Should().ContainSingle();
        duplicates.Should().BeEmpty();

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task PartitionDuplicatesAsync_BankContainsPkm_DetectedAsDuplicate()
    {
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5"));
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();
        pkm.Should().NotBeNull();

        // Seed the bank with a RawEntry whose bytesBase64 matches the test PKM.
        var rawEntry = new BankService.RawEntry
        {
            Id = 1,
            BytesBase64 = Convert.ToBase64String(GetStoredData(pkm)),
            Meta = new BankService.RawMeta
            {
                Species = pkm.Species,
                IsShiny = pkm.IsShiny,
                Nickname = pkm.Nickname,
                Ext = pkm.Extension,
                Tag = null
            },
            AddedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var (service, ctx) = CreateService([rawEntry]);

        var (unique, duplicates) = await service.PartitionDuplicatesAsync([pkm]);

        unique.Should().BeEmpty();
        duplicates.Should().ContainSingle();

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task ExportAsync_ReturnsExpectedBytes()
    {
        var expectedBytes = "test json"u8.ToArray();

        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop
            .SetupModule("./js/bank.js")
            .Setup<byte[]>("exportAll")
            .SetResult(expectedBytes);

        var service = new BankService(ctx.Services.GetRequiredService<IJSRuntime>());

        var result = await service.ExportAsync();

        result.Should().BeEquivalentTo(expectedBytes);

        await service.DisposeAsync();
        ctx.Dispose();
    }
}
