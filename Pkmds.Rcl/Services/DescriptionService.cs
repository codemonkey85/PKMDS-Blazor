// ReSharper disable ClassNeverInstantiated.Local

namespace Pkmds.Rcl.Services;

public sealed partial class DescriptionService(HttpClient http, ILogger<DescriptionService> logger) : IDescriptionService
{
    // -------------------------------------------------------------------------
    // Lazy loaders
    // -------------------------------------------------------------------------

    private const string DataRoot = "_content/Pkmds.Rcl/data/";

    // Lazily loaded caches — store the Task so all concurrent callers share one HTTP request
    // rather than each firing their own (which causes silent failures under load in WASM).
    private Task<Dictionary<string, JsonAbilityEntry>>? abilitiesTask;
    private Task<Dictionary<string, Dictionary<string, string>>>? hmDataTask;
    private Task<Dictionary<string, JsonItemEntry>>? itemsTask;
    private Task<Dictionary<string, JsonMoveEntry>>? movesTask;
    private Task<Dictionary<string, Dictionary<string, string>>>? tmDataTask;

    // Secondary index of items keyed by a punctuation-insensitive normalization of the name,
    // built lazily alongside the main items dict. Lets GetItemInfoAsync fall back when PKHeX's
    // spelling differs from PokeAPI's in whitespace/hyphens/apostrophes/accents/etc.
    private Dictionary<string, JsonItemEntry>? itemsByNormalizedKey;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public async Task<MoveSummary?> GetMoveInfoAsync(int moveId, GameVersion version)
    {
        var moves = await GetMovesAsync().ConfigureAwait(false);
        if (!moves.TryGetValue(moveId.ToString(), out var entry))
        {
            return null;
        }

        var targetVg = ToVersionGroup(version);
        var epoch = ResolveEpoch(entry.Stats, targetVg);
        var description = ResolveFlavor(entry.Flavor, targetVg) is { Length: > 0 } flavor
            ? flavor
            : entry.Description;

        MoveSecondaryEffects? secondaryEffects = null;
        if (entry.Meta is { } m)
        {
            secondaryEffects = new MoveSecondaryEffects(
                m.AilmentId,
                m.AilmentName,
                m.AilmentChance,
                m.FlinchChance,
                m.Drain,
                m.Healing,
                m.MinHits,
                m.MaxHits,
                m.CritRate,
                m.StatChance,
                m.StatChanges ?? []);
        }

        return new MoveSummary(
            entry.Name,
            epoch?.Type ?? string.Empty,
            epoch?.Category ?? string.Empty,
            epoch?.Power,
            epoch?.Pp,
            epoch?.Accuracy,
            description,
            entry.Target ?? string.Empty,
            entry.Flags ?? [],
            entry.Priority,
            secondaryEffects);
    }

    public async Task<AbilitySummary?> GetAbilityInfoAsync(int abilityId, GameVersion version)
    {
        var abilities = await GetAbilitiesAsync().ConfigureAwait(false);
        if (!abilities.TryGetValue(abilityId.ToString(), out var entry))
        {
            return null;
        }

        var description = ResolveFlavor(entry.Flavor, ToVersionGroup(version)) is { Length: > 0 } flavor
            ? flavor
            : entry.Description;

        return new AbilitySummary(entry.Name, description);
    }

    public async Task<string?> GetTmMoveNameAsync(string tmNumber, GameVersion version)
    {
        var tmData = await GetTmDataAsync().ConfigureAwait(false);
        var key = ToTmDataKey(version);
        if (key is null || !tmData.TryGetValue(key, out var versionData))
        {
            return null;
        }

        return versionData.GetValueOrDefault(tmNumber);
    }

    public async Task<string?> GetHmMoveNameAsync(string hmKey, GameVersion version)
    {
        var hmData = await GetHmDataAsync().ConfigureAwait(false);
        var key = ToHmDataKey(version);
        if (key is null || !hmData.TryGetValue(key, out var versionData))
        {
            return null;
        }

        return versionData.GetValueOrDefault(hmKey);
    }

