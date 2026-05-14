#!/usr/bin/env dotnet-run
#:property PublishAot=false
/*
 * Generate flat JSON description files from PokeAPI CSV data for PKMDS-Blazor.
 *
 * These files are consumed by DescriptionService in Pkmds.Rcl to power info
 * tooltips for moves, abilities, and items in the Pokémon editor and bag.
 *
 * Usage:
 *   dotnet run generate-descriptions.cs -- --pokeapi /path/to/pokeapi [--output /path/to/output]
 *
 * Arguments:
 *   --pokeapi   Path to the PokeAPI repo root, or directly to its data/v2/csv directory.
 *   --output    Output directory for the generated JSON files.
 *               Defaults to ../Pkmds.Rcl/wwwroot/data/ relative to this script.
 *
 * Output files:
 *   ability-info.json  — abilities indexed by PokeAPI numeric ID
 *   move-info.json     — moves indexed by PokeAPI numeric ID, with per-version-group stats
 *   item-info.json     — items indexed by lowercase English name (for cross-referencing with PKHeX)
 *
 * Optional --showdown path enables two supplements:
 *   moves: secondary effects (stat changes, status, flinch, drain, multi-hit, crit rate,
 *          wind/slicing flags) for moves PokeAPI's move_meta / move_meta_stat_changes CSV
 *          hasn't caught up on yet (mostly Gen 8+).
 *   items: shortDesc from data/text/items.ts used as a fallback description when PokeAPI's
 *          item_prose.csv has no English row for the item (common for Gen 9 held items).
 *
 * Optional --overrides path loads a description-overrides.json produced by
 * tools/scrape-pokemondb-descriptions.cs. Used as a last-resort description fallback
 * (applied after PokeAPI short_effect and Showdown shortDesc) for items and moves.
 * Auto-discovered at tools/data/description-overrides.json under the repo root if the
 * flag is omitted.
 *
 * Version-group changelog interpretation
 * ---------------------------------------
 * move_changelog stores the OLD value that was in effect BEFORE the named version group.
 * Reading entries for a given field sorted ascending by VG gives a chain:
 *     [(VG=3, V3), (VG=11, V11)]  with current value Vc
 * means:
 *     VG 1–2   → V3
 *     VG 3–10  → V11
 *     VG 11+   → Vc
 * This is implemented in FieldEpochs() and used to produce a compact epoch list in each
 * move's "stats" array. The service picks the entry with the largest fromVersionGroup ≤ target.
 */

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// ---------------------------------------------------------------------------
// Args
// ---------------------------------------------------------------------------

string? pokeapiArg = null;
string? outputArg = null;
string? showdownArg = null;
string? overridesArg = null;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--pokeapi" && i + 1 < args.Length) pokeapiArg = args[++i];
    else if (args[i] == "--output" && i + 1 < args.Length) outputArg = args[++i];
    else if (args[i] == "--showdown" && i + 1 < args.Length) showdownArg = args[++i];
    else if (args[i] == "--overrides" && i + 1 < args.Length) overridesArg = args[++i];
}

if (pokeapiArg is null)
{
    Console.Error.WriteLine("Usage: dotnet run tools/generate-descriptions.cs -- --pokeapi /path/to/pokeapi [--showdown /path/to/pokemon-showdown] [--overrides /path/to/description-overrides.json] [--output /path/to/output]");
    return 1;
}

// If no explicit --overrides path was given, auto-discover tools/data/description-overrides.json
// by walking up from the current working directory. Makes the common path "just work".
if (overridesArg is null)
{
    var walk = Environment.CurrentDirectory;
    while (walk is not null)
    {
        var candidate = Path.Combine(walk, "tools", "data", "description-overrides.json");
        if (File.Exists(candidate)) { overridesArg = candidate; break; }
        walk = Path.GetDirectoryName(walk);
    }
}

var pokeapiRoot = Path.GetFullPath(pokeapiArg);
var csvDir = Directory.Exists(Path.Combine(pokeapiRoot, "data"))
    ? Path.Combine(pokeapiRoot, "data", "v2", "csv")
    : pokeapiRoot;

if (!Directory.Exists(csvDir))
{
    Console.Error.WriteLine($"ERROR: CSV directory not found: {csvDir}");
    return 1;
}

var outputDir = outputArg is not null
    ? Path.GetFullPath(outputArg)
    : FindDefaultOutputDir();

Directory.CreateDirectory(outputDir);

Console.WriteLine($"Reading CSV from : {csvDir}");
Console.WriteLine($"Writing JSON to  : {outputDir}");
if (overridesArg is not null) Console.WriteLine($"Overrides from   : {overridesArg}");
Console.WriteLine();

// Load description overrides (from scrape-pokemondb-descriptions.cs and manual corrections, if present).
// Keys: item-info.json-style lowercase name; move-info.json-style numeric id; ability name (case-insensitive).
// Item/move overrides are last-resort fallbacks when PokeAPI/Showdown have no description.
// Ability description overrides always replace (used to correct upstream data bugs, not fill gaps).
// abilityFlavorRemove drops specific version-group flavor entries that PokeAPI mis-attributed.
var itemOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var moveOverrides = new Dictionary<string, string>(StringComparer.Ordinal);
var abilityOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var abilityFlavorRemove = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
if (overridesArg is not null && File.Exists(overridesArg))
{
    var overridesJson = JsonNode.Parse(File.ReadAllText(overridesArg))?.AsObject();
    if (overridesJson?["items"] is JsonObject itemsObj)
        foreach (var (k, v) in itemsObj)
            if (v is not null && v.GetValue<string>() is { Length: > 0 } s)
                itemOverrides[k] = s;
    if (overridesJson?["moves"] is JsonObject movesObj)
        foreach (var (k, v) in movesObj)
            if (v is not null && v.GetValue<string>() is { Length: > 0 } s)
                moveOverrides[k] = s;
    if (overridesJson?["abilities"] is JsonObject abilitiesObj)
        foreach (var (k, v) in abilitiesObj)
            if (v is not null && v.GetValue<string>() is { Length: > 0 } s)
                abilityOverrides[k] = s;
    if (overridesJson?["abilityFlavorRemove"] is JsonObject flavorRemoveObj)
        foreach (var (k, v) in flavorRemoveObj)
            if (v is JsonArray arr)
                abilityFlavorRemove[k] = [.. arr.OfType<JsonValue>().Select(x => x.GetValue<string>())];
    Console.WriteLine($"  → {itemOverrides.Count} item, {moveOverrides.Count} move, {abilityOverrides.Count} ability description overrides loaded ({abilityFlavorRemove.Count} ability flavor edits)");
    Console.WriteLine();
}

