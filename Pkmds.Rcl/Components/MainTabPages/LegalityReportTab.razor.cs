namespace Pkmds.Rcl.Components.MainTabPages;

public partial class LegalityReportTab : RefreshAwareComponent
{
    private bool hasRun;
    private bool isLegalizing;
    private bool isScanning;
    private List<LegalityReportEntry> legalityReportEntries = [];
    private double legalizationPercent;
    private string legalizationStatusText = string.Empty;
    private CancellationTokenSource? legalizeCts;
    private LegalityStatus? statusFilter;

    // Field-level diffs collected during the most recent batch legalize, keyed by
    // (Box, Slot) for box entries and (-1, partySlot) for party entries. Only the
    // rows that flipped to Legal get an entry — everything else stays absent so
    // the row's "View changes" affordance can hide cleanly.
    private readonly Dictionary<(int Box, int Slot), LegalizationChanges> changesByLocation = [];

    /// <summary>
    /// Callback invoked after a row is clicked to jump to the Party / Box tab.
    /// </summary>
    [Parameter]
    public EventCallback OnJumpToPartyBox { get; set; }

    private int LegalCount => legalityReportEntries.Count(e => e.Status == LegalityStatus.Legal);
    private int FishyCount => legalityReportEntries.Count(e => e.Status == LegalityStatus.Fishy);
    private int IllegalCount => legalityReportEntries.Count(e => e.Status == LegalityStatus.Illegal);

    private bool HasIllegalOrFishy => hasRun && legalityReportEntries.Any(e =>
        e.Status is LegalityStatus.Illegal or LegalityStatus.Fishy);

    private async Task LegalizeAllAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        // Process Illegal entries before Fishy ones — if the user cancels partway
        // through a long sweep, the more severe issues are fixed first.
        var targets = legalityReportEntries
            .Where(e => e.Status is LegalityStatus.Illegal or LegalityStatus.Fishy)
            .OrderBy(e => e.Status == LegalityStatus.Illegal
                ? 0
                : 1)
            .ToList();

        if (targets.Count == 0)
        {
            return;
        }

        legalizeCts?.Dispose();
        legalizeCts = new CancellationTokenSource();
        var ct = legalizeCts.Token;

        // Drop any diffs from a prior run — they don't apply once we re-legalize.
        changesByLocation.Clear();

        isLegalizing = true;
        legalizationPercent = 0;
        legalizationStatusText = $"Legalizing 0/{targets.Count}…";
        StateHasChanged();

        await Task.Yield();

        var successCount = 0;
        var fishyCount = 0;
        var failureCount = 0;
        var cancelled = false;
        var processed = 0;