    public async Task<ItemSummary?> GetItemInfoAsync(string itemName, GameVersion version)
    {
        var items = await GetItemsAsync().ConfigureAwait(false);
        var key = itemName.Trim().ToLowerInvariant();
        if (!items.TryGetValue(key, out var entry))
        {
            // PKHeX item names sometimes differ from PokeAPI's by whitespace, hyphens,
            // apostrophe style (straight vs curly), accents (é vs e), or Gen 1/2 CamelCase
            // (e.g. BlackGlasses, NeverMeltIce, SquirtBottle). Fall back to a punctuation-
            // insensitive lookup so these still render instead of showing "No description".
            itemsByNormalizedKey ??= BuildNormalizedItemIndex(items);
            if (!itemsByNormalizedKey.TryGetValue(NormalizeItemKey(itemName), out entry))
            {
                return null;
            }
        }

        var description = ResolveFlavor(entry.Flavor, ToVersionGroup(version)) is { Length: > 0 } flavor
            ? flavor
            : entry.Description;

        return new ItemSummary(entry.Name, description);
    }

    private Task<Dictionary<string, JsonAbilityEntry>> GetAbilitiesAsync() =>
        abilitiesTask ??= LoadAsync(DataRoot + "ability-info.json", DescriptionJsonContext.Default.Abilities);

    private Task<Dictionary<string, JsonMoveEntry>> GetMovesAsync() =>
        movesTask ??= LoadAsync(DataRoot + "move-info.json", DescriptionJsonContext.Default.Moves);

    private Task<Dictionary<string, JsonItemEntry>> GetItemsAsync() =>
        itemsTask ??= LoadAsync(DataRoot + "item-info.json", DescriptionJsonContext.Default.Items);

    private Task<Dictionary<string, Dictionary<string, string>>> GetTmDataAsync() =>
        tmDataTask ??= LoadAsync(DataRoot + "tm-data.json", DescriptionJsonContext.Default.MachineData);

    private Task<Dictionary<string, Dictionary<string, string>>> GetHmDataAsync() =>
        hmDataTask ??= LoadAsync(DataRoot + "hm-data.json", DescriptionJsonContext.Default.MachineData);

    private async Task<T> LoadAsync<T>(string path, JsonTypeInfo<T> typeInfo) where T : new()
    {
        try
        {
            await using var stream = await http.GetStreamAsync(path);
            return await JsonSerializer.DeserializeAsync(stream, typeInfo) ?? new T();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load description data from {Path}", path);
            return new T();
        }
    }

    // -------------------------------------------------------------------------
    // Version-group resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Maps a PKHeX <see cref="GameVersion" /> to the hm-data.json key for that game's HM list.
    /// Returns null for games without HMs (Gen 7+, PLA, GCN games).
    /// </summary>
    private static string? ToHmDataKey(GameVersion version) => version switch
    {
        GameVersion.RD or GameVersion.GN or GameVersion.BU
            or GameVersion.RB or GameVersion.RBY or GameVersion.YW
            or GameVersion.Gen1 => "gen1",
        GameVersion.GD or GameVersion.SI or GameVersion.C
            or GameVersion.GS or GameVersion.GSC or GameVersion.Gen2 => "gen2",
        GameVersion.R or GameVersion.S or GameVersion.E
            or GameVersion.RS or GameVersion.RSE => "gen3rse",
        GameVersion.FR or GameVersion.LG or GameVersion.FRLG => "gen3frlg",
        GameVersion.D or GameVersion.P or GameVersion.Pt or GameVersion.DP => "gen4dpp",
        GameVersion.HG or GameVersion.SS or GameVersion.HGSS => "gen4hgss",
        GameVersion.B or GameVersion.W or GameVersion.B2 or GameVersion.W2
            or GameVersion.BW or GameVersion.B2W2 or GameVersion.Gen5 => "gen5",
        GameVersion.X or GameVersion.Y or GameVersion.XY => "gen6xy",
        GameVersion.OR or GameVersion.AS or GameVersion.ORAS => "gen6oras",
        _ => null // Gen 7+, PLA, GCN — no HMs
    };

