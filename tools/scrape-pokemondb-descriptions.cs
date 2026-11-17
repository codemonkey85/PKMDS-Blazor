#!/usr/bin/env dotnet-run
#:property PublishAot=false
#:include shared/ToolPaths.cs
/*
 * Scrape pokemondb.net for item and move descriptions that are missing from the
 * generated JSON data (PokeAPI + Showdown). Produces a cache file consumed by
 * generate-descriptions.cs as a last-resort fallback.
 *
 * Targets entries in ability-info.json / move-info.json / item-info.json whose
 * description AND flavor map are both empty. These are the "No description
 * available" tooltips in the UI.
 *
 * Respects pokemondb.net's robots.txt (Crawl-delay: 2) — defaults to a 2.5s
 * delay between requests. Sends a descriptive User-Agent.
 *
 * Usage:
 *   dotnet run tools/scrape-pokemondb-descriptions.cs [-- [options]]
 *
 * Options:
 *   --data <dir>         Directory with the generated JSON files.
 *                        Default: Pkmds.Rcl/wwwroot/data/ under the repo root.
 *   --output <path>      Cache file path.
 *                        Default: tools/data/description-overrides.json under the repo root.
 *   --delay <ms>         Delay between requests in milliseconds. Default: 2500.
 *   --force              Re-scrape entries that already have a cached description.
 *   --retry-notfound     Re-scrape entries previously marked as 404.
 *   --items-only         Scrape items only (skip moves).
 *   --moves-only         Scrape moves only (skip items).
 *   --limit <n>          Stop after scraping <n> new entries (for testing).
 *
 * Output structure (description-overrides.json):
 *   {
 *     "items": { "tera orb": "An orb that holds...", ... },
 *     "moves": { "896": "Blazing Torque deals...", ... },
 *     "notFound": {
 *       "items": ["Aipom Hair", ...],
 *       "moves": []
 *     }
 *   }
 */

using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

const string UserAgent = "PKMDS-Blazor-Scraper/1.0 (+https://github.com/codemonkey85/PKMDS-Blazor)";
const string BaseUrl = "https://pokemondb.net";

// ---------------------------------------------------------------------------
// Args
// ---------------------------------------------------------------------------

string? dataArg = null;
string? outputArg = null;
int delayMs = 2500;
bool force = false;
bool retryNotFound = false;
bool itemsOnly = false;
bool movesOnly = false;
int limit = int.MaxValue;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--data" when i + 1 < args.Length: dataArg = args[++i]; break;
        case "--output" when i + 1 < args.Length: outputArg = args[++i]; break;
        case "--delay" when i + 1 < args.Length: delayMs = int.Parse(args[++i]); break;
        case "--force": force = true; break;
        case "--retry-notfound": retryNotFound = true; break;
        case "--items-only": itemsOnly = true; break;
        case "--moves-only": movesOnly = true; break;
        case "--limit" when i + 1 < args.Length: limit = int.Parse(args[++i]); break;
    }
}

var repoRoot = Pkmds.Tools.ToolPaths.FindRepositoryRoot() ?? throw new DirectoryNotFoundException("Could not find Pkmds.Rcl/ in any parent directory.");
var dataDir = dataArg is not null ? Path.GetFullPath(dataArg) : Path.Combine(repoRoot, "Pkmds.Rcl", "wwwroot", "data");
var outputPath = outputArg is not null
    ? Path.GetFullPath(outputArg)
    : Path.Combine(repoRoot, "tools", "data", "description-overrides.json");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

// ---------------------------------------------------------------------------
// Load existing cache
// ---------------------------------------------------------------------------

var cache = File.Exists(outputPath)
    ? JsonNode.Parse(File.ReadAllText(outputPath))?.AsObject() ?? new JsonObject()
    : new JsonObject();

var cachedItems = (cache["items"] as JsonObject) ?? new JsonObject();
var cachedMoves = (cache["moves"] as JsonObject) ?? new JsonObject();
var cachedNotFound = (cache["notFound"] as JsonObject) ?? new JsonObject();
var notFoundItems = (cachedNotFound["items"] as JsonArray) ?? [];
var notFoundMoves = (cachedNotFound["moves"] as JsonArray) ?? [];

HashSet<string> notFoundItemSet = [.. notFoundItems.Select(n => (string)n!).Where(n => n is not null)];
HashSet<string> notFoundMoveSet = [.. notFoundMoves.Select(n => (string)n!).Where(n => n is not null)];

if (retryNotFound)
{
    notFoundItemSet.Clear();
    notFoundMoveSet.Clear();
}

// ---------------------------------------------------------------------------
// Find runtime gaps from the generated JSON
// ---------------------------------------------------------------------------

static bool IsEmpty(string? s) => string.IsNullOrWhiteSpace(s);