// Walk up from the working directory to find the repo root (contains Pkmds.Rcl/).
string FindDefaultOutputDir()
{
    var dir = Environment.CurrentDirectory;
    while (dir is not null)
    {
        var candidate = Path.Combine(dir, "Pkmds.Rcl", "wwwroot", "data");
        if (Directory.Exists(Path.Combine(dir, "Pkmds.Rcl")))
            return candidate;
        dir = Path.GetDirectoryName(dir);
    }
    throw new DirectoryNotFoundException("Could not find Pkmds.Rcl/ in any parent directory. Use --output to specify the output path.");
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const string English = "9";
string[] MoveStatFields = ["type_id", "power", "pp", "accuracy", "effect_id", "effect_chance"];

string StripMarkup(string text) =>
    Regex.Replace(text, @"\[([^\]]+)\]\{[^}]+\}", "$1");

string CleanText(string text)
{
    text = StripMarkup(text);
    text = Regex.Replace(text, "\u00ad[\r\n]+\\s*", "");   // soft hyphen before newline = word-wrap artifact
    text = text.Replace("\u00ad", "");                      // remaining bare soft hyphens
    text = Regex.Replace(text, @"[\r\n]+", " ");            // newlines → space
    text = Regex.Replace(text, @"[ \t]{2,}", " ");          // collapse spaces
    return text.Trim();
}

// Full CSV parser: handles quoted fields with embedded commas, newlines, and "" escapes.
List<List<string>> ParseCsv(string text)
{
    var records = new List<List<string>>();
    var field = new StringBuilder();
    var current = new List<string>();
    var inQuotes = false;
    var i = 0;
    while (i < text.Length)
    {
        var c = text[i];
        if (inQuotes)
        {
            if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i += 2; }
            else if (c == '"') { inQuotes = false; i++; }
            else { field.Append(c); i++; }
        }
        else
        {
            if (c == '"') { inQuotes = true; i++; }
            else if (c == ',') { current.Add(field.ToString()); field.Clear(); i++; }
            else if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                current.Add(field.ToString()); field.Clear();
                records.Add(current); current = []; i += 2;
            }
            else if (c == '\n')
            {
                current.Add(field.ToString()); field.Clear();
                records.Add(current); current = []; i++;
            }
            else { field.Append(c); i++; }
        }
    }
    if (field.Length > 0 || current.Count > 0)
    {
        current.Add(field.ToString());
        records.Add(current);
    }
    // Drop trailing empty record from trailing newline
    if (records.Count > 0 && records[^1] is [""]) records.RemoveAt(records.Count - 1);
    return records;
}

List<Dictionary<string, string>> ReadCsv(string path)
{
    var records = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
    if (records.Count == 0) return [];
    var headers = records[0];
    var result = new List<Dictionary<string, string>>(records.Count - 1);
    for (var i = 1; i < records.Count; i++)
    {
        var values = records[i];
        var row = new Dictionary<string, string>(headers.Count);
        for (var j = 0; j < headers.Count && j < values.Count; j++)
            row[headers[j]] = values[j];
        result.Add(row);
    }
    return result;
}

// Build { itemId: { versionGroupId: flavorText } } for English rows.
Dictionary<string, Dictionary<string, string>> EnFlavor(List<Dictionary<string, string>> rows, string idField)
{
    var result = new Dictionary<string, Dictionary<string, string>>();
    foreach (var row in rows)
    {
        if (!row.TryGetValue("language_id", out var lang) || lang != English) continue;
        var itemId = row[idField];
        var vg = row["version_group_id"];
        if (!result.TryGetValue(itemId, out var vgMap)) result[itemId] = vgMap = [];
        vgMap[vg] = CleanText(row["flavor_text"]);
    }
    return result;
}

JsonObject ToJsonObject(Dictionary<string, string> dict)
{
    var obj = new JsonObject();
    foreach (var (k, v) in dict) obj[k] = v;
    return obj;
}

// ---------------------------------------------------------------------------
// Showdown moves.ts parser
// ---------------------------------------------------------------------------
//
// Reads pokemon-showdown/data/moves.ts and extracts secondary effects for moves
// missing from PokeAPI's move_meta / move_meta_stat_changes CSV files.
// Only the fields that map to our JSON meta model are extracted.
//
// Handles:
//   secondary: { chance, status, boosts }  — volatileStatus only checked for 'flinch'
//   secondaries: [{ ... }, { ... }]
//   drain: [n, d]       recoil: [n, d]     heal: [n, d]
//   multihit: n         multihit: [min, max]
//   critRatio: n        status: 'xxx'  (top-level, for status moves)

/// <summary>
/// Walk forward from <paramref name="openPos"/> (which must be the opening char)
/// and return the index of the matching close char, respecting string literals and comments.
/// Returns -1 on failure.
/// </summary>
static int FindMatchingClose(string text, int openPos, char open, char close)
{
    int depth = 0;
    bool inStr = false;
    char strChar = '\0';
    for (int i = openPos; i < text.Length; i++)
    {
        char c = text[i];
        if (inStr)
        {
            if (c == '\\') { i++; continue; }
            if (c == strChar) inStr = false;
            continue;
        }
        if (c is '\'' or '"' or '`') { inStr = true; strChar = c; continue; }
        // single-line comment
        if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
        {
            while (i < text.Length && text[i] != '\n') i++;
            continue;
        }
        // multi-line comment
        if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
        {
            i += 2;
            while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
            i++;
            continue;
        }
        if (c == open) depth++;
        else if (c == close) { if (--depth == 0) return i; }
    }
    return -1;
}