    /// <summary>
    /// Maps a PKHeX <see cref="GameVersion" /> to the tm-data.json key for that game's TM list.
    /// Returns null for games without a standard TM system (e.g. PLA).
    /// </summary>
    private static string? ToTmDataKey(GameVersion version) => version switch
    {
        GameVersion.RD or GameVersion.GN or GameVersion.BU
            or GameVersion.RB or GameVersion.RBY or GameVersion.YW
            or GameVersion.Gen1 => "gen1",
        GameVersion.GD or GameVersion.SI or GameVersion.C
            or GameVersion.GS or GameVersion.GSC or GameVersion.Gen2 => "gen2",
        GameVersion.R or GameVersion.S or GameVersion.E
            or GameVersion.FR or GameVersion.LG
            or GameVersion.RS or GameVersion.RSE or GameVersion.FRLG
            or GameVersion.CXD or GameVersion.COLO or GameVersion.XD => "gen3",
        GameVersion.D or GameVersion.P or GameVersion.Pt
            or GameVersion.HG or GameVersion.SS
            or GameVersion.DP or GameVersion.HGSS or GameVersion.Gen4 => "gen4",
        GameVersion.B or GameVersion.W or GameVersion.B2 or GameVersion.W2
            or GameVersion.BW or GameVersion.B2W2 or GameVersion.Gen5 => "gen5",
        GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS
            or GameVersion.XY or GameVersion.ORAS or GameVersion.Gen6 => "gen6",
        GameVersion.SN or GameVersion.MN or GameVersion.US or GameVersion.UM
            or GameVersion.SM or GameVersion.USUM or GameVersion.Gen7 => "gen7sm",
        GameVersion.GP or GameVersion.GE or GameVersion.GG
            or GameVersion.Gen7b => "gen7lgpe",
        GameVersion.SW or GameVersion.SH or GameVersion.SWSH => "gen8swsh",
        GameVersion.BD or GameVersion.SP or GameVersion.BDSP => "gen8bdsp",
        GameVersion.PLA or GameVersion.Gen8 => null, // no standard TM system
        GameVersion.SL or GameVersion.VL or GameVersion.SV
            or GameVersion.Gen9 => "gen9sv",
        GameVersion.ZA => "gen9za",
        _ => null
    };

    /// <summary>
    /// Maps a PKHeX <see cref="GameVersion" /> to the corresponding PokeAPI version-group ID.
    /// The version-group IDs match those in the PokeAPI CSV data used to generate the JSON files.
    /// </summary>
    private static int ToVersionGroup(GameVersion version) => version switch
    {
        GameVersion.RD or GameVersion.GN or GameVersion.BU
            or GameVersion.RB or GameVersion.RBY or GameVersion.Gen1 => 1, // red-blue
        GameVersion.YW => 2, // yellow
        GameVersion.GD or GameVersion.SI or GameVersion.GS => 3, // gold-silver
        GameVersion.C or GameVersion.GSC or GameVersion.Gen2 => 4, // crystal
        GameVersion.R or GameVersion.S or GameVersion.RS or GameVersion.RSE => 5, // ruby-sapphire
        GameVersion.E => 6, // emerald
        GameVersion.FR or GameVersion.LG or GameVersion.FRLG => 7, // firered-leafgreen
        GameVersion.CXD or GameVersion.COLO or GameVersion.XD => 13, // xd (closest GCN match)
        GameVersion.D or GameVersion.P or GameVersion.DP => 8, // diamond-pearl
        GameVersion.Pt => 9, // platinum
        GameVersion.HG or GameVersion.SS or GameVersion.HGSS or GameVersion.Gen4 => 10, // heartgold-soulsilver
        GameVersion.B or GameVersion.W or GameVersion.BW => 11, // black-white
        GameVersion.B2 or GameVersion.W2 or GameVersion.B2W2 or GameVersion.Gen5 => 14, // black-2-white-2
        GameVersion.X or GameVersion.Y or GameVersion.XY => 15, // x-y
        GameVersion.OR or GameVersion.AS or GameVersion.ORAS or GameVersion.Gen6 => 16, // omega-ruby-alpha-sapphire
        GameVersion.SN or GameVersion.MN or GameVersion.SM => 17, // sun-moon
        GameVersion.US or GameVersion.UM or GameVersion.USUM or GameVersion.Gen7 => 18, // ultra-sun-ultra-moon
        GameVersion.GP or GameVersion.GE or GameVersion.GG or GameVersion.Gen7b => 19, // lets-go
        GameVersion.SW or GameVersion.SH or GameVersion.SWSH => 20, // sword-shield
        GameVersion.BD or GameVersion.SP or GameVersion.BDSP => 23, // brilliant-diamond-shining-pearl
        GameVersion.PLA => 24, // legends-arceus
        GameVersion.Gen8 => 24, // latest gen 8 (PLA)
        GameVersion.SL or GameVersion.VL or GameVersion.SV or GameVersion.Gen9 => 25, // scarlet-violet
        GameVersion.ZA => 30, // legends-za
        GameVersion.CP => 31, // mega-dimension
        _ => 25 // default to latest known
    };

