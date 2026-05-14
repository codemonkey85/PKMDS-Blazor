namespace Pkmds.Rcl.Services;

public partial class BankService(IJSRuntime js) : IBankService, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            await module.DisposeAsync();
        }
    }

    public async Task AddAsync(PKM pkm, string? tag = null, string? sourceSave = null)
    {
        await GetModuleAsync();
        var storedData = new byte[pkm.SIZE_STORED];
        pkm.WriteDecryptedDataStored(storedData);
        var b64 = Convert.ToBase64String(storedData);
        var meta = new BankMeta(
            pkm.Species,
            pkm.IsShiny,
            pkm.Nickname,
            pkm.Extension,
            tag,
            sourceSave);
        // Serialize via source-gen on our side, then hand IJS a JsonDocument.RootElement
        // (a trim-safe framework primitive). Blazor's marshaler writes the element's
        // underlying JSON structure across the boundary, so JS receives a real object —
        // not a string. That matters because if a service-worker rollout briefly leaves
        // a user with a stale cached bank.js (no parseMeta), an opaque string payload
        // would land in IndexedDB as the meta field, breaking the species/isShiny/tag
        // indexes and all subsequent reads.
        if (module is null)
        {
            return;
        }

        using var metaDoc = JsonSerializer.SerializeToDocument(meta, BankJsonContext.Default.BankMeta);
        await module.InvokeVoidAsync("addPokemon", b64, metaDoc.RootElement);
    }

    public async Task AddRangeAsync(IEnumerable<PKM> pokemon, string? tag = null, string? sourceSave = null)
    {
        await GetModuleAsync();
        // Collect all entries first and send in one JS call / one IDB transaction
        // rather than one round-trip per Pokémon.
        var entries = pokemon.Select(pkm =>
        {
            var storedData = new byte[pkm.SIZE_STORED];
            pkm.WriteDecryptedDataStored(storedData);
            return new BankAddEntry(
                Convert.ToBase64String(storedData),
                new BankMeta(
                    pkm.Species,
                    pkm.IsShiny,
                    pkm.Nickname,
                    pkm.Extension,
                    tag,
                    sourceSave));
        }).ToArray();

        if (entries.Length > 0)
        {
            if (module is null)
            {
                return;
            }

            // JsonDocument.RootElement over the array — see AddAsync for why we avoid
            // sending an opaque string payload across IJS during a SW rollout window.
            using var entriesDoc = JsonSerializer.SerializeToDocument(entries, BankJsonContext.Default.BankAddEntryArray);
            await module.InvokeVoidAsync("addRange", entriesDoc.RootElement);
        }
    }

    public async Task<IReadOnlyList<BankEntry>> GetAllAsync()
    {
        await GetModuleAsync();
        if (module is null)
        {
            return [];
        }

        // Prefer the *Json variant (JSON string → source-gen deserialize). If a stale
        // cached bank.js without the *Json export is served during a service-worker
        // rollout, getAllPokemonJson will throw "is not a function"; fall back to the
        // legacy getAllPokemon (returns the raw array, marshalled here as JsonElement
        // — a trim-safe primitive — then source-gen deserialized).
        RawEntry[] raw;
        try
        {
            var rawJson = await module.InvokeAsync<string>("getAllPokemonJson");
            if (string.IsNullOrEmpty(rawJson))
            {
                return [];
            }
            raw = JsonSerializer.Deserialize(rawJson, BankJsonContext.Default.RawEntryArray) ?? [];
        }
        catch (JSException ex) when (ex.Message.Contains("is not a function", StringComparison.OrdinalIgnoreCase))
        {
            var element = await module.InvokeAsync<JsonElement>("getAllPokemon");
            raw = element.ValueKind == JsonValueKind.Array
                ? element.Deserialize(BankJsonContext.Default.RawEntryArray) ?? []
                : [];
        }

        if (raw.Length == 0)
        {
            return [];
        }

        var results = new List<BankEntry>(raw.Length);
        var invalidIds = new List<long>();

        foreach (var r in raw)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(r.BytesBase64);
            }
            catch (FormatException)
            {
                // Record contains invalid base64 data — schedule it for deletion so it
                // doesn't prevent the rest of the bank from loading.
                invalidIds.Add(r.Id);
                continue;
            }

            if (!FileUtil.TryGetPKM(bytes, out var pkm, r.Meta.Ext))
            {
                // Record is corrupt or uses an unsupported format — schedule it for
                // deletion so it doesn't accumulate as an undeletable ghost entry.
                invalidIds.Add(r.Id);
                continue;
            }

            var speciesName = GameInfo.Strings.Species.Count > pkm.Species
                ? GameInfo.Strings.Species[pkm.Species]
                : string.Empty;

            if (!DateTimeOffset.TryParse(r.AddedAt, out var addedAt))
            {
                addedAt = DateTimeOffset.UtcNow;
            }

            results.Add(new BankEntry
            {
                Id = r.Id,
                Pokemon = pkm,
                SpeciesName = string.IsNullOrEmpty(speciesName)
                    ? "Unknown"
                    : speciesName,
                Tag = r.Meta.Tag,
                SourceSave = r.Meta.SourceSave,
                AddedAt = addedAt
            });
        }

        // Proactively clean up any records that could not be rehydrated.
        foreach (var id in invalidIds)
        {
            await DeleteAsync(id);
        }

        return results;
    }

    public async Task DeleteAsync(long id)
    {
        await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync("deletePokemon", id);
    }

    public async Task ClearAsync()
    {
        await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync("clearAll");
    }

    public async Task<byte[]> ExportAsync()
    {
        await GetModuleAsync();
        // JS exportAll() returns a Uint8Array; Blazor marshals it directly to byte[].
        if (module is null)
        {
            return [];
        }

        return await module.InvokeAsync<byte[]>("exportAll");
    }

    public async Task ImportAsync(byte[] data)
    {
        await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync("importAll", data);
    }

    public async Task<bool> IsDuplicateAsync(PKM pkm)
    {
        var all = await GetAllAsync();
        var candidateBytes = new byte[pkm.SIZE_STORED];
        pkm.WriteDecryptedDataStored(candidateBytes);
        return all.Any(entry =>
        {
            var entryBytes = new byte[entry.Pokemon.SIZE_STORED];
            entry.Pokemon.WriteDecryptedDataStored(entryBytes);
            return entryBytes.AsSpan().SequenceEqual(candidateBytes);
        });
    }

    public async Task<(IReadOnlyList<PKM> Unique, IReadOnlyList<PKM> Duplicates)> PartitionDuplicatesAsync(
        IEnumerable<PKM> candidates)
    {
        var all = await GetAllAsync();

        // Build a hash set of existing entries' stored data (base64) for O(1) lookup.
        var existingHashes = all
            .Select(e =>
            {
                var data = new byte[e.Pokemon.SIZE_STORED];
                e.Pokemon.WriteDecryptedDataStored(data);
                return Convert.ToBase64String(data);
            })
            .ToHashSet();

        var unique = new List<PKM>();
        var duplicates = new List<PKM>();

        foreach (var pkm in candidates)
        {
            var storedBytes = new byte[pkm.SIZE_STORED];
            pkm.WriteDecryptedDataStored(storedBytes);
            if (existingHashes.Contains(Convert.ToBase64String(storedBytes)))
            {
                duplicates.Add(pkm);
            }
            else
            {
                unique.Add(pkm);
            }
        }

        return (unique, duplicates);
    }

    private async Task GetModuleAsync() =>
        module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/bank.js");

    // ── DTOs for JS interop ──────────────────────────────────────────────
    // internal so that Pkmds.Tests can reference these types when setting up
    // JS interop mocks (via InternalsVisibleTo in Pkmds.Rcl.csproj).

    // Write-side payloads (replace anonymous types so source-gen can describe them).

    internal sealed record BankMeta(
        [property: JsonPropertyName("species")] ushort Species,
        [property: JsonPropertyName("isShiny")] bool IsShiny,
        [property: JsonPropertyName("nickname")] string Nickname,
        [property: JsonPropertyName("ext")] string Ext,
        [property: JsonPropertyName("tag")] string? Tag,
        [property: JsonPropertyName("sourceSave")] string? SourceSave);

    internal sealed record BankAddEntry(
        [property: JsonPropertyName("bytesBase64")] string BytesBase64,
        [property: JsonPropertyName("meta")] BankMeta Meta);

    // Read-side payloads. Mutable POCOs because the source-gen deserializer needs a
    // public parameterless constructor + settable properties to round-trip through
    // System.Text.Json on a JSON object with these property names.

    internal sealed class RawEntry
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("bytesBase64")]
        public string BytesBase64 { get; set; } = string.Empty;

        [JsonPropertyName("meta")]
        public RawMeta Meta { get; set; } = new();

        [JsonPropertyName("addedAt")]
        public string AddedAt { get; set; } = string.Empty;
    }

    internal sealed class RawMeta
    {
        [JsonPropertyName("species")]
        public ushort Species { get; set; }

        [JsonPropertyName("isShiny")]
        public bool IsShiny { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;

        [JsonPropertyName("ext")]
        public string Ext { get; set; } = string.Empty;

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("sourceSave")]
        public string? SourceSave { get; set; }
    }

    [JsonSerializable(typeof(BankMeta))]
    [JsonSerializable(typeof(BankAddEntry[]))]
    [JsonSerializable(typeof(RawEntry[]))]
    internal sealed partial class BankJsonContext : JsonSerializerContext;
}