/// <summary>
/// Extract the content between the balanced open/close for a named field within
/// <paramref name="text"/>.  Returns null if the field is absent or is <c>null</c>.
/// </summary>
static string? ExtractShowdownBlock(string text, string field, char open = '{', char close = '}')
{
    var m = Regex.Match(text, $@"\b{Regex.Escape(field)}:\s*([{Regex.Escape(open.ToString())}n])");
    if (!m.Success) return null;
    if (m.Groups[1].Value == "n") return null; // "null"
    int openIdx = text.IndexOf(open, m.Index + m.Length - 1);
    if (openIdx < 0) return null;
    int closeIdx = FindMatchingClose(text, openIdx, open, close);
    if (closeIdx < 0) return null;
    return text[(openIdx + 1)..closeIdx];
}

/// <summary>Split the content of a JS array into its top-level elements.</summary>
static IReadOnlyList<string> SplitJsArray(string arrayContent)
{
    var result = new List<string>();
    int depth = 0;
    bool inStr = false;
    char strChar = '\0';
    int start = 0;
    for (int i = 0; i < arrayContent.Length; i++)
    {
        char c = arrayContent[i];
        if (inStr)
        {
            if (c == '\\') { i++; continue; }
            if (c == strChar) inStr = false;
            continue;
        }
        if (c is '\'' or '"' or '`') { inStr = true; strChar = c; continue; }
        if (c is '{' or '[' or '(') depth++;
        else if (c is '}' or ']' or ')') depth--;
        else if (c == ',' && depth == 0)
        {
            var elem = arrayContent[start..i].Trim();
            if (elem.Length > 0) result.Add(elem);
            start = i + 1;
        }
    }
    var last = arrayContent[start..].Trim();
    if (last.Length > 0) result.Add(last);
    return result;
}

static int? SdInt(string text, string field)
{
    var m = Regex.Match(text, $@"\b{Regex.Escape(field)}:\s*(-?\d+)");
    return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : null;
}

static string? SdStr(string text, string field)
{
    var m = Regex.Match(text, $@"\b{Regex.Escape(field)}:\s*['""](\w+)['""]");
    return m.Success ? m.Groups[1].Value : null;
}

static (int N, int D)? SdPair(string text, string field)
{
    var m = Regex.Match(text, $@"\b{Regex.Escape(field)}:\s*\[(-?\d+),\s*(-?\d+)\]");
    return m.Success ? (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)) : null;
}

static ShowdownSecondaryEffect? ParseShowdownSecondary(string content)
{
    var chance = SdInt(content, "chance") ?? 0;
    var status = SdStr(content, "status");
    var volatileStatus = SdStr(content, "volatileStatus");
    // flinch is a volatileStatus but handled separately for clarity
    var flinch = volatileStatus == "flinch"
        || content.Contains("'flinch'") || content.Contains("\"flinch\"");
    if (flinch) volatileStatus = null; // don't double-report flinch

    var boosts = new List<(string, int)>();
    var boostsContent = ExtractShowdownBlock(content, "boosts");
    if (boostsContent is not null)
    {
        foreach (Match bm in Regex.Matches(boostsContent, @"(\w+):\s*(-?\d+)"))
            if (int.TryParse(bm.Groups[2].Value, out var bv))
                boosts.Add((bm.Groups[1].Value, bv));
    }

    return status is null && volatileStatus is null && !flinch && boosts.Count == 0 ? null
        : new ShowdownSecondaryEffect(chance, status, volatileStatus, flinch, boosts);
}

