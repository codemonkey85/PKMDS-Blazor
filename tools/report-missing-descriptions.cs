#!/usr/bin/env dotnet-run
#:property PublishAot=false
#:include shared/ToolPaths.cs
/*
 * Report entries in ability-info.json / move-info.json / item-info.json that are
 * missing a description, per-gen flavor, or both.
 *
 * This is a diagnostic tool — it reads the generated JSON files and produces a
 * plain-text report grouping missing entries by dataset and category. Use it to
 * track progress toward full coverage as PokeAPI / Showdown / other sources fill
 * in gaps upstream.
 *
 * Usage:
 *   dotnet run tools/report-missing-descriptions.cs [-- [--data <dir>] [--output <path>]]
 *
 * Arguments:
 *   --data    Directory containing ability-info.json, move-info.json, item-info.json.
 *             Defaults to Pkmds.Rcl/wwwroot/data/ under the repo root.
 *   --output  Output file path. Defaults to missing-flavor-report.txt at the repo root.
 *             Pass "-" to write to stdout.
 *
 * Categories:
 *   RUNTIME UI GAP       — description empty AND no populated flavor entries. This is what
 *                          surfaces as "No description available" in tooltips. Priority list.
 *   DATA COMPLETENESS    — either description OR flavor missing (but the UI still has
 *                          something to render). DescriptionService prefers gen-appropriate
 *                          flavor over the top-level description, so these entries look fine
 *                          in the UI today. Informational; chase for 100% data completeness.
 */

using System.Text;
using System.Text.Json.Nodes;
using Pkmds.Tools;

string? dataArg = null;
string? outputArg = null;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--data" && i + 1 < args.Length) dataArg = args[++i];
    else if (args[i] == "--output" && i + 1 < args.Length) outputArg = args[++i];
}

var dataDir = dataArg is not null
    ? Path.GetFullPath(dataArg)
    : ToolPaths.FindDataDirectory()
      ?? throw new DirectoryNotFoundException("Could not find Pkmds.Rcl/ in any parent directory. Use --data to specify the data path.");
if (!Directory.Exists(dataDir))
{
    Console.Error.WriteLine($"ERROR: data directory not found: {dataDir}");
    return 1;
}

var outputPath = outputArg switch
{
    "-"  => null,
    null => Path.Combine(ToolPaths.FindRepositoryRoot() ?? Environment.CurrentDirectory, "missing-flavor-report.txt"),
    _    => Path.GetFullPath(outputArg),
};

static bool IsEmpty(string? s) => string.IsNullOrWhiteSpace(s);

static bool AllFlavorsEmpty(JsonNode? flavor)
{
    if (flavor is not JsonObject obj || obj.Count == 0) return true;
    foreach (var (_, node) in obj)
        if (!IsEmpty(node?.GetValue<string>())) return false;
    return true;
}

static (List<string> RuntimeGap, List<string> DescOnly, List<string> FlavorOnly) Classify(
    JsonObject data, bool prefixIdInLabel)
{
    var runtimeGap = new List<string>();
    var descOnly = new List<string>();
    var flavorOnly = new List<string>();
    foreach (var (key, node) in data)
    {
        if (node is not JsonObject entry) continue;
        var name = (string?)entry["name"] ?? key;
        var label = prefixIdInLabel ? $"#{key} {name}" : name;
        var descEmpty = IsEmpty((string?)entry["description"]);
        var flavorEmpty = AllFlavorsEmpty(entry["flavor"]);
        if (descEmpty && flavorEmpty) runtimeGap.Add(label);
        else if (descEmpty) descOnly.Add(label);
        else if (flavorEmpty) flavorOnly.Add(label);
    }
    runtimeGap.Sort(StringComparer.OrdinalIgnoreCase);
    descOnly.Sort(StringComparer.OrdinalIgnoreCase);
    flavorOnly.Sort(StringComparer.OrdinalIgnoreCase);
    return (runtimeGap, descOnly, flavorOnly);
}

static JsonObject LoadJson(string path) =>
    (JsonObject)JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8))!;

