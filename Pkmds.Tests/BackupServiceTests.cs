using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Pkmds.Tests;

/// <summary>
/// Unit tests for <see cref="BackupService" /> using bUnit's JS-interop mocks.
/// <see cref="BackupService.RawBackupEntry" /> and <see cref="BackupService.RawBackupMeta" />
/// are <c>internal</c> and accessible here via <c>InternalsVisibleTo</c> in Pkmds.Rcl.csproj.
/// The backup JS contract passes JSON strings across the IJS boundary (trim-safe);
/// these tests mirror that by serializing entries with the service's own source-gen
/// context before handing them to the mock.
/// </summary>
public class BackupServiceTests
{
    private static BackupService.RawBackupEntry BuildEntry(
        long id = 1,
        string? bytes = null,
        string fileName = "test.sav",
        string saveType = "SAV9SV",
        int generation = 9,
        string gameVersion = "Scarlet",
        string trainerName = "Trainer",
        long sizeBytes = 100,
        bool isManicEmu = false,
        string createdAt = "2026-05-14T10:00:00.0000000+00:00",
        string source = "manual") => new()
        {
            Id = id,
            BytesBase64 = bytes,
            Meta = new BackupService.RawBackupMeta
            {
                FileName = fileName,
                SaveType = saveType,
                Generation = generation,
                GameVersion = gameVersion,
                TrainerName = trainerName,
                SizeBytes = sizeBytes,
                IsManicEmu = isManicEmu
            },
            CreatedAt = createdAt,
            Source = source
        };

    private static (BackupService Service, BunitContext Ctx) CreateService(
        BackupService.RawBackupEntry[]? metadata = null,
        (long Id, BackupService.RawBackupEntry? Entry)? singleBackup = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var module = ctx.JSInterop.SetupModule("./js/backup.js");

        var metaJson = JsonSerializer.Serialize(
            metadata ?? [],
            BackupService.BackupJsonContext.Default.RawBackupEntryArray);
        module.Setup<string>("getBackupMetadataJson").SetResult(metaJson);

        if (singleBackup is { } pair)
        {
            var singleJson = pair.Entry is null
                ? null
                : JsonSerializer.Serialize(
                    pair.Entry,
                    BackupService.BackupJsonContext.Default.RawBackupEntry);
            module.Setup<string?>("getBackupJson", pair.Id).SetResult(singleJson);
        }

        var jsRuntime = ctx.Services.GetRequiredService<IJSRuntime>();
        var service = new BackupService(jsRuntime);
        return (service, ctx);
    }

    [Fact]
    public async Task GetAllMetadataAsync_NoBackups_ReturnsEmptyList()
    {
        var (service, ctx) = CreateService();

        var result = await service.GetAllMetadataAsync();

        result.Should().BeEmpty();

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task GetAllMetadataAsync_PopulatedBackups_DeserializesAllFields()
    {
        var entries = new[]
        {
            BuildEntry(
                id: 42,
                fileName: "Pokemon Scarlet.sav",
                saveType: "SAV9SV",
                generation: 9,
                gameVersion: "Scarlet",
                trainerName: "Yeniel",
                sizeBytes: 3_133_440,
                createdAt: "2026-05-14T14:30:00.0000000+00:00",
                source: "auto"),
            BuildEntry(
                id: 43,
                fileName: "manic_export.zip",
                saveType: "SAV3E",
                generation: 3,
                gameVersion: "Emerald",
                trainerName: "John",
                sizeBytes: 131_072,
                isManicEmu: true,
                createdAt: "2026-05-14T15:00:00.0000000+00:00",
                source: "manual")
        };

        var (service, ctx) = CreateService(entries);

        var result = await service.GetAllMetadataAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(42);
        result[0].FileName.Should().Be("Pokemon Scarlet.sav");
        result[0].SaveType.Should().Be("SAV9SV");
        result[0].Generation.Should().Be(9);
        result[0].GameVersion.Should().Be("Scarlet");
        result[0].TrainerName.Should().Be("Yeniel");
        result[0].SizeBytes.Should().Be(3_133_440);
        result[0].IsManicEmu.Should().BeFalse();
        result[0].Source.Should().Be("auto");

        result[1].Id.Should().Be(43);
        result[1].SaveType.Should().Be("SAV3E");
        result[1].IsManicEmu.Should().BeTrue();
        result[1].Source.Should().Be("manual");

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task GetAllMetadataAsync_MalformedCreatedAt_FallsBackToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var entries = new[] { BuildEntry(createdAt: "not a real timestamp") };

        var (service, ctx) = CreateService(entries);

        var result = await service.GetAllMetadataAsync();

        result.Should().ContainSingle();
        result[0].CreatedAt.Should().BeOnOrAfter(before);

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task GetBackupBytesAsync_MissingRecord_ReturnsNull()
    {
        var (service, ctx) = CreateService(singleBackup: (Id: 99, Entry: null));

        var result = await service.GetBackupBytesAsync(99);

        result.Should().BeNull();

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task GetBackupBytesAsync_ValidRecord_RoundTripsBytes()
    {
        var payload = "the save bytes"u8.ToArray();
        var entries = BuildEntry(id: 7, bytes: Convert.ToBase64String(payload));

        var (service, ctx) = CreateService(singleBackup: (Id: 7, Entry: entries));

        var result = await service.GetBackupBytesAsync(7);

        result.Should().BeEquivalentTo(payload);

        await service.DisposeAsync();
        ctx.Dispose();
    }

    [Fact]
    public async Task GetBackupBytesAsync_InvalidBase64_ReturnsNull()
    {
        var entries = BuildEntry(id: 8, bytes: "not valid base64 !!!");

        var (service, ctx) = CreateService(singleBackup: (Id: 8, Entry: entries));

        var result = await service.GetBackupBytesAsync(8);

        result.Should().BeNull();

        await service.DisposeAsync();
        ctx.Dispose();
    }
}
