#!/usr/bin/env dotnet-run
#:property PublishAot=false
#:include shared/ToolPaths.cs
/*
 * Generate tm-data.json from the Bulbapedia "List of TMs" page.
 *
 * The output maps each game version key to a dict of TM/TR-number -> move-name.
 * TM numbers preserve their zero-padded format from the page (e.g. "01", "001").
 * TR entries for Sword/Shield use "TR00"-"TR99" keys in the gen8swsh section.
 *
 * Usage:
 *   dotnet run tools/generate-tm-data.cs [-- [--url <url>] [--input path/to/file.html] [--output /path/to/output]]
 *
 * Arguments:
 *   --input   Path to a saved Bulbapedia HTML file (optional; fetches live if omitted).
 *   --url     URL to fetch HTML from (default: https://bulbapedia.bulbagarden.net/wiki/List_of_TMs).
 *             Ignored when --input is provided.
 *   --output  Output directory for tm-data.json.
 *             Defaults to ../Pkmds.Rcl/wwwroot/data/ relative to the repo root.
 *
 * Table order on the Bulbapedia page (12 tables in order):
 *   gen1, gen2, gen3, gen4, gen5, gen6,
 *   gen7sm, gen7lgpe, gen8swsh, gen8bdsp, gen9sv, gen9za
 *
 * Sword/Shield TR data is sourced from PKHeX.Core PersonalInfo8SWSH.MachineMovesRecord
 * (the Bulbapedia equivalent is at:
 *  https://bulbapedia.bulbagarden.net/wiki/List_of_TMs_and_TRs_in_Pok%C3%A9mon_Sword_and_Shield)
 * and merged into gen8swsh under "TR00"-"TR99" keys.
 *
 * Game-version-key mapping (used by DescriptionService.ToTmDataKey):
 *   gen1     -> Red/Green/Blue/Yellow
 *   gen2     -> Gold/Silver/Crystal
 *   gen3     -> Ruby/Sapphire/Emerald/FireRed/LeafGreen/Colosseum/XD
 *   gen4     -> Diamond/Pearl/Platinum/HeartGold/SoulSilver
 *   gen5     -> Black/White/Black2/White2
 *   gen6     -> X/Y/OmegaRuby/AlphaSapphire
 *   gen7sm   -> Sun/Moon/UltraSun/UltraMoon
 *   gen7lgpe -> Let's Go Pikachu/Eevee
 *   gen8swsh -> Sword/Shield (TMs as "00"-"99", TRs as "TR00"-"TR99")
 *   gen8bdsp -> BrilliantDiamond/ShiningPearl
 *   gen9sv   -> Scarlet/Violet
 *   gen9za   -> Legends Z-A
 */

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Pkmds.Tools;

// ---------------------------------------------------------------------------
// Sword/Shield TR move IDs (TR00-TR99)
// Source: PKHeX.Core PersonalInfo8SWSH.MachineMovesRecord
// Ref:    https://bulbapedia.bulbagarden.net/wiki/List_of_TMs_and_TRs_in_Pok%C3%A9mon_Sword_and_Shield
// ---------------------------------------------------------------------------
int[] SwshTrMoveIds =
[
     14,  34,  53,  56,  57,  58,  59,  67,  85,  87,
     89,  94,  97, 116, 118, 126, 127, 133, 141, 161,
    164, 179, 188, 191, 200, 473, 203, 214, 224, 226,
    227, 231, 242, 247, 248, 253, 257, 269, 271, 276,
    285, 299, 304, 315, 322, 330, 334, 337, 339, 347,
    348, 349, 360, 370, 390, 394, 396, 398, 399, 402,
    404, 405, 406, 408, 411, 412, 413, 414, 417, 428,
    430, 437, 438, 441, 442, 444, 446, 447, 482, 484,
    486, 492, 500, 502, 503, 526, 528, 529, 535, 542,
    583, 599, 605, 663, 667, 675, 676, 706, 710, 776,
];

string[] TableKeys =
[
    "gen1", "gen2", "gen3", "gen4", "gen5", "gen6",
    "gen7sm", "gen7lgpe", "gen8swsh", "gen8bdsp", "gen9sv", "gen9za",
];

// ---------------------------------------------------------------------------
// Args
// ---------------------------------------------------------------------------

const string DefaultBulbapediaUrl = "https://bulbapedia.bulbagarden.net/wiki/List_of_TMs";

string? inputArg = null;
string? urlArg = null;
string? outputArg = null;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--input" && i + 1 < args.Length) inputArg = args[++i];
    else if (args[i] == "--url" && i + 1 < args.Length) urlArg = args[++i];
    else if (args[i] == "--output" && i + 1 < args.Length) outputArg = args[++i];
}

var outputDir = outputArg is not null
    ? Path.GetFullPath(outputArg)
    : ToolPaths.FindDataDirectory()
      ?? throw new DirectoryNotFoundException("Could not find Pkmds.Rcl/ in any parent directory. Use --output to specify the output path.");

Directory.CreateDirectory(outputDir);