    /// <summary>
    /// Produces a lookup key that strips Unicode accents, non-alphanumerics, and case so
    /// that PKHeX spellings like "BlackGlasses", "King's Rock", "Fresh-Start Mochi",
    /// "Exp Share", and "Poké Ball" all collapse to the same key as their item-info.json
    /// counterparts ("black glasses", "king's rock", "fresh start mochi", "exp. share",
    /// "poké ball"). Used only as a last-resort fallback — the exact-key match still wins
    /// first.
    /// </summary>
    private static string NormalizeItemKey(string name)
    {
        var decomposed = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            // char.IsLetterOrDigit on decomposed-form é returns true for 'e' and false for
            // the combining acute accent, so this effectively strips diacritics.
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }
        return sb.ToString();
    }

    private static Dictionary<string, JsonItemEntry> BuildNormalizedItemIndex(Dictionary<string, JsonItemEntry> items)
    {
        var map = new Dictionary<string, JsonItemEntry>(items.Count, StringComparer.Ordinal);
        foreach (var (key, entry) in items)
        {
            var nkey = NormalizeItemKey(key);
            if (nkey.Length > 0)
            {
                // On collision, first-write wins; primary-key exact matches still take
                // priority because the normalized index is only consulted on miss.
                map.TryAdd(nkey, entry);
            }
        }
        return map;
    }

    /// <summary>
    /// Returns the flavor text entry with the largest version-group ID that is ≤
    /// <paramref name="targetVg" />. Falls back to the smallest available entry if
    /// none qualifies (e.g., the target game predates all entries).
    /// </summary>
    private static string? ResolveFlavor(Dictionary<string, string>? flavor, int targetVg)
    {
        if (flavor is null or { Count: 0 })
        {
            return null;
        }

        string? best = null;
        var bestVg = -1;
        var smallestVg = int.MaxValue;
        string? smallestEntry = null;

        foreach (var (key, text) in flavor)
        {
            if (!int.TryParse(key, out var vg))
            {
                continue;
            }

            if (vg <= targetVg && vg > bestVg)
            {
                bestVg = vg;
                best = text;
            }

            if (vg >= smallestVg)
            {
                continue;
            }

            smallestVg = vg;
            smallestEntry = text;
        }

        return best ?? smallestEntry;
    }

    /// <summary>
    /// From the stat epochs array (sorted ascending by fromVersionGroup), returns the epoch
    /// whose <c>fromVersionGroup</c> is the largest value ≤ <paramref name="targetVg" />.
    /// Falls back to the first epoch if the target predates all epochs.
    /// </summary>
    private static JsonMoveStatEpoch? ResolveEpoch(List<JsonMoveStatEpoch>? epochs, int targetVg)
    {
        if (epochs is null or { Count: 0 })
        {
            return null;
        }

        JsonMoveStatEpoch? best = null;
        foreach (var epoch in epochs)
        {
            if (epoch.FromVersionGroup <= targetVg)
            {
                best = epoch;
            }
        }

        return best ?? epochs[0];
    }

    // -------------------------------------------------------------------------
    // JSON deserialization models (internal)
    // -------------------------------------------------------------------------

    private sealed record JsonAbilityEntry(
        string Name,
        string Description,
        Dictionary<string, string>? Flavor);

    private sealed record JsonMoveEntry(
        string Name,
        string Description,
        string? Target,
        List<string>? Flags,
        List<JsonMoveStatEpoch> Stats,
        Dictionary<string, string>? Flavor,
        int Priority = 0,
        JsonMoveMeta? Meta = null);

    private sealed record JsonMoveStatEpoch(
        int FromVersionGroup,
        string Type,
        string Category,
        int? Power,
        int? Pp,
        int? Accuracy);

    private sealed record JsonMoveMeta(
        int AilmentId = 0,
        string? AilmentName = null,
        int AilmentChance = 0,
        int FlinchChance = 0,
        int Drain = 0,
        int Healing = 0,
        int? MinHits = null,
        int? MaxHits = null,
        int CritRate = 0,
        int StatChance = 0,
        List<MoveStatChange>? StatChanges = null);

    private sealed record JsonItemEntry(
        string Name,
        string Description,
        Dictionary<string, string>? Flavor);

    // Source-generated JSON context for the four data shapes loaded from
    // wwwroot/data/*.json. Nested so it can reference the private records
    // above. PropertyNameCaseInsensitive matches the prior JsonSerializerOptions
    // behavior (data files are camelCase; record properties are PascalCase).
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(Dictionary<string, JsonAbilityEntry>), TypeInfoPropertyName = "Abilities")]
    [JsonSerializable(typeof(Dictionary<string, JsonMoveEntry>), TypeInfoPropertyName = "Moves")]
    [JsonSerializable(typeof(Dictionary<string, JsonItemEntry>), TypeInfoPropertyName = "Items")]
    [JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>), TypeInfoPropertyName = "MachineData")]
    private sealed partial class DescriptionJsonContext : JsonSerializerContext;
}