/// <summary>
/// Parse pokemon-showdown/data/moves.ts and return a dictionary of
/// PokeAPI move ID → supplemental secondary-effect data.
/// </summary>
static IReadOnlyDictionary<int, ShowdownMoveSupplement> ReadShowdownMoves(string showdownPath)
{
    // Maps Showdown flag names → PokeAPI move flag identifiers.
    // Showdown-only flags (metronome, allyanim, noassist, etc.) are intentionally omitted.
    IReadOnlyDictionary<string, string> flagMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["contact"]    = "contact",
        ["charge"]     = "charge",
        ["recharge"]   = "recharge",
        ["protect"]    = "protect",
        ["reflectable"]= "reflectable",
        ["snatch"]     = "snatch",
        ["mirror"]     = "mirror",
        ["punch"]      = "punch",
        ["sound"]      = "sound",
        ["gravity"]    = "gravity",
        ["defrost"]    = "defrost",
        ["distance"]   = "distance",
        ["heal"]       = "heal",
        ["bypasssub"]  = "authentic",
        ["powder"]     = "powder",
        ["bite"]       = "bite",
        ["pulse"]      = "pulse",
        ["bullet"]     = "ballistics",
        ["nonsky"]     = "non-sky-battle",
        ["dance"]      = "dance",
        ["wind"]       = "wind",
        ["slicing"]    = "slicing",
    };

    var movesTs = File.Exists(showdownPath) && Path.GetExtension(showdownPath) == ".ts"
        ? showdownPath
        : Path.Combine(showdownPath, "data", "moves.ts");

    if (!File.Exists(movesTs))
    {
        Console.Error.WriteLine($"WARNING: Showdown moves.ts not found at {movesTs}");
        return new Dictionary<int, ShowdownMoveSupplement>();
    }

    var text = File.ReadAllText(movesTs, Encoding.UTF8);
    var result = new Dictionary<int, ShowdownMoveSupplement>();

    // Each top-level move entry starts with: \n\t<identifier>: {
    var entryRe = new Regex(@"\n\t[a-z0-9]+:\s*\{", RegexOptions.Compiled);

    foreach (Match entryMatch in entryRe.Matches(text))
    {
        int openPos = entryMatch.Index + entryMatch.Length - 1; // the {
        int closePos = FindMatchingClose(text, openPos, '{', '}');
        if (closePos < 0) continue;

        var block = text[(openPos + 1)..closePos];

        var num = SdInt(block, "num");
        if (num is not > 0) continue;

        // Secondary effects
        var secondaries = new List<ShowdownSecondaryEffect>();

        var secContent = ExtractShowdownBlock(block, "secondary");
        if (secContent is not null)
        {
            var fx = ParseShowdownSecondary(secContent);
            if (fx is not null) secondaries.Add(fx);
        }

        var secsContent = ExtractShowdownBlock(block, "secondaries", '[', ']');
        if (secsContent is not null)
        {
            foreach (var elem in SplitJsArray(secsContent))
            {
                var inner = elem.Trim();
                if (!inner.StartsWith('{')) continue;
                var fx = ParseShowdownSecondary(inner[1..(inner.EndsWith('}') ? inner.Length - 1 : inner.Length)]);
                if (fx is not null) secondaries.Add(fx);
            }
        }

        // Top-level status (status moves like Will-O-Wisp).
        // Only relevant when no secondary already carries a status.
        string? topStatus = secondaries.Any(s => s.Status is not null) ? null : SdStr(block, "status");

        var drain = SdPair(block, "drain");
        var recoil = SdPair(block, "recoil");
        var heal = SdPair(block, "heal");
        var critRatio = SdInt(block, "critRatio");

        // multihit: try [min, max] first, then scalar
        (int Min, int Max)? multihitRange = null;
        int? multihitFixed = null;
        var mhPair = SdPair(block, "multihit");
        if (mhPair.HasValue)
            multihitRange = (mhPair.Value.N, mhPair.Value.D);
        else if (SdInt(block, "multihit") is { } mhFixed)
            multihitFixed = mhFixed;

        // Flags: extract from flags: { key: 1, ... } block and map to PokeAPI identifiers
        var sdFlagsSet = new HashSet<string>(StringComparer.Ordinal);
        var flagsContent = ExtractShowdownBlock(block, "flags");
        if (flagsContent is not null)
        {
            foreach (Match fm in Regex.Matches(flagsContent, @"(\w+):"))
            {
                if (flagMap.TryGetValue(fm.Groups[1].Value, out var pokeApiFlag))
                    sdFlagsSet.Add(pokeApiFlag);
            }
        }
        // Sort for deterministic JSON output
        var sdFlags = sdFlagsSet.OrderBy(f => f).ToList();

        bool hasData = secondaries.Count > 0 || topStatus is not null
            || drain.HasValue || recoil.HasValue || heal.HasValue
            || multihitFixed.HasValue || multihitRange.HasValue
            || critRatio is > 1 || sdFlags.Count > 0;
        if (!hasData) continue;

        result[num.Value] = new ShowdownMoveSupplement(
            secondaries, topStatus, drain, recoil, heal,
            multihitFixed, multihitRange, critRatio is > 1 ? critRatio : null, sdFlags);
    }

    Console.WriteLine($"  → {result.Count} moves with secondary/status/flag data from Showdown");
    return result;
}

/// <summary>
/// Parse pokemon-showdown/data/text/items.ts and return a dictionary of
/// Showdown item id (alphanumeric, e.g. "abilityshield") → English shortDesc.
/// Used as a fallback for items whose PokeAPI item_prose.csv row is empty.
/// </summary>
static IReadOnlyDictionary<string, string> ReadShowdownItemText(string showdownPath)
{
    var itemsTs = File.Exists(showdownPath) && Path.GetExtension(showdownPath) == ".ts"
        ? showdownPath
        : Path.Combine(showdownPath, "data", "text", "items.ts");

    if (!File.Exists(itemsTs))
    {
        Console.Error.WriteLine($"WARNING: Showdown text/items.ts not found at {itemsTs}");
        return new Dictionary<string, string>();
    }

    var text = File.ReadAllText(itemsTs, Encoding.UTF8);
    var result = new Dictionary<string, string>(StringComparer.Ordinal);

    // Each top-level item entry starts with: \n\t<id>: {
    var entryRe = new Regex(@"\n\t([a-z0-9]+):\s*\{", RegexOptions.Compiled);
    foreach (Match entryMatch in entryRe.Matches(text))
    {
        var id = entryMatch.Groups[1].Value;
        int openPos = entryMatch.Index + entryMatch.Length - 1; // the {
        int closePos = FindMatchingClose(text, openPos, '{', '}');
        if (closePos < 0) continue;
        var block = text[(openPos + 1)..closePos];

        // Grab the first shortDesc. Showdown puts the current-gen shortDesc at the top level,
        // with any per-gen overrides nested afterward in genX: { ... } blocks, so the first
        // match is always the current description.
        var m = Regex.Match(block, @"shortDesc:\s*""([^""]*)""");
        if (m.Success) result[id] = m.Groups[1].Value;
    }

    Console.WriteLine($"  → {result.Count} items with shortDesc from Showdown");
    return result;
}

// Human-readable name for a Showdown volatile status that appears in a secondary effect.
// Returns null for volatile statuses that are better described by other fields (e.g. stat changes)
// or that don't have a meaningful user-facing description.
static string? ShowdownVolatileAilmentName(string volatileStatus) => volatileStatus switch
{
    "confusion"     => "Confusion",
    "saltcure"      => "Salt Cure",
    "healblock"     => "Heal Block",
    "syrupbomb"     => "Syrup Bomb",
    _               => null,
};

// PokeAPI ailment ID + English name for a Showdown status abbreviation.
static (int Id, string Name) ShowdownAilment(string statusCode) => statusCode switch
{
    "par" => (1, "Paralysis"),
    "slp" => (2, "Sleep"),
    "frz" => (3, "Freeze"),
    "brn" => (4, "Burn"),
    "psn" => (5, "Poison"),
    "tox" => (5, "Badly Poisoned"),
    _ => (99, statusCode),
};

// Full stat name from Showdown abbreviation.
static string ShowdownStatName(string abbr) => abbr switch
{
    "atk" => "Attack", "def" => "Defense",
    "spa" => "Sp. Atk", "spd" => "Sp. Def", "spe" => "Speed",
    "acc" => "Accuracy", "eva" => "Evasion",
    _ => abbr,
};

// ---------------------------------------------------------------------------
// Move stat epoch logic
// ---------------------------------------------------------------------------