static bool AllFlavorsEmpty(JsonNode? flavor)
{
    if (flavor is not JsonObject obj || obj.Count == 0) return true;
    foreach (var (_, node) in obj)
        if (!IsEmpty(node?.GetValue<string>())) return false;
    return true;
}

JsonObject LoadJson(string path) =>
    (JsonObject)JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8))!;

var itemsJson = LoadJson(Path.Combine(dataDir, "item-info.json"));
var movesJson = LoadJson(Path.Combine(dataDir, "move-info.json"));

// Items: keyed by lowercase English name. Collect (key, displayName) pairs.
var itemGaps = new List<(string Key, string Name)>();
foreach (var (key, node) in itemsJson)
{
    if (node is not JsonObject entry) continue;
    var descEmpty = IsEmpty((string?)entry["description"]);
    var flavorEmpty = AllFlavorsEmpty(entry["flavor"]);
    if (descEmpty && flavorEmpty)
    {
        var name = (string?)entry["name"] ?? key;
        itemGaps.Add((key, name));
    }
}

// Moves: keyed by numeric id.
var moveGaps = new List<(string Id, string Name)>();
foreach (var (id, node) in movesJson)
{
    if (node is not JsonObject entry) continue;
    var descEmpty = IsEmpty((string?)entry["description"]);
    var flavorEmpty = AllFlavorsEmpty(entry["flavor"]);
    if (descEmpty && flavorEmpty)
    {
        var name = (string?)entry["name"] ?? id;
        moveGaps.Add((id, name));
    }
}

Console.WriteLine($"Runtime gaps discovered: {itemGaps.Count} items, {moveGaps.Count} moves");
Console.WriteLine($"Cache: {cachedItems.Count} item descriptions, {cachedMoves.Count} move descriptions, " +
                  $"{notFoundItemSet.Count} items and {notFoundMoveSet.Count} moves previously 404");
Console.WriteLine($"Output: {outputPath}");
Console.WriteLine($"Delay : {delayMs}ms");
Console.WriteLine();

// ---------------------------------------------------------------------------
// Slug + HTML extraction
// ---------------------------------------------------------------------------

static string Slugify(string name)
{
    // NFD normalize, strip combining marks (é → e)
    var normalized = name.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(normalized.Length);
    foreach (var c in normalized)
    {
        var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
        if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
        if (c == '\'' || c == '’' || c == '‘') continue; // drop apostrophes (both straight and curly)
        sb.Append(c);
    }
    var stripped = sb.ToString().ToLowerInvariant();
    stripped = Regex.Replace(stripped, @"[^a-z0-9]+", "-");
    return stripped.Trim('-');
}

static string StripHtml(string html)
{
    var noTags = Regex.Replace(html, @"<[^>]+>", "");
    var decoded = WebUtility.HtmlDecode(noTags);
    return Regex.Replace(decoded, @"\s+", " ").Trim();
}