        for (var i = 0; i < targets.Count; i++)
        {
            if (ct.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            var entry = targets[i];
            legalizationStatusText = $"Legalizing {i + 1}/{targets.Count}: {entry.SpeciesName} ({entry.Location})";
            legalizationPercent = (double)i / targets.Count * 100;
            StateHasChanged();
            // Task.Delay(1) rather than Task.Yield(): in Blazor WASM, Yield stays on the
            // same JS macrotask and the browser never paints. Delay(1) uses setTimeout and
            // actually releases the main thread so the UI can render and process clicks
            // (including the Cancel button).
            try
            {
                await Task.Delay(1, ct);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                break;
            }

            LegalizationOutcome result;
            try
            {
                // Tighter per-Pokémon timeout for batch runs than the 15s default: a
                // hundreds-of-Pokémon sweep can't afford to spend 15s apiece, and the
                // tighter cap also stops any one attempt from monopolising the WASM
                // thread long enough to trip "Page Unresponsive".
                result = await LegalizationService.LegalizeAsync(
                    entry.Pokemon,
                    saveFile,
                    progress: null,
                    ct,
                    timeoutSeconds: 5);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                break;
            }
            catch
            {
                failureCount++;
                processed++;
                continue;
            }

            processed++;

            if (result.Status != LegalizationStatus.Success)
            {
                failureCount++;
                continue;
            }

            // Pass EntityImportSettings.None to bypass SaveFile.UpdatePKM's "adapt as if
            // traded in" path. The PKM is already legal for the current save; letting
            // SetPKM mutate handler/memory/trainer fields can reintroduce illegalities
            // that our pre-write analysis doesn't see.
            PKM storedPk;
            if (entry.IsParty)
            {
                saveFile.SetPartySlotAtIndex(result.Pokemon, entry.SlotNumber, EntityImportSettings.None);
                storedPk = saveFile.GetPartySlotAtIndex(entry.SlotNumber);
            }
            else
            {
                saveFile.SetBoxSlotAtIndex(result.Pokemon, entry.BoxNumber, entry.SlotNumber, EntityImportSettings.None);
                storedPk = saveFile.GetBoxSlotAtIndex(entry.BoxNumber, entry.SlotNumber);
            }

            // Re-read the stored bytes and re-analyse so the table reflects exactly what
            // the save now contains — if the round trip mutated the PKM, we'll see the
            // real status here rather than an optimistic pre-write one.
            var storedLa = AppService.GetLegalityAnalysis(storedPk, isParty: entry.IsParty);
            var newStatus = LegalityUi.GetStatus(storedLa);
            var updated = entry with
            {
                Pokemon = storedPk,
                Status = newStatus,
                FirstIssue = newStatus == LegalityStatus.Legal
                    ? string.Empty
                    : LegalityUi.GetFirstIssue(storedLa)
            };
            var idx = legalityReportEntries.IndexOf(entry);
            if (idx >= 0)
            {
                legalityReportEntries[idx] = updated;
            }

            switch (newStatus)
            {
                case LegalityStatus.Legal:
                    successCount++;
                    // Recompute the diff against the bytes actually stored in the save
                    // rather than reusing result.Changes (which was diffed against the
                    // in-memory result.Pokemon before SetBoxSlot/SetPartySlot ran). The
                    // EntityImportSettings.None write path can still serialize/deserialize
                    // and trim trailing fields, so the row's "View changes" must reflect
                    // what the user will see if they re-scan.
                    var storedChanges = PkmDiffer.Diff(entry.Pokemon, storedPk);
                    if (!storedChanges.IsEmpty)
                    {
                        // Index by the row's location, not the entry instance, because the
                        // entry was replaced above with a new record and any future re-scan
                        // will rebuild new instances entirely.
                        var key = entry.IsParty
                            ? (-1, entry.SlotNumber)
                            : (entry.BoxNumber, entry.SlotNumber);
                        changesByLocation[key] = storedChanges;
                    }

                    break;
                case LegalityStatus.Fishy:
                    // Valid PKHeX-wise but still flagged Fishy — count separately so the
                    // summary reflects the partial improvement honestly.
                    fishyCount++;
                    break;
                default:
                    failureCount++;
                    break;
            }
        }

        legalizationPercent = 100;
        StateHasChanged();

        var summary = BuildLegalizeSummary(targets.Count, processed, successCount, fishyCount, failureCount, cancelled);
        Snackbar.Add(summary.Message, summary.Severity);

        // Skip a haptic on plain cancellation — the user already knows; only signal completion.
        if (!cancelled)
        {
            if (successCount > 0 && failureCount == 0)
            {
                Haptics.Success();
            }
            else if (successCount == 0 && failureCount > 0)
            {
                Haptics.Error();
            }
            else
            {
                Haptics.Confirm();
            }
        }

        isLegalizing = false;
        legalizationStatusText = string.Empty;
        legalizationPercent = 0;
        legalizeCts.Dispose();
        legalizeCts = null;

        RefreshService.Refresh();
    }

    private void CancelLegalizeAll() => legalizeCts?.Cancel();

    private static (string Message, Severity Severity) BuildLegalizeSummary(
        int targetCount,
        int processed,
        int successCount,
        int fishyCount,
        int failureCount,
        bool cancelled)
    {
        var prefix = cancelled
            ? $"Legalization cancelled ({processed}/{targetCount} processed)."
            : $"Processed {targetCount} Pokémon.";

        var parts = new List<string>();
        if (successCount > 0)
        {
            parts.Add($"now Legal: {successCount}");
        }

        if (fishyCount > 0)
        {
            parts.Add($"still Fishy: {fishyCount}");
        }

        if (failureCount > 0)
        {
            parts.Add($"could not fix: {failureCount}");
        }

        var detail = parts.Count > 0
            ? " " + string.Join(", ", parts) + "."
            : string.Empty;

        var severity = cancelled
            ? Severity.Info
            : failureCount == 0 && fishyCount == 0
                ? Severity.Success
                : Severity.Warning;

        return (prefix + detail, severity);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            legalizeCts?.Cancel();
            legalizeCts?.Dispose();
            legalizeCts = null;
        }