// Given sorted (vg, oldValue) pairs and the current value, return (fromVg, value) epochs.
// See module docstring for the changelog interpretation.
List<(int fromVg, string value)> FieldEpochs(List<(int vg, string val)> changes, string currentVal)
{
    if (changes.Count == 0)
        return [(1, currentVal)];

    var epochs = new List<(int, string)>();
    for (var i = 0; i < changes.Count; i++)
    {
        var fromVg = i > 0 ? changes[i - 1].vg : 1;
        epochs.Add((fromVg, changes[i].val));
    }
    epochs.Add((changes[^1].vg, currentVal));
    return epochs;
}

List<Dictionary<string, string>> ComputeStatEpochs(
    Dictionary<string, string> move,
    List<Dictionary<string, string>> changes)
{
    var timelines = new Dictionary<string, List<(int fromVg, string value)>>();
    foreach (var field in MoveStatFields)
    {
        var fieldChanges = changes
            .Where(c => c.TryGetValue(field, out var v) && !string.IsNullOrEmpty(v))
            .Select(c => (vg: int.Parse(c["changed_in_version_group_id"]), val: c[field]))
            .OrderBy(x => x.vg)
            .ToList();
        move.TryGetValue(field, out var current);
        timelines[field] = FieldEpochs(fieldChanges, current ?? "");
    }

    var allFromVgs = timelines.Values
        .SelectMany(tl => tl.Select(x => x.fromVg))
        .Distinct()
        .OrderBy(x => x)
        .ToList();

    string ValueAt(string field, int targetVg)
    {
        var val = timelines[field][0].value;
        foreach (var (fromVg, v) in timelines[field])
            if (fromVg <= targetVg) val = v;
        return val;
    }

    var epochs = new List<Dictionary<string, string>>();
    foreach (var fromVg in allFromVgs)
    {
        var snapshot = MoveStatFields.ToDictionary(f => f, f => ValueAt(f, fromVg));
        if (epochs.Count == 0 || !MoveStatFields.All(f => epochs[^1][f] == snapshot[f]))
        {
            var epoch = new Dictionary<string, string>(snapshot) { ["fromVersionGroup"] = fromVg.ToString() };
            epochs.Add(epoch);
        }
    }
    return epochs;
}

// ---------------------------------------------------------------------------
// Generators
// ---------------------------------------------------------------------------

JsonObject GenerateAbilityInfo(
    string csvDir,
    IReadOnlyDictionary<string, string>? overrides = null,
    IReadOnlyDictionary<string, HashSet<string>>? flavorRemove = null)
{
    overrides ??= new Dictionary<string, string>();
    flavorRemove ??= new Dictionary<string, HashSet<string>>();

    var abilities = ReadCsv(Path.Combine(csvDir, "abilities.csv"))
        .ToDictionary(r => r["id"]);

    var names = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "ability_names.csv")))
        if (r["local_language_id"] == English) names[r["ability_id"]] = r["name"];

    var descriptions = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "ability_prose.csv")))
        if (r["local_language_id"] == English) descriptions[r["ability_id"]] = CleanText(r["short_effect"]);

    var flavor = EnFlavor(ReadCsv(Path.Combine(csvDir, "ability_flavor_text.csv")), "ability_id");

    var result = new JsonObject();
    foreach (var (abilityId, ability) in abilities)
    {
        if (ability.TryGetValue("is_main_series", out var main) && main == "0") continue;
        if (!names.TryGetValue(abilityId, out var name)) continue;

        // Ability description overrides replace (not fall back) — used to correct upstream data bugs.
        var description = overrides.TryGetValue(name, out var corrected)
            ? corrected
            : descriptions.GetValueOrDefault(abilityId, "");

        var entry = new JsonObject
        {
            ["name"] = name,
            ["description"] = description,
        };
        if (flavor.TryGetValue(abilityId, out var flavorMap))
        {
            if (flavorRemove.TryGetValue(name, out var versionsToDrop))
                flavorMap = flavorMap.Where(kv => !versionsToDrop.Contains(kv.Key))
                                     .ToDictionary(kv => kv.Key, kv => kv.Value);
            entry["flavor"] = ToJsonObject(flavorMap);
        }
        result[abilityId] = entry;
    }
    return result;
}

