using PKHexSeverity = PKHeX.Core.Severity;

namespace Pkmds.Rcl.Components.Dialogs;

public partial class BulkImportDialog : IDisposable
{
    // MudFileUpload requires an explicit upper bound; this is effectively "unlimited" for
    // a hand-curated bulk drop. A full Gen 8+ bank dump tops out at ~990 (32 boxes × 30 + party).
    private const int MaxBulkImportFiles = 9999;

    // Yield cadence inside the placement / legality loops. WASM is single-threaded —
    // without periodic yields the UI freezes mid-import.
    private const int YieldEveryN = 8;

    private static readonly string AcceptList = BuildAcceptList();

    private CancellationTokenSource? cts;
    private bool isImporting;
    private bool overwriteExisting;
    private double progressPercent;
    private BulkImportResult? result;
    private IReadOnlyList<IBrowserFile>? selectedFiles;
    private bool sendOverflowToBank = true;
    private string statusText = string.Empty;

    /// <summary>
    /// Files supplied up-front (e.g. by a multi-file drop on the box grid). When set,
    /// the file picker is hidden and these files are imported directly.
    /// </summary>
    [Parameter]
    public IReadOnlyList<(string FileName, byte[] Data)>? PreloadedFiles { get; set; }

    /// <summary>
    /// When <see langword="true" /> (the default), the placement scan fills boxes before
    /// the party. Set to <see langword="false" /> when the import was initiated from a
    /// party slot drop so the party fills first.
    /// </summary>
    [Parameter]
    public bool FillBoxesFirst { get; set; } = true;

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Inject]
    private IBankService BankService { get; set; } = null!;

    private bool HasPreloadedFiles => PreloadedFiles is { Count: > 0 };
    private bool HasAnyFiles => HasPreloadedFiles || selectedFiles is { Count: > 0 };

    public void Dispose()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private static string BuildAcceptList() =>
        string.Join(",", EntityFileExtension.GetExtensionsAll().Select(e => "." + e));

    private async Task ImportAsync()
    {
        if (AppState.SaveFile is not { } sav)
        {
            Snackbar.Add("No save file loaded.", Severity.Warning);
            return;
        }

        var sources = await LoadSourcesAsync();
        if (sources.Count == 0)
        {
            Snackbar.Add("No files to import.", Severity.Warning);
            return;
        }

        cts?.Dispose();
        cts = new CancellationTokenSource();
        var ct = cts.Token;

        isImporting = true;
        progressPercent = 0;
        statusText = $"Parsing {sources.Count} file(s)…";
        StateHasChanged();
        await Task.Yield();

        var parseSkipped = 0;
        var parsed = new List<PKM>(sources.Count);
        var placed = new List<PKM>();
        var overflow = new List<PKM>();
        var illegal = 0;
        var overflowToBank = 0;
        var cancelled = false;

        try
        {
            for (var i = 0; i < sources.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var (fileName, data) = sources[i];
                progressPercent = (double)i / sources.Count * 50;
                statusText = $"Parsing {i + 1}/{sources.Count}: {fileName}";

                var ext = Path.GetExtension(fileName);
                if (!FileUtil.TryGetPKM(data, out var pkm, ext, sav))
                {
                    parseSkipped++;
                    continue;
                }

                var converted = ConvertForSave(pkm, sav, AppState.IsHaXEnabled);
                if (converted is null)
                {
                    parseSkipped++;
                    continue;
                }

                sav.AdaptToSaveFile(converted);
                parsed.Add(converted);

                if ((i + 1) % YieldEveryN != 0)
                {
                    continue;
                }

                StateHasChanged();
                await Task.Delay(1, ct);
            }

            // Placement phase: write each PKM, then re-read the stored bytes so legality
            // analysis runs against what the save actually holds (handler/memory/trainer
            // fields can change on write). Lists are filled in place so cancellation
            // mid-loop preserves the partial counts for the summary.
            await PlacePokemonAsync(sav, parsed, placed, overflow, overwriteExisting, FillBoxesFirst, ct);

            // Legality phase: counter is updated via callback so cancellation mid-scan
            // still reports an accurate "checked so far" total in the summary.
            await CountIllegalAsync(placed, current => illegal = current, ct);

            if (overflow.Count > 0 && sendOverflowToBank)
            {
                statusText = $"Sending {overflow.Count} Pokémon to Bank…";
                progressPercent = 85;
                StateHasChanged();

                var tid = sav.DisplayTID.ToString(AppService.GetIdFormatString());
                var gameName = SaveFileNameDisplay.FriendlyGameName(sav.Version);
                var sourceSave = $"{sav.OT} ({tid}, {gameName})";
                await BankService.AddRangeAsync(overflow, sourceSave: sourceSave);
                overflowToBank = overflow.Count;
            }

            progressPercent = 100;
            statusText = "Done.";
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Import failed: {ex.Message}", Severity.Error);
            return;
        }
        finally
        {
            // Capture before nulling so a concurrent component Dispose (which also nulls
            // cts) can't cause us to leak the CTS — exactly one path disposes it.
            var localCts = cts;
            cts = null;
            localCts?.Dispose();
            isImporting = false;
            StateHasChanged();
        }

        result = new BulkImportResult(
            Imported: placed.Count,
            Skipped: parseSkipped,
            Illegal: illegal,
            OverflowRemaining: overflow.Count - overflowToBank,
            OverflowToBank: overflowToBank)
        { Cancelled = cancelled };

        RefreshService.RefreshBoxAndPartyState();
    }

    private async Task<List<(string FileName, byte[] Data)>> LoadSourcesAsync()
    {
        if (HasPreloadedFiles)
        {
            return PreloadedFiles!.ToList();
        }

        if (selectedFiles is null)
        {
            return [];
        }

        var list = new List<(string, byte[])>(selectedFiles.Count);
        foreach (var file in selectedFiles)
        {
            try
            {
                await using var stream = file.OpenReadStream(Constants.MaxFileSize);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                list.Add((file.Name, ms.ToArray()));
            }
            catch
            {
                // Treat unreadable files as skipped; let the parse phase report them.
                list.Add((file.Name, []));
            }
        }

        return list;
    }

    private static PKM? ConvertForSave(PKM pkm, SaveFile sav, bool isHaXEnabled)
    {
        var clone = pkm.Clone();
        if (clone.GetType() == sav.PKMType)
        {
            return clone;
        }

        var converted = EntityConverter.ConvertToType(pkm, sav.PKMType, out var result);

        if (result.IsSuccess && converted is not null || !isHaXEnabled)
        {
            return result.IsSuccess
                ? converted
                : null;
        }

        var previous = EntityConverter.AllowIncompatibleConversion;
        EntityConverter.AllowIncompatibleConversion = EntityCompatibilitySetting.AllowIncompatibleAll;
        try
        {
            converted = EntityConverter.ConvertToType(pkm, sav.PKMType, out result);
        }
        finally
        {
            EntityConverter.AllowIncompatibleConversion = previous;
        }

        return result.IsSuccess ? converted : null;
    }

    private async Task PlacePokemonAsync(
        SaveFile sav,
        IReadOnlyList<PKM> candidates,
        List<PKM> placed,
        List<PKM> overflow,
        bool overwrite,
        bool fillBoxesFirst,
        CancellationToken ct)
    {
        var total = candidates.Count;
        if (total == 0)
        {
            return;
        }

        statusText = $"Placing {total} Pokémon…";
        progressPercent = 50;
        StateHasChanged();
        await Task.Delay(1, ct);

        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();

            var location = TryPlaceSequential(sav, candidates[i], overwrite, fillBoxesFirst);
            if (location is { } loc)
            {
                // Re-read the stored entity. SetXxxSlotAtIndex can adjust handler/memory/
                // trainer fields, so the in-memory candidate may diverge from what's saved.
                var stored = loc.IsParty
                    ? sav.GetPartySlotAtIndex(loc.PartyIndex)
                    : sav.GetBoxSlotAtIndex(loc.Box, loc.Slot);
                placed.Add(stored);
            }
            else
            {
                overflow.Add(candidates[i]);
            }

            // Reserve 50→70% for placement.
            progressPercent = 50 + (double)(i + 1) / total * 20;
            statusText = $"Placing {i + 1}/{total}…";

            if ((i + 1) % YieldEveryN != 0 && i != total - 1)
            {
                continue;
            }

            StateHasChanged();
            await Task.Delay(1, ct);
        }
    }

    private async Task CountIllegalAsync(IReadOnlyList<PKM> placed, Action<int> updateCount, CancellationToken ct)
    {
        if (placed.Count == 0)
        {
            return;
        }

        var illegal = 0;
        for (var i = 0; i < placed.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var la = AppService.GetLegalityAnalysis(placed[i]);
            if (IsIllegal(la))
            {
                illegal++;
            }

            // Push the running count up so the caller's summary reflects partial progress
            // if the user cancels mid-scan.
            updateCount(illegal);

            // Reserve 70→85% for legality scan.
            progressPercent = 70 + (double)(i + 1) / placed.Count * 15;
            statusText = $"Checking legality {i + 1}/{placed.Count}…";

            if ((i + 1) % YieldEveryN != 0 && i != placed.Count - 1)
            {
                continue;
            }

            StateHasChanged();
            await Task.Delay(1, ct);
        }
    }

    private static PlacedSlot? TryPlaceSequential(SaveFile sav, PKM pkm, bool overwrite, bool fillBoxesFirst)
    {
        // Party is never overwritten — `sav.PartyCount < 6` means there's a grow slot,
        // matching IAppService.TryPlacePokemonInFirstAvailableSlot. The locked-slot guard
        // in TryFillBoxes is therefore intentionally box-only: party has no battle-team
        // lock concept (that flag only applies to box storage in Gen 5/6).
        if (fillBoxesFirst)
        {
            return TryFillBoxes(sav, pkm, overwrite) ?? TryFillParty(sav, pkm);
        }

        return TryFillParty(sav, pkm) ?? TryFillBoxes(sav, pkm, overwrite);
    }

    private static PlacedSlot? TryFillParty(SaveFile sav, PKM pkm)
    {
        if (sav.PartyCount >= 6)
        {
            return null;
        }

        var index = sav.PartyCount;
        sav.SetPartySlotAtIndex(pkm, index);
        return PlacedSlot.ForParty(index);
    }

    private static PlacedSlot? TryFillBoxes(SaveFile sav, PKM pkm, bool overwrite)
    {
        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                var existing = sav.GetBoxSlotAtIndex(box, slot);
                if (existing.Species != 0)
                {
                    if (!overwrite || !IsSlotEligibleForOverwrite(sav, box, slot))
                    {
                        continue;
                    }
                }

                sav.SetBoxSlotAtIndex(pkm, box, slot);
                return PlacedSlot.ForBox(box, slot);
            }
        }

        return null;
    }

    // Avoid stomping on locked battle-team slots (Gen 5/6). PKHeX's GetBoxSlotFlags
    // returns a Locked flag for these; treat them as "skip" even in overwrite mode.
    private static bool IsSlotEligibleForOverwrite(SaveFile sav, int box, int slot)
    {
        var flags = sav.GetBoxSlotFlags(box, slot);
        return !flags.HasFlag(StorageSlotSource.Locked);
    }

    // Match PokemonStorageComponent.GetStatus / LegalityReportTab.GetStatus so the bulk
    // summary's illegal count agrees with the box header and the Legality Report tab.
    // `la.Valid` is also false for Fishy results, which would over-count here.
    private static bool IsIllegal(LegalityAnalysis la) =>
        la.Results.Any(r => r.Judgement == PKHexSeverity.Invalid)
        || !MoveResult.AllValid(la.Info.Moves)
        || !MoveResult.AllValid(la.Info.Relearn);

    private readonly record struct PlacedSlot(bool IsParty, int Box, int Slot, int PartyIndex)
    {
        public static PlacedSlot ForParty(int index) => new(true, 0, 0, index);
        public static PlacedSlot ForBox(int box, int slot) => new(false, box, slot, 0);
    }

    private void CancelImport() => cts?.Cancel();

    private Severity GetResultSeverity() => result switch
    {
        null => Severity.Info,
        { Cancelled: true } => Severity.Info,
        { Skipped: 0, Illegal: 0, OverflowRemaining: 0 } => Severity.Success,
        _ => Severity.Warning
    };

    private string GetResultMessage()
    {
        if (result is null)
        {
            return string.Empty;
        }

        var prefix = result.Cancelled
            ? $"Import cancelled. Imported {result.Imported}"
            : $"Imported {result.Imported}";
        var parts = new List<string> { prefix };

        if (result.Skipped > 0)
        {
            parts.Add($"skipped {result.Skipped} (incompatible)");
        }

        if (result.Illegal > 0)
        {
            parts.Add($"illegal: {result.Illegal}");
        }

        if (result.OverflowToBank > 0)
        {
            parts.Add($"sent {result.OverflowToBank} to Bank");
        }

        if (result.OverflowRemaining > 0)
        {
            parts.Add($"{result.OverflowRemaining} didn't fit (slots full)");
        }

        return string.Join(", ", parts) + ".";
    }

    private void Close() => MudDialog?.Close(result is null
        ? DialogResult.Cancel()
        : DialogResult.Ok(result));

    private sealed record BulkImportResult(
        int Imported,
        int Skipped,
        int Illegal,
        int OverflowRemaining,
        int OverflowToBank)
    {
        public bool Cancelled { get; init; }
    }
}
