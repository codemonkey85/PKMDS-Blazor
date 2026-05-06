namespace Pkmds.Rcl.Components.Dialogs;

public partial class ShowdownImportDialog
{
    private string? fetchError;
    private bool importToParty;

    private string inputText = string.Empty;
    private bool isFetching;
    private bool isParsing;
    private List<ParsedEntry> parsedEntries = [];
    private SaveFile? parsedWithSaveFile;
    private string? pasteInfo;

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Inject]
    private HttpClient Http { get; set; } = null!;

    private bool IsUrl => PokepasteTeam.IsURL(inputText, out _);

    private async Task ParseTextAsync()
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        isParsing = true;
        parsedEntries = [];
        // Record which save the entries are valid for so Import can detect a mismatch
        // (e.g. the save was swapped between parse and import).
        parsedWithSaveFile = AppState.SaveFile;
        StateHasChanged();

        try
        {
            // Yield so the spinner paints before per-set conversion + legality analysis runs.
            // Task.Delay(1) (not Task.Yield) because in Blazor WASM, Yield stays on the same
            // JS macrotask and never lets the browser paint.
            await Task.Delay(1);

            var sets = AppService.ParseShowdownText(inputText);
            var entries = new List<ParsedEntry>(sets.Count);
            foreach (var set in sets)
            {
                var pkm = AppService.ConvertShowdownSetToPkm(set);
                var (status, firstIssue) = AnalyzeLegality(pkm);
                entries.Add(new ParsedEntry(set, pkm, status, firstIssue));

                // Inter-entry yield so the browser can process input between conversions,
                // matching the pattern used by LegalityReportTab's batch legalize sweep.
                await Task.Delay(1);
            }

            parsedEntries = entries;
        }
        finally
        {
            // Always clear the spinner / re-enable inputs, even if conversion or analysis
            // threw — otherwise the dialog can get stuck in "Analyzing…" forever.
            isParsing = false;
            StateHasChanged();
        }
    }

    private (LegalityStatus? Status, string FirstIssue) AnalyzeLegality(PKM? pkm)
    {
        if (pkm is null)
        {
            return (null, string.Empty);
        }

        var la = AppService.GetLegalityAnalysis(pkm);
        var status = LegalityUi.GetStatus(la);
        var firstIssue = status == LegalityStatus.Legal
            ? string.Empty
            : LegalityUi.GetFirstIssue(la);
        return (status, firstIssue);
    }

    private static string GetStatusTooltip(ParsedEntry entry) =>
        string.IsNullOrEmpty(entry.FirstIssue)
            ? LegalityUi.GetStatusLabel(entry.Status ?? LegalityStatus.Legal)
            : entry.FirstIssue;

    private async Task FetchUrlAsync()
    {
        if (!PokepasteTeam.IsURL(inputText, out var rawUrl))
        {
            return;
        }

        isFetching = true;
        fetchError = null;
        pasteInfo = null;

        // Convert the /raw URL (returned by PokepasteTeam.IsURL) to the /json endpoint,
        // which also sets Access-Control-Allow-Origin: * and provides title/author/notes.
        var jsonUrl = rawUrl.EndsWith("/raw", StringComparison.OrdinalIgnoreCase)
            ? string.Concat(rawUrl.AsSpan(0, rawUrl.Length - 4), "/json")
            : rawUrl + "/json";

        try
        {
            var json = await Http.GetStringAsync(jsonUrl);
            var response = JsonSerializer.Deserialize<PokePasteResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response is null || string.IsNullOrWhiteSpace(response.Paste))
            {
                fetchError = "Received an empty paste from PokePaste.";
            }
            else
            {
                inputText = response.Paste;
                pasteInfo = BuildPasteInfo(response);
                isFetching = false;
                await ParseTextAsync();
                return;
            }
        }
        catch (Exception ex)
        {
            fetchError = $"Failed to fetch from PokePaste: {ex.Message}";
        }

        isFetching = false;
    }

    private static string? BuildPasteInfo(PokePasteResponse response)
    {
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(response.Title))
        {
            parts.Add($"Title: {response.Title}");
        }

        if (!string.IsNullOrWhiteSpace(response.Author))
        {
            parts.Add($"Author: {response.Author}");
        }

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : null;
    }

    private string GetSetSummary(ShowdownSet set)
    {
        var strings = GameInfo.Strings;
        var speciesName = strings.Species.Count > set.Species
            ? strings.Species[set.Species]
            : set.Species.ToString();

        var parts = new List<string>(3);

        if (!string.IsNullOrEmpty(set.Nickname) && !set.Nickname.Equals(speciesName, StringComparison.Ordinal))
        {
            parts.Add($"{set.Nickname} ({speciesName})");
        }
        else
        {
            parts.Add(speciesName);
        }

        parts.Add($"Lv.{set.Level}");

        // ShowdownSet.HeldItem is in the set's Context-specific item ID space, which differs
        // from the modern (Gen 4+) `strings.Item` table for Gen 1/2/3/4/8b/9/9a sets.
        var setItems = strings.GetItemStrings(set.Context);
        if (set.HeldItem > 0 && set.HeldItem < setItems.Length)
        {
            parts.Add($"@ {setItems[set.HeldItem]}");
        }

        return string.Join(" ", parts);
    }

    private static string GetWarnings(ShowdownSet set)
    {
        var localization = BattleTemplateParseErrorLocalization.Get();
        return string.Join("\n", set.InvalidLines.Select(e => e.Humanize(localization)));
    }

    private async Task ImportAsync()
    {
        if (AppState.SaveFile is not { } sav)
        {
            Snackbar.Add("No save file loaded.", Severity.Warning);
            return;
        }

        // Defensive: the dialog is modal so the save shouldn't change mid-flight, but if
        // it somehow did (or the user parsed before loading a save), the stored PKMs
        // belong to a different target and must not be imported.
        if (!ReferenceEquals(parsedWithSaveFile, sav))
        {
            Snackbar.Add("Save file changed since parsing. Please parse again.", Severity.Warning);
            parsedEntries = [];
            parsedWithSaveFile = null;
            StateHasChanged();
            return;
        }

        // Reuse the pre-converted PKMs from parsing — no need to re-run the legalization
        // engine at import time. Drop entries whose species/form isn't in the target
        // game: when the Auto-Legality engine can't produce a valid encounter (e.g. a
        // Gen 9 species against a Gen 5 save) it silently substitutes a different
        // species on the PKM, so we can't trust pkm.Species — check the *set's* species,
        // which preserves the user's original intent. Illegal-but-present entries are
        // still imported — users can legalize them after.
        var converted = new List<PKM>(parsedEntries.Count);
        var conversionFailed = 0;
        var skippedImpossible = 0;
        var illegalAmongConverted = 0;
        foreach (var entry in parsedEntries)
        {
            // Reject blank PKMs (Species == 0). LegalizationService falls back to a
            // blank when no encounter survives pre-filtering; placing one would silently
            // succeed (writing a Species=0 entity to an empty slot is a no-op) and the
            // user would see a green snackbar with no Pokémon imported.
            if (entry.Pokemon is not { Species: > 0 } pkm)
            {
                conversionFailed++;
                continue;
            }

            if (entry.Set.Species == 0 ||
                !sav.Personal.IsPresentInGame(entry.Set.Species, entry.Set.Form))
            {
                skippedImpossible++;
                continue;
            }

            converted.Add(pkm);
            // The legality engine's BuildSetFallback / SuccessHaX paths produce populated
            // but illegal PKMs (e.g. Garchomp@15 in BDSP). They survive the Species/IsPresent
            // filters above, so without this tracking the snackbar would say "Imported 1
            // Pokémon" with green Success severity even though the result is plainly illegal.
            if (entry.Status == LegalityStatus.Illegal)
            {
                illegalAmongConverted++;
            }
        }

        if (converted.Count == 0)
        {
            ReportResult(0, conversionFailed, skippedImpossible, illegalAmongConverted);
            MudDialog?.Close();
            return;
        }

        if (importToParty)
        {
            await ImportToPartyAsync(sav, converted, conversionFailed, skippedImpossible, illegalAmongConverted);
        }
        else
        {
            ImportToBox(converted, conversionFailed, skippedImpossible, illegalAmongConverted);
        }
    }

    private async Task ImportToPartyAsync(SaveFile sav, List<PKM> converted, int conversionFailed, int skippedImpossible, int illegalAmongConverted)
    {
        var emptySlots = 6 - sav.PartyCount;

        if (converted.Count <= emptySlots)
        {
            // Enough empty slots — just fill them.
            var placed = 0;
            foreach (var pkm in converted)
            {
                if (AppService.TryPlacePokemonInPartySlot(pkm))
                {
                    placed++;
                }
            }

            ReportResult(placed, conversionFailed + (converted.Count - placed), skippedImpossible, illegalAmongConverted);
            MudDialog?.Close();
            return;
        }

        // Party doesn't have enough room — ask to overwrite.
        var names = string.Join(", ", converted
            .Take(6)
            .Select(p => AppService.GetPokemonSpeciesName(p.Species) ?? p.Species.ToString()));

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Overwrite Party?",
            $"The party doesn't have enough empty slots for all {converted.Count} Pokémon. " +
            $"Replace the entire party with: {names}?",
            yesText: "Overwrite",
            cancelText: "Cancel");

        if (confirm != true)
        {
            return;
        }

        var written = AppService.OverwriteParty(converted);
        var skipped = Math.Max(0, converted.Count - written);
        ReportResult(written, conversionFailed + skipped, skippedImpossible, illegalAmongConverted);
        MudDialog?.Close();
    }

    private void ImportToBox(List<PKM> converted, int conversionFailed, int skippedImpossible, int illegalAmongConverted)
    {
        var placed = 0;
        foreach (var pkm in converted)
        {
            if (AppService.TryPlacePokemonInFirstAvailableSlot(pkm))
            {
                placed++;
            }
        }

        RefreshService.RefreshBoxState();
        ReportResult(placed, conversionFailed + (converted.Count - placed), skippedImpossible, illegalAmongConverted);
        MudDialog?.Close();
    }

    private void ReportResult(int imported, int failed, int skippedImpossible, int illegal)
    {
        if (imported == 0 && failed == 0 && skippedImpossible == 0)
        {
            return;
        }

        // Cap illegal at imported — placement failures might have dropped some illegal
        // entries, and we can't tell which. Reporting more illegal than imported would
        // read as nonsense to the user.
        var illegalImported = Math.Min(illegal, imported);

        var parts = new List<string>(3);
        if (failed > 0)
        {
            parts.Add($"{failed} could not be placed");
        }

        if (skippedImpossible > 0)
        {
            parts.Add($"{skippedImpossible} skipped (not in this game)");
        }

        if (illegalImported > 0)
        {
            parts.Add($"{illegalImported} illegal");
        }

        var message = parts.Count == 0
            ? $"Imported {imported} Pokémon."
            : $"Imported {imported} Pokémon. {string.Join(", ", parts)}.";

        var severity = parts.Count == 0
            ? Severity.Success
            : Severity.Warning;

        Snackbar.Add(message, severity);
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());

    private sealed record ParsedEntry(
        ShowdownSet Set,
        PKM? Pokemon,
        LegalityStatus? Status,
        string FirstIssue);
}