/// <summary>
/// Extract a description from a pokemondb.net item or move page. Tries the Effects
/// section first (primary source for most pages); falls back to the first row of the
/// Game descriptions table when Effects is empty (common for LA-only items like
/// Blank Plate). pokemondb's HTML is sloppy (missing closing <c>&lt;/p&gt;</c> before
/// a <c>&lt;/div&gt;</c>), so the Effects extraction captures the block between the
/// Effects h2 and the next h2 or end-of-section, then concatenates paragraphs.
/// </summary>
static string? ExtractEffects(string html)
{
    var headerMatch = Regex.Match(html, @"<h2[^>]*>\s*Effects\s*</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    if (headerMatch.Success)
    {
        var rest = html[(headerMatch.Index + headerMatch.Length)..];
        var stopMatch = Regex.Match(rest, @"<h2[^>]*>|</div>\s*<div\s+class=""grid-col", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var block = stopMatch.Success ? rest[..stopMatch.Index] : rest;

        var paragraphs = new List<string>();
        foreach (Match pm in Regex.Matches(block, @"<p[^>]*>(.*?)(?=</p>|<p[^>]*>|$)", RegexOptions.Singleline))
        {
            var text = StripHtml(pm.Groups[1].Value);
            if (text.Length > 0) paragraphs.Add(text);
        }
        if (paragraphs.Count > 0) return string.Join(" ", paragraphs);
    }

    // Fallback: first cell-med-text under the Game descriptions table.
    var descMatch = Regex.Match(html, @"<td\s+class=""cell-med-text"">(.*?)</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    if (descMatch.Success)
    {
        var text = StripHtml(descMatch.Groups[1].Value);
        if (text.Length > 0) return text;
    }

    return null;
}

/// <summary>
/// Reject obvious placeholder extractions. pokemondb's TM pages follow a template
/// "TMxxx teaches a move to a compatible Pokémon. TMxxx is:" that trails into a
/// <c>&lt;ul&gt;</c> of moves outside the <c>&lt;p&gt;</c>. For TMs with no mapped
/// move (common for unused placeholder slots), the trailing colon is the entire
/// useful content extracted. Detect and reject by the trailing colon.
/// </summary>
static bool IsUsableDescription(string text) => !text.TrimEnd().EndsWith(':');

// ---------------------------------------------------------------------------
// HTTP
// ---------------------------------------------------------------------------

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
http.Timeout = TimeSpan.FromSeconds(20);

async Task<(int Status, string? Html)> FetchAsync(string url)
{
    try
    {
        using var resp = await http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return ((int)resp.StatusCode, null);
        var html = await resp.Content.ReadAsStringAsync();
        return ((int)resp.StatusCode, html);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"    ERROR {url}: {ex.Message}");
        return (0, null);
    }
}

// ---------------------------------------------------------------------------
// Scrape
// ---------------------------------------------------------------------------

int fetched = 0, newDescriptions = 0, newNotFound = 0, skipped = 0, errors = 0;

async Task ScrapeAsync(string kind, string cacheKey, string displayName, JsonObject targetCache, HashSet<string> targetNotFound)
{
    if (!force && targetCache.ContainsKey(cacheKey))
    {
        skipped++;
        return;
    }
    if (!retryNotFound && targetNotFound.Contains(cacheKey))
    {
        skipped++;
        return;
    }
    if (fetched >= limit) return;

    var slug = Slugify(displayName);
    var url = $"{BaseUrl}/{kind}/{slug}";

    if (fetched > 0) await Task.Delay(delayMs);
    fetched++;

    var (status, html) = await FetchAsync(url);
    if (status == 404)
    {
        Console.WriteLine($"  [{fetched:D4}] 404 {kind}/{slug} ({displayName})");
        targetNotFound.Add(cacheKey);
        newNotFound++;
        return;
    }
    if (html is null)
    {
        Console.WriteLine($"  [{fetched:D4}] HTTP {status} {kind}/{slug} ({displayName})");
        errors++;
        return;
    }

    var effects = ExtractEffects(html);
    if (effects is null)
    {
        Console.WriteLine($"  [{fetched:D4}] NO-EFFECTS {kind}/{slug} ({displayName}) — page loaded but no Effects section");
        errors++;
        return;
    }
    if (!IsUsableDescription(effects))
    {
        Console.WriteLine($"  [{fetched:D4}] REJECT {kind}/{slug} ({displayName}) — placeholder/stub: \"{effects}\"");
        targetNotFound.Add(cacheKey);
        newNotFound++;
        return;
    }

    targetCache[cacheKey] = effects;
    newDescriptions++;
    Console.WriteLine($"  [{fetched:D4}] OK {kind}/{slug}: {effects[..Math.Min(80, effects.Length)]}{(effects.Length > 80 ? "..." : "")}");
}

void Save()
{
    cache["items"] = cachedItems;
    cache["moves"] = cachedMoves;
    var nf = new JsonObject
    {
        ["items"] = new JsonArray(notFoundItemSet.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).Select(n => JsonValue.Create(n)).ToArray<JsonNode?>()),
        ["moves"] = new JsonArray(notFoundMoveSet.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).Select(n => JsonValue.Create(n)).ToArray<JsonNode?>()),
    };
    cache["notFound"] = nf;

    var serializerOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };
    File.WriteAllText(outputPath, cache.ToJsonString(serializerOptions) + "\n", new UTF8Encoding(false));
}

// Save periodically so a long scrape isn't lost on Ctrl+C.
Console.CancelKeyPress += (_, e) =>
{
    Console.Error.WriteLine("\n\nInterrupted — saving partial results...");
    Save();
    Console.Error.WriteLine($"Saved: {newDescriptions} new descriptions, {newNotFound} new 404s");
    Environment.Exit(130);
};

try
{
    if (!movesOnly)
    {
        Console.WriteLine($"=== Items ({itemGaps.Count}) ===");
        foreach (var (key, name) in itemGaps)
        {
            if (fetched >= limit) break;
            await ScrapeAsync("item", key, name, cachedItems, notFoundItemSet);
            if (fetched % 25 == 0 && fetched > 0) Save();
        }
    }

    if (!itemsOnly)
    {
        Console.WriteLine($"\n=== Moves ({moveGaps.Count}) ===");
        foreach (var (id, name) in moveGaps)
        {
            if (fetched >= limit) break;
            await ScrapeAsync("move", id, name, cachedMoves, notFoundMoveSet);
            if (fetched % 25 == 0 && fetched > 0) Save();
        }
    }
}
finally
{
    Save();
}

Console.WriteLine();
Console.WriteLine($"Done. Fetched {fetched}, cached {newDescriptions} new descriptions, " +
                  $"{newNotFound} new 404s, {skipped} skipped, {errors} errors.");
Console.WriteLine($"Output: {outputPath}");
return 0;