        base.Dispose(disposing);
    }

    private async Task RunScanAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        isScanning = true;
        hasRun = false;
        legalityReportEntries = [];
        // A re-scan invalidates whatever the old diffs claimed; the entries they keyed
        // were re-fetched from the save and may not match anymore.
        changesByLocation.Clear();
        statusFilter = null;
        StateHasChanged();

        // Yield once so the spinner renders before the CPU-bound sweep begins.
        await Task.Yield();

        var entries = new List<LegalityReportEntry>();

        // --- Party slots ---
        for (var i = 0; i < saveFile.PartyCount; i++)
        {
            var pkm = saveFile.GetPartySlotAtIndex(i);
            if (pkm is not { Species: > 0 })
            {
                continue;
            }

            var la = AppService.GetLegalityAnalysis(pkm, isParty: true);
            entries.Add(BuildEntry(pkm, la, true, 0, i));
        }

        // --- Box slots (yield after every box to keep the UI responsive) ---
        for (var box = 0; box < saveFile.BoxCount; box++)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var pkm = saveFile.GetBoxSlotAtIndex(box, slot);
                if (pkm is not { Species: > 0 })
                {
                    continue;
                }

                var la = AppService.GetLegalityAnalysis(pkm);
                entries.Add(BuildEntry(pkm, la, false, box, slot));
            }

            // Yield after every box so the progress spinner stays animated.
            await Task.Yield();
        }

        legalityReportEntries = entries;
        isScanning = false;
        hasRun = true;
        StateHasChanged();
    }

    private LegalityReportEntry BuildEntry(PKM pkm, LegalityAnalysis la, bool isParty, int box, int slot)
    {
        var speciesName = AppService.GetPokemonSpeciesName(pkm.Species) ??
                          pkm.Species.ToString(CultureInfo.InvariantCulture);
        var location = isParty
            ? $"Party {slot + 1}"
            : $"Box {box + 1}, Slot {slot + 1}";

        return new LegalityReportEntry
        {
            Pokemon = pkm,
            SpeciesName = speciesName,
            Location = location,
            Status = LegalityUi.GetStatus(la),
            FirstIssue = LegalityUi.GetFirstIssue(la),
            IsParty = isParty,
            BoxNumber = box,
            SlotNumber = slot
        };
    }

    private void SetFilter(LegalityStatus? filter) => statusFilter = filter;

    private async Task OnRowClickAsync(TableRowClickEventArgs<LegalityReportEntry> args)
    {
        if (args.Item is not { } entry)
        {
            return;
        }

        if (!await UnsavedChangesGuard.ConfirmAsync(
                AppService,
                DialogService,
                "This Pokémon has unsaved changes. Save them to the slot before jumping to the report entry?",
                snackbar: Snackbar))
        {
            return;
        }

        if (entry.IsParty)
        {
            AppService.SetSelectedPartyPokemon(entry.Pokemon, entry.SlotNumber);
        }
        else if (AppState.SaveFile is SAV7b lgsave)
        {
            // Let's Go renders all boxes as a single flat scrollable list.
            // Convert box+slot to the flat index (0..999) that SetSelectedLetsGoPokemon expects.
            var flatSlot = entry.BoxNumber * lgsave.BoxSlotCount + entry.SlotNumber;
            AppService.SetSelectedLetsGoPokemon(entry.Pokemon, flatSlot);
        }
        else
        {
            AppService.SetSelectedBoxPokemon(entry.Pokemon, entry.BoxNumber, entry.SlotNumber);
        }

        await OnJumpToPartyBox.InvokeAsync();
    }

    private bool TableFilterFunction(LegalityReportEntry legalityReportEntry) =>
        statusFilter is null || legalityReportEntry.Status == statusFilter;

    private bool HasChangesFor(LegalityReportEntry entry) =>
        changesByLocation.ContainsKey(GetLocationKey(entry));

    private async Task ShowChangesDialogAsync(LegalityReportEntry entry)
    {
        if (!changesByLocation.TryGetValue(GetLocationKey(entry), out var changes) || changes.IsEmpty)
        {
            return;
        }

        var label = $"{entry.SpeciesName} ({entry.Location})";
        var parameters = new DialogParameters<LegalizationChangesDialog>
        {
            { x => x.Changes, changes },
            { x => x.PokemonLabel, label }
        };

        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<LegalizationChangesDialog>(
            "Legalization Changes", parameters, options);
    }

    private static (int Box, int Slot) GetLocationKey(LegalityReportEntry entry) =>
        entry.IsParty ? (-1, entry.SlotNumber) : (entry.BoxNumber, entry.SlotNumber);
}
