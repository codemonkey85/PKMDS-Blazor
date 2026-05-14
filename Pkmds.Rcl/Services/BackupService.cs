namespace Pkmds.Rcl.Services;

public partial class BackupService(IJSRuntime js) : IBackupService, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }

    public async Task<long> CreateBackupAsync(byte[] saveBytes, SaveFile saveFile, string? fileName, bool isManicEmu, string source)
    {
        var module = await GetModuleAsync();
        var b64 = Convert.ToBase64String(saveBytes);
        var meta = new BackupMeta(
            fileName ?? string.Empty,
            saveFile.GetType().Name,
            (int)saveFile.Generation,
            saveFile.Version.ToString(),
            saveFile.OT,
            saveBytes.Length,
            isManicEmu);
        // Serialize via source-gen on our side and pass a JSON string across the JS interop
        // boundary — see BankService for the rationale (trim-safe vs. reflection-based
        // Blazor JsonSerializer under TrimMode=full; see #894).
        var metaJson = JsonSerializer.Serialize(meta, BackupJsonContext.Default.BackupMeta);
        return await module.InvokeAsync<long>("addBackup", b64, metaJson, source);
    }

    public async Task<IReadOnlyList<BackupEntry>> GetAllMetadataAsync()
    {
        var module = await GetModuleAsync();
        // Call the *Json variant so we get a JSON string suitable for source-gen
        // deserialization. The legacy getBackupMetadata export still returns an
        // array for service-worker rollout safety; see backup.js.
        var rawJson = await module.InvokeAsync<string>("getBackupMetadataJson");

        if (string.IsNullOrEmpty(rawJson))
        {
            return [];
        }

        var raw = JsonSerializer.Deserialize(rawJson, BackupJsonContext.Default.RawBackupEntryArray);

        if (raw is null || raw.Length == 0)
        {
            return [];
        }

        var results = new List<BackupEntry>(raw.Length);
        foreach (var r in raw)
        {
            if (!DateTimeOffset.TryParse(r.CreatedAt, out var createdAt))
            {
                createdAt = DateTimeOffset.UtcNow;
            }

            results.Add(new BackupEntry
            {
                Id = r.Id,
                FileName = r.Meta?.FileName ?? string.Empty,
                SaveType = r.Meta?.SaveType ?? string.Empty,
                Generation = r.Meta?.Generation ?? 0,
                GameVersion = r.Meta?.GameVersion ?? string.Empty,
                TrainerName = r.Meta?.TrainerName ?? string.Empty,
                SizeBytes = r.Meta?.SizeBytes ?? 0,
                IsManicEmu = r.Meta?.IsManicEmu ?? false,
                CreatedAt = createdAt,
                Source = r.Source ?? string.Empty
            });
        }

        return results;
    }

    public async Task<byte[]?> GetBackupBytesAsync(long id)
    {
        var module = await GetModuleAsync();
        // *Json variant — see GetAllMetadataAsync for rationale.
        var rawJson = await module.InvokeAsync<string?>("getBackupJson", id);

        if (string.IsNullOrEmpty(rawJson))
        {
            return null;
        }

        var raw = JsonSerializer.Deserialize(rawJson, BackupJsonContext.Default.RawBackupEntry);

        if (raw?.BytesBase64 is null)
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(raw.BytesBase64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public async Task DeleteAsync(long id)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("deleteBackup", id);
    }

    public async Task ClearAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("clearAll");
    }

    public async Task EnforceRetentionAsync(int maxBackups)
    {
        var module = await GetModuleAsync();
        var count = await module.InvokeAsync<int>("getCount");
        if (count <= maxBackups)
        {
            return;
        }

        var excess = count - maxBackups;
        var oldestIds = await module.InvokeAsync<long[]>("getOldestIds", excess);
        if (oldestIds.Length > 0)
        {
            await module.InvokeVoidAsync("deleteMultiple", oldestIds);
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/backup.js");

    // ── DTOs for JS interop ──────────────────────────────────────────────
    // internal so that Pkmds.Tests can reference these types when setting up
    // JS interop mocks (via InternalsVisibleTo in Pkmds.Rcl.csproj).

    // Write-side payload (replaces the anonymous type so source-gen can describe it).

    internal sealed record BackupMeta(
        [property: JsonPropertyName("fileName")] string FileName,
        [property: JsonPropertyName("saveType")] string SaveType,
        [property: JsonPropertyName("generation")] int Generation,
        [property: JsonPropertyName("gameVersion")] string GameVersion,
        [property: JsonPropertyName("trainerName")] string TrainerName,
        [property: JsonPropertyName("sizeBytes")] long SizeBytes,
        [property: JsonPropertyName("isManicEmu")] bool IsManicEmu);

    // Read-side payloads. Mutable POCOs because the source-gen deserializer needs a
    // public parameterless constructor + settable properties to round-trip through
    // System.Text.Json on a JSON object with these property names.

    internal sealed class RawBackupEntry
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("bytesBase64")]
        public string? BytesBase64 { get; set; }

        [JsonPropertyName("meta")]
        public RawBackupMeta? Meta { get; set; }

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    internal sealed class RawBackupMeta
    {
        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("saveType")]
        public string? SaveType { get; set; }

        [JsonPropertyName("generation")]
        public int? Generation { get; set; }

        [JsonPropertyName("gameVersion")]
        public string? GameVersion { get; set; }

        [JsonPropertyName("trainerName")]
        public string? TrainerName { get; set; }

        [JsonPropertyName("sizeBytes")]
        public long? SizeBytes { get; set; }

        [JsonPropertyName("isManicEmu")]
        public bool? IsManicEmu { get; set; }
    }

    [JsonSerializable(typeof(BackupMeta))]
    [JsonSerializable(typeof(RawBackupEntry))]
    [JsonSerializable(typeof(RawBackupEntry[]))]
    internal sealed partial class BackupJsonContext : JsonSerializerContext;
}