JsonObject GenerateMoveInfo(string csvDir, string? showdownPath = null, IReadOnlyDictionary<string, string>? overrides = null)
{
    var damageClasses = new Dictionary<string, string> { ["1"] = "Status", ["2"] = "Physical", ["3"] = "Special" };

    var typeNames = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "type_names.csv")))
        if (r["local_language_id"] == English) typeNames[r["type_id"]] = r["name"];

    var effectProse = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "move_effect_prose.csv")))
        if (r["local_language_id"] == English) effectProse[r["move_effect_id"]] = CleanText(r["short_effect"]);

    var moveNames = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "move_names.csv")))
        if (r["local_language_id"] == English) moveNames[r["move_id"]] = r["name"];

    var flavor = EnFlavor(ReadCsv(Path.Combine(csvDir, "move_flavor_text.csv")), "move_id");

    var changelogByMove = new Dictionary<string, List<Dictionary<string, string>>>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "move_changelog.csv")))
    {
        var id = r["move_id"];
        if (!changelogByMove.TryGetValue(id, out var list)) changelogByMove[id] = list = [];
        list.Add(r);
    }

    var targetNames = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "move_target_prose.csv")))
        if (r["local_language_id"] == English) targetNames[r["move_target_id"]] = r["name"];

    var flagIds = ReadCsv(Path.Combine(csvDir, "move_flags.csv"))
        .ToDictionary(r => r["id"], r => r["identifier"]);

    var flagsByMove = new Dictionary<string, List<string>>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "move_flag_map.csv")))
    {
        if (!flagIds.TryGetValue(r["move_flag_id"], out var identifier)) continue;
        var id = r["move_id"];
        if (!flagsByMove.TryGetValue(id, out var list)) flagsByMove[id] = list = [];
        list.Add(identifier);
    }

    // PokeAPI does not track wind or slicing flags (Gen IX mechanics). Supplement from Showdown data.
    // Identifiers match moves.csv `identifier` column (hyphenated lowercase).
    HashSet<string> windMoveIdentifiers =
    [
        "aeroblast", "air-cutter", "bleakwind-storm", "blizzard", "fairy-wind",
        "gust", "heat-wave", "hurricane", "icy-wind", "petal-blizzard",
        "sandsear-storm", "sandstorm", "springtide-storm", "tailwind", "twister",
        "whirlwind", "wildbolt-storm",
    ];
    HashSet<string> slicingMoveIdentifiers =
    [
        "aerial-ace", "air-cutter", "air-slash", "aqua-cutter", "behemoth-blade",
        "bitter-blade", "ceaseless-edge", "cross-poison", "cut", "fury-cutter",
        "kowtow-cleave", "leaf-blade", "mighty-cleave", "night-slash", "population-bomb",
        "psyblade", "psycho-cut", "razor-leaf", "razor-shell", "sacred-sword",
        "secret-sword", "slash", "solar-blade", "stone-axe", "tachyon-cutter", "x-scissor",
    ];

    // move_meta: secondary effects (ailment, flinch, drain, healing, multi-hit, crit rate)
    var moveMetaById = new Dictionary<string, Dictionary<string, string>>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "move_meta.csv")))
        moveMetaById[r["move_id"]] = r;

    // ailment names (English only)
    var ailmentNames = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "move_meta_ailment_names.csv")))
        if (r["local_language_id"] == English) ailmentNames[r["move_meta_ailment_id"]] = r["name"];

    // stat changes per move
    var statChangesByMove = new Dictionary<string, List<(string stat, int change)>>();
    var statNameById = new Dictionary<string, string>
    {
        ["1"] = "HP", ["2"] = "Attack", ["3"] = "Defense",
        ["4"] = "Sp. Atk", ["5"] = "Sp. Def", ["6"] = "Speed",
        ["7"] = "Accuracy", ["8"] = "Evasion",
    };
    foreach (var r in ReadCsv(Path.Combine(csvDir, "move_meta_stat_changes.csv")))
    {
        var id = r["move_id"];
        if (!statChangesByMove.TryGetValue(id, out var list)) statChangesByMove[id] = list = [];
        var statName = statNameById.GetValueOrDefault(r["stat_id"], r["stat_id"]);
        list.Add((statName, int.Parse(r["change"])));
    }

    // Load Showdown supplement for moves absent from PokeAPI's move_meta CSV.
    var showdownMoves = showdownPath is not null
        ? ReadShowdownMoves(showdownPath)
        : (IReadOnlyDictionary<int, ShowdownMoveSupplement>)new Dictionary<int, ShowdownMoveSupplement>();

    var result = new JsonObject();
    foreach (var move in ReadCsv(Path.Combine(csvDir, "moves.csv")))
    {
        var moveId = move["id"];
        if (!moveNames.TryGetValue(moveId, out var name)) continue;

        move.TryGetValue("effect_id", out var effectId);
        var rawDesc = effectId is not null ? effectProse.GetValueOrDefault(effectId, "") : "";
        move.TryGetValue("effect_chance", out var effectChance);
        if (!string.IsNullOrEmpty(effectChance) && rawDesc.Contains("$effect_chance%"))
            rawDesc = rawDesc.Replace("$effect_chance%", $"{effectChance}%");

        // Last-resort fallback: scraped description override keyed by numeric move id.
        if (rawDesc.Length == 0 && overrides is not null && overrides.TryGetValue(moveId, out var overrideDesc))
            rawDesc = overrideDesc;

        var epochs = ComputeStatEpochs(move, changelogByMove.GetValueOrDefault(moveId, []));

        var resolvedStats = new JsonArray();
        foreach (var epoch in epochs)
        {
            move.TryGetValue("damage_class_id", out var dcId);
            var stat = new JsonObject
            {
                ["fromVersionGroup"] = int.Parse(epoch["fromVersionGroup"]),
                ["type"] = typeNames.GetValueOrDefault(epoch.GetValueOrDefault("type_id", ""), ""),
                ["category"] = dcId is not null ? damageClasses.GetValueOrDefault(dcId, "") : "",
                ["power"] = !string.IsNullOrEmpty(epoch.GetValueOrDefault("power")) ? int.Parse(epoch["power"]) : JsonValue.Create<int?>(null),
                ["pp"] = !string.IsNullOrEmpty(epoch.GetValueOrDefault("pp")) ? int.Parse(epoch["pp"]) : JsonValue.Create<int?>(null),
                ["accuracy"] = !string.IsNullOrEmpty(epoch.GetValueOrDefault("accuracy")) ? int.Parse(epoch["accuracy"]) : JsonValue.Create<int?>(null),
            };
            resolvedStats.Add(stat);
        }

        move.TryGetValue("priority", out var priorityStr);
        var priority = int.TryParse(priorityStr, out var p) ? p : 0;

        move.TryGetValue("target_id", out var targetId);
        move.TryGetValue("identifier", out var moveIdentifier);
        var moveFlags = flagsByMove.TryGetValue(moveId, out var flags) ? new List<string>(flags) : [];
        // Supplement flags from Showdown: adds any flags Showdown knows about that PokeAPI is missing.
        // PokeAPI's move_flag_map can be incomplete for newer moves (e.g. only wind, missing protect/mirror).
        if (int.TryParse(moveId, out var flagMoveIdInt)
            && showdownMoves.TryGetValue(flagMoveIdInt, out var sdForFlags)
            && sdForFlags.Flags.Count > 0)
            foreach (var f in sdForFlags.Flags)
                if (!moveFlags.Contains(f))
                    moveFlags.Add(f);
        if (moveIdentifier is not null && windMoveIdentifiers.Contains(moveIdentifier) && !moveFlags.Contains("wind"))
            moveFlags.Add("wind");
        if (moveIdentifier is not null && slicingMoveIdentifiers.Contains(moveIdentifier) && !moveFlags.Contains("slicing"))
            moveFlags.Add("slicing");
        var entry = new JsonObject
        {
            ["name"] = name,
            ["description"] = rawDesc,
            ["target"] = targetId is not null ? targetNames.GetValueOrDefault(targetId, "") : "",
            ["flags"] = new JsonArray(moveFlags.Select(f => JsonValue.Create(f)).ToArray<JsonNode?>()),
            ["stats"] = resolvedStats,
        };
        if (priority != 0)
            entry["priority"] = priority;
        if (flavor.TryGetValue(moveId, out var flavorMap))
            entry["flavor"] = ToJsonObject(flavorMap);

        // Secondary effects from move_meta
        if (moveMetaById.TryGetValue(moveId, out var meta))
        {
            var metaObj = new JsonObject();

            var ailmentIdStr = meta.GetValueOrDefault("meta_ailment_id", "0");
            if (int.TryParse(ailmentIdStr, out var ailmentId) && ailmentId != 0)
            {
                metaObj["ailmentId"] = ailmentId;
                if (ailmentNames.TryGetValue(ailmentIdStr, out var ailmentName))
                    metaObj["ailmentName"] = ailmentName;
            }
            if (int.TryParse(meta.GetValueOrDefault("ailment_chance", "0"), out var ailmentChance) && ailmentChance > 0)
                metaObj["ailmentChance"] = ailmentChance;
            if (int.TryParse(meta.GetValueOrDefault("flinch_chance", "0"), out var flinchChance) && flinchChance > 0)
                metaObj["flinchChance"] = flinchChance;
            if (int.TryParse(meta.GetValueOrDefault("drain", "0"), out var drain) && drain != 0)
                metaObj["drain"] = drain;
            if (int.TryParse(meta.GetValueOrDefault("healing", "0"), out var healing) && healing != 0)
                metaObj["healing"] = healing;
            var minHitsStr = meta.GetValueOrDefault("min_hits", "");
            var maxHitsStr = meta.GetValueOrDefault("max_hits", "");
            if (int.TryParse(minHitsStr, out var minHits)) metaObj["minHits"] = minHits;
            if (int.TryParse(maxHitsStr, out var maxHits)) metaObj["maxHits"] = maxHits;
            if (int.TryParse(meta.GetValueOrDefault("crit_rate", "0"), out var critRate) && critRate > 0)
                metaObj["critRate"] = critRate;
            if (int.TryParse(meta.GetValueOrDefault("stat_chance", "0"), out var statChance) && statChance > 0)
                metaObj["statChance"] = statChance;

            if (statChangesByMove.TryGetValue(moveId, out var statChanges) && statChanges.Count > 0)
            {
                var changesArr = new JsonArray();
                foreach (var (stat, change) in statChanges)
                    changesArr.Add(new JsonObject { ["stat"] = stat, ["change"] = change });
                metaObj["statChanges"] = changesArr;
            }

            // Let Showdown refine the ailment name when PokeAPI is too coarse.
            // e.g. PokeAPI uses ailment 5 ("Poison") for both psn and tox; Showdown distinguishes them.
            if (metaObj.ContainsKey("ailmentId")
                && int.TryParse(moveId, out var refineMoveId)
                && showdownMoves.TryGetValue(refineMoveId, out var sdRefine))
            {
                var sdStatus = sdRefine.TopLevelStatus
                    ?? sdRefine.Secondaries.Select(fx => fx.Status).FirstOrDefault(s => s is not null);
                if (sdStatus is not null)
                {
                    var (_, refinedName) = ShowdownAilment(sdStatus);
                    metaObj["ailmentName"] = refinedName;
                }
            }

            if (metaObj.Count > 0)
                entry["meta"] = metaObj;
        }
        else if (int.TryParse(moveId, out var moveIdInt)
                 && showdownMoves.TryGetValue(moveIdInt, out var sd))
        {
            // Supplement: build meta from Showdown data for moves missing from PokeAPI move_meta.
            var metaObj = new JsonObject();

            // Top-level status (e.g. Will-O-Wisp): guaranteed ailment
            if (sd.TopLevelStatus is { } topStatus)
            {
                var (aId, aName) = ShowdownAilment(topStatus);
                metaObj["ailmentId"] = aId;
                metaObj["ailmentName"] = aName;
            }

            // Secondary effects (merge across all secondaries/secondaries[] entries)
            foreach (var fx in sd.Secondaries)
            {
                if (fx.Status is { } status)
                {
                    var (aId, aName) = ShowdownAilment(status);
                    if (!metaObj.ContainsKey("ailmentId")) metaObj["ailmentId"] = aId;
                    if (!metaObj.ContainsKey("ailmentName")) metaObj["ailmentName"] = aName;
                    if (fx.Chance is > 0 and < 100 && !metaObj.ContainsKey("ailmentChance"))
                        metaObj["ailmentChance"] = fx.Chance;
                }
                if (fx.VolatileStatus is { } vs && ShowdownVolatileAilmentName(vs) is { } vsName
                    && !metaObj.ContainsKey("ailmentId"))
                {
                    metaObj["ailmentId"] = -2; // sentinel: volatile/move-specific condition
                    metaObj["ailmentName"] = vsName;
                    if (fx.Chance is > 0 and < 100 && !metaObj.ContainsKey("ailmentChance"))
                        metaObj["ailmentChance"] = fx.Chance;
                }
                if (fx.Flinch && fx.Chance > 0 && !metaObj.ContainsKey("flinchChance"))
                    metaObj["flinchChance"] = fx.Chance;
                if (fx.Boosts.Count > 0)
                {
                    if (fx.Chance is > 0 and < 100 && !metaObj.ContainsKey("statChance"))
                        metaObj["statChance"] = fx.Chance;
                    if (!metaObj.ContainsKey("statChanges"))
                    {
                        var changesArr = new JsonArray();
                        foreach (var (abbr, change) in fx.Boosts)
                            changesArr.Add(new JsonObject { ["stat"] = ShowdownStatName(abbr), ["change"] = change });
                        metaObj["statChanges"] = changesArr;
                    }
                }
            }

            // Drain / recoil
            if (sd.Drain.HasValue)
                metaObj["drain"] = (int)Math.Round(sd.Drain.Value.N * 100.0 / sd.Drain.Value.D, MidpointRounding.AwayFromZero);
            else if (sd.Recoil.HasValue)
                metaObj["drain"] = -(int)Math.Round(sd.Recoil.Value.N * 100.0 / sd.Recoil.Value.D, MidpointRounding.AwayFromZero);

            // Self-healing (Recover, Roost, etc.)
            if (sd.Heal.HasValue)
                metaObj["healing"] = (int)Math.Round(sd.Heal.Value.N * 100.0 / sd.Heal.Value.D, MidpointRounding.AwayFromZero);

            // Multi-hit
            if (sd.MultihitFixed.HasValue)
            {
                metaObj["minHits"] = sd.MultihitFixed.Value;
                metaObj["maxHits"] = sd.MultihitFixed.Value;
            }
            else if (sd.MultihitRange.HasValue)
            {
                metaObj["minHits"] = sd.MultihitRange.Value.Min;
                metaObj["maxHits"] = sd.MultihitRange.Value.Max;
            }

            // Crit rate (Showdown critRatio > 1 → high crit; use the value directly)
            if (sd.CritRatio.HasValue)
                metaObj["critRate"] = sd.CritRatio.Value;

            if (metaObj.Count > 0)
                entry["meta"] = metaObj;
        }

        result[moveId] = entry;
    }
    return result;
}