var abilitiesPath = Path.Combine(dataDir, "ability-info.json");
var movesPath = Path.Combine(dataDir, "move-info.json");
var itemsPath = Path.Combine(dataDir, "item-info.json");

foreach (var p in new[] { abilitiesPath, movesPath, itemsPath })
    if (!File.Exists(p))
    {
        Console.Error.WriteLine($"ERROR: expected JSON not found: {p}");
        return 1;
    }

var abilities = LoadJson(abilitiesPath);
var moves = LoadJson(movesPath);
var items = LoadJson(itemsPath);

var classified = new (string Label, (List<string> RuntimeGap, List<string> DescOnly, List<string> FlavorOnly) Lists, int Total)[]
{
    ("Items",     Classify(items,     prefixIdInLabel: false), items.Count),
    ("Abilities", Classify(abilities, prefixIdInLabel: true),  abilities.Count),
    ("Moves",     Classify(moves,     prefixIdInLabel: true),  moves.Count),
};

var sb = new StringBuilder();
sb.AppendLine("=== Missing description/flavor report ===");
sb.AppendLine("Generated from Pkmds.Rcl/wwwroot/data/*.json after the latest generate-descriptions.cs run.");
sb.AppendLine("Rerun tools/report-missing-descriptions.cs to regenerate.");
sb.AppendLine();
sb.AppendLine("The report is split into two sections:");
sb.AppendLine();
sb.AppendLine("  1. RUNTIME UI GAPS — entries with no description AND no flavor. These surface");
sb.AppendLine("     as \"No description available\" in the UI. Priority list for filling in.");
sb.AppendLine();
sb.AppendLine("  2. DATA COMPLETENESS GAPS — entries missing either the description or the flavor");
sb.AppendLine("     map, but not both. DescriptionService prefers gen-appropriate flavor text, so");
sb.AppendLine("     these render fine in tooltips today. Chase these for full data parity, but");
sb.AppendLine("     they don't affect user-visible behavior.");
sb.AppendLine();

// --- Runtime UI gaps (priority) ---
sb.AppendLine("=".PadRight(72, '='));
sb.AppendLine("SECTION 1: RUNTIME UI GAPS (no description AND no flavor)");
sb.AppendLine("=".PadRight(72, '='));
sb.AppendLine();
var runtimeTotal = classified.Sum(c => c.Lists.RuntimeGap.Count);
sb.AppendLine($"Total runtime gaps across all datasets: {runtimeTotal}");
sb.AppendLine();
foreach (var (label, (runtimeGap, _, _), total) in classified)
{
    sb.AppendLine($"-- {label} ({runtimeGap.Count} of {total}) --");
    foreach (var n in runtimeGap) sb.AppendLine($"  {n}");
    sb.AppendLine();
}

// --- Data completeness (informational) ---
sb.AppendLine("=".PadRight(72, '='));
sb.AppendLine("SECTION 2: DATA COMPLETENESS GAPS (render fine today; chase for 100% coverage)");
sb.AppendLine("=".PadRight(72, '='));
sb.AppendLine();
var completenessTotal = classified.Sum(c => c.Lists.DescOnly.Count + c.Lists.FlavorOnly.Count);
sb.AppendLine($"Total data-completeness gaps across all datasets: {completenessTotal}");
sb.AppendLine();
foreach (var (label, (_, descOnly, flavorOnly), total) in classified)
{
    sb.AppendLine($"-- {label} ({descOnly.Count + flavorOnly.Count} of {total}) --");
    sb.AppendLine($"  description missing (flavor present): {descOnly.Count}");
    foreach (var n in descOnly) sb.AppendLine($"    {n}");
    sb.AppendLine($"  flavor missing (description present): {flavorOnly.Count}");
    foreach (var n in flavorOnly) sb.AppendLine($"    {n}");
    sb.AppendLine();
}

if (outputPath is null)
{
    Console.Write(sb.ToString());
}
else
{
    File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"Wrote {outputPath}");
}
return 0;