string html;
if (inputArg is not null)
{
    var inputPath = Path.GetFullPath(inputArg);
    if (!File.Exists(inputPath))
    {
        Console.Error.WriteLine($"ERROR: Input file not found: {inputPath}");
        return 1;
    }
    Console.WriteLine($"Reading HTML from : {inputPath}");
    Console.WriteLine($"Writing JSON to   : {outputDir}");
    Console.WriteLine();
    html = File.ReadAllText(inputPath, Encoding.UTF8);
}
else
{
    var fetchUrl = urlArg ?? DefaultBulbapediaUrl;
    Console.WriteLine($"Fetching HTML from: {fetchUrl}");
    Console.WriteLine($"Writing JSON to   : {outputDir}");
    Console.WriteLine();
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("User-Agent", "PKMDS-Blazor/1.0 (data-generation tool; +https://github.com/codemonkey85/PKMDS-Blazor)");
    html = await httpClient.GetStringAsync(fetchUrl);
}

// ---------------------------------------------------------------------------
// HTML parsing
// ---------------------------------------------------------------------------

string StripTags(string html) => Regex.Replace(html, "<[^>]+>", "");

string Clean(string text)
{
    var decoded = WebUtility.HtmlDecode(text);
    var stripped = StripTags(decoded);
    var normalized = stripped.Replace('\u00A0', ' ');
    // Remove footnote/annotation fragments (e.g. [1], [a], [note])
    var noFootnotes = Regex.Replace(normalized, @"\[[^\]]{1,10}\]", "");
    return Regex.Replace(noFootnotes, @"\s+", " ").Trim();
}

List<Dictionary<string, string>> ParseTables(string html)
{
    var results = new List<Dictionary<string, string>>();
    var tablePattern = new Regex(@"<table[^>]*roundtable[^>]*>(.*?)</table>", RegexOptions.Singleline);
    var rowPattern = new Regex(@"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline);
    var cellPattern = new Regex(@"<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline);

    foreach (Match tableMatch in tablePattern.Matches(html))
    {
        var tableBody = tableMatch.Groups[1].Value;
        var tmMap = new Dictionary<string, string>();

        foreach (Match rowMatch in rowPattern.Matches(tableBody))
        {
            var cells = cellPattern.Matches(rowMatch.Groups[1].Value)
                .Select(c => Clean(c.Groups[1].Value))
                .ToList();

            if (cells.Count < 2) continue;
            var tmNum = cells[0];
            var moveName = cells[1];

            // Skip header rows
            if (tmNum is "#" or "TM" or "HM" || moveName is "Move" or "") continue;
            // Validate: TM number should be digits only
            if (!tmNum.All(char.IsDigit)) continue;

            tmMap[tmNum] = moveName;
        }

        results.Add(tmMap);
    }

    return results;
}

Dictionary<string, string> BuildSwshTrMap(JsonObject moveInfo)
{
    var result = new Dictionary<string, string>();
    for (var i = 0; i < SwshTrMoveIds.Length; i++)
    {
        var moveId = SwshTrMoveIds[i].ToString();
        if (moveInfo[moveId] is JsonObject entry && entry["name"] is JsonValue nameVal)
            result[$"TR{i:D2}"] = nameVal.GetValue<string>();
    }
    return result;
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

var tables = ParseTables(html);

if (tables.Count != TableKeys.Length)
    Console.Error.WriteLine($"WARNING: Expected {TableKeys.Length} tables, found {tables.Count}. The page structure may have changed.");

// Load move-info.json to resolve TR move IDs -> names
var moveInfoPath = Path.Combine(outputDir, "move-info.json");
JsonObject moveInfo;
if (!File.Exists(moveInfoPath))
{
    Console.Error.WriteLine($"WARNING: {moveInfoPath} not found — SWSH TR names will be omitted. Run generate-descriptions.cs first.");
    moveInfo = [];
}
else
{
    moveInfo = JsonNode.Parse(File.ReadAllText(moveInfoPath, Encoding.UTF8))!.AsObject();
}

var serializerOptions = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    WriteIndented = false,
};

var result = new JsonObject();
for (var i = 0; i < Math.Min(tables.Count, TableKeys.Length); i++)
{
    var key = TableKeys[i];
    var tmMap = tables[i];

    if (key == "gen8swsh")
    {
        var trMap = BuildSwshTrMap(moveInfo);
        foreach (var (k, v) in trMap) tmMap[k] = v;
        Console.WriteLine($"  {key}: {tmMap.Count - trMap.Count} TMs + {trMap.Count} TRs");
    }
    else
    {
        Console.WriteLine($"  {key}: {tmMap.Count} TMs");
    }

    var section = new JsonObject();
    foreach (var (k, v) in tmMap) section[k] = v;
    result[key] = section;
}

var outPath = Path.Combine(outputDir, "tm-data.json");
File.WriteAllText(outPath, result.ToJsonString(serializerOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
var sizeKb = new FileInfo(outPath).Length / 1024;

Console.WriteLine();
Console.WriteLine($"Done -> tm-data.json ({sizeKb} KB)");
return 0;