JsonObject GenerateItemInfo(string csvDir, string? showdownPath = null, IReadOnlyDictionary<string, string>? overrides = null)
{
    var itemNames = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "item_names.csv")))
        if (r["local_language_id"] == English) itemNames[r["item_id"]] = r["name"];

    var descriptions = new Dictionary<string, string>();
    foreach (var r in ReadCsv(Path.Combine(csvDir, "item_prose.csv")))
        if (r["local_language_id"] == English) descriptions[r["item_id"]] = CleanText(r["short_effect"]);

    var flavor = EnFlavor(ReadCsv(Path.Combine(csvDir, "item_flavor_text.csv")), "item_id");

    // Supplemental short descriptions for items where PokeAPI's item_prose.csv is empty
    // (common for Gen 9 held items, event items, and items added after PokeAPI freezes).
    var showdownItemDescs = showdownPath is not null
        ? ReadShowdownItemText(showdownPath)
        : (IReadOnlyDictionary<string, string>)new Dictionary<string, string>();

    overrides ??= new Dictionary<string, string>();

    var result = new JsonObject();
    foreach (var item in ReadCsv(Path.Combine(csvDir, "items.csv")))
    {
        var itemId = item["id"];
        if (!itemNames.TryGetValue(itemId, out var name)) continue;
        var key = name.ToLowerInvariant().Trim();

        var desc = descriptions.GetValueOrDefault(itemId, "");
        if (desc.Length == 0 && item.TryGetValue("identifier", out var identifier))
        {
            var sdKey = identifier.Replace("-", "");
            if (showdownItemDescs.TryGetValue(sdKey, out var sdDesc))
                desc = sdDesc;
        }
        // Last-resort fallback: scraped description override (see scrape-pokemondb-descriptions.cs).
        if (desc.Length == 0 && overrides.TryGetValue(key, out var overrideDesc))
            desc = overrideDesc;

        var entry = new JsonObject
        {
            ["name"] = name,
            ["description"] = desc,
        };
        if (flavor.TryGetValue(itemId, out var flavorMap))
            entry["flavor"] = ToJsonObject(flavorMap);

        // PokeAPI can have multiple rows with the same English name (e.g. Legends: Arceus
        // variants `lapoke-ball` / `lagreat-ball` / `laultra-ball` alongside the originals).
        // Don't let an empty duplicate clobber a populated entry.
        var newHasData = desc.Length > 0 || entry["flavor"] is not null;
        if (result.TryGetPropertyValue(key, out var existingNode)
            && existingNode is JsonObject existing)
        {
            var existingHasData =
                ((string?)existing["description"])?.Length > 0
                || existing["flavor"] is not null;
            if (existingHasData && !newHasData)
                continue;
        }
        result[key] = entry;
    }
    return result;
}

// ---------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------

var serializerOptions = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    WriteIndented = false,
};

var tasks = new (string file, Func<string, JsonObject> generator, string label)[]
{
    ("ability-info.json", csv => GenerateAbilityInfo(csv, abilityOverrides, abilityFlavorRemove), "abilities"),
    ("move-info.json",    csv => GenerateMoveInfo(csv, showdownArg, moveOverrides),               "moves"),
    ("item-info.json",    csv => GenerateItemInfo(csv, showdownArg, itemOverrides),               "items"),
};

foreach (var (file, generator, label) in tasks)
{
    Console.Write($"Generating {file}...");
    var data = generator(csvDir);
    var outPath = Path.Combine(outputDir, file);
    File.WriteAllText(outPath, data.ToJsonString(serializerOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    var sizeKb = new FileInfo(outPath).Length / 1024;
    Console.WriteLine($"  {data.Count:N0} {label} -> {file} ({sizeKb} KB)");
}

Console.WriteLine();
Console.WriteLine("Done.");
return 0;

// ---------------------------------------------------------------------------
// Showdown data types (must be declared after all top-level statements)
// ---------------------------------------------------------------------------

/// <summary>One secondary effect from a Showdown secondary/secondaries entry.</summary>
record ShowdownSecondaryEffect(
    int Chance,
    string? Status,
    string? VolatileStatus,
    bool Flinch,
    IReadOnlyList<(string Stat, int Change)> Boosts);

/// <summary>All supplemental data extracted from one Showdown move entry.</summary>
record ShowdownMoveSupplement(
    IReadOnlyList<ShowdownSecondaryEffect> Secondaries,
    string? TopLevelStatus,
    (int N, int D)? Drain,
    (int N, int D)? Recoil,
    (int N, int D)? Heal,
    int? MultihitFixed,
    (int Min, int Max)? MultihitRange,
    int? CritRatio,
    IReadOnlyList<string> Flags);
