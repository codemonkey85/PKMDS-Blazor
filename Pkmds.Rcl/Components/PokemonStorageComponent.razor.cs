namespace Pkmds.Rcl.Components;

public partial class PokemonStorageComponent : RefreshAwareComponent
{
    private int cachedBoxNumber = -1;

    private int illegalCountInBox;

    private bool isLegalizingBox;
    private CancellationTokenSource? legalizeBoxCts;
    private double legalizeBoxPercent;
    private string legalizeBoxStatusText = string.Empty;

    [Inject]
    private ILegalizationService LegalizationService { get; set; } = null!;

    protected override RefreshEvents SubscribeTo =>
        RefreshEvents.AppState | RefreshEvents.BoxState;

    private int GetIllegalCountInBox()
    {
        if (AppState.SaveFile is not { } saveFile || AppState.BoxEdit is not { } boxEdit)
        {
            illegalCountInBox = 0;
            cachedBoxNumber = -1;
            return 0;
        }

        var box = boxEdit.CurrentBox;
        if (box == cachedBoxNumber && !isLegalizingBox)
        {
            return illegalCountInBox;
        }

        var count = 0;
        for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
        {
            var pkm = saveFile.GetBoxSlotAtIndex(box, slot);
            if (pkm is not { Species: > 0 })
            {
                continue;
            }

            var la = AppService.GetLegalityAnalysis(pkm);
            if (LegalityUi.GetStatus(la) == LegalityStatus.Illegal)
            {
                count++;
            }
        }

        illegalCountInBox = count;
        cachedBoxNumber = box;
        return count;
    }

    private void InvalidateIllegalCount() => cachedBoxNumber = -1;

    private void GoToNextBox()
    {
        if (AppState.SaveFile is null || AppState.BoxEdit is null)
        {
            return;
        }

        AppState.BoxEdit.MoveRight();
        AppState.SaveFile.CurrentBox = AppState.BoxEdit.CurrentBox;

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.RefreshBoxState();
    }

    private void GoToPreviousBox()
    {
        if (AppState.SaveFile is null || AppState.BoxEdit is null)
        {
            return;
        }

        AppState.BoxEdit.MoveLeft();
        AppState.SaveFile.CurrentBox = AppState.BoxEdit.CurrentBox;

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.RefreshBoxState();
    }

    private void OnBoxChanged(int newBox)
    {
        if (AppState.SaveFile is null || AppState.BoxEdit is null)
        {
            return;
        }

        AppState.BoxEdit.LoadBox(newBox);
        AppState.SaveFile.CurrentBox = AppState.BoxEdit.CurrentBox;

        AppState.SelectedBoxNumber = null;
        AppState.SelectedBoxSlotNumber = null;

        RefreshService.RefreshBoxState();
    }

    private int GetBoxPokemonCount(int boxId)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return 0;
        }

        var count = 0;
        for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
        {
            if (saveFile.GetBoxSlotAtIndex(boxId, slot).Species != 0)
            {
                count++;
            }
        }

        return count;
    }

    private async Task OpenBoxLayoutDialog()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Medium);
        await DialogService.ShowAsync<BoxLayoutDialog>("Box Layout", options);
        RefreshService.RefreshBoxState();
    }

    private bool HasAnyExportablePokemon()
    {
        if (AppState.SaveFile is not { } sav)
        {
            return false;
        }

        // Don't trust the reported party count: ROM hacks (e.g. SAV3-based Pokémon Unbound) write
        // garbage into the single-byte party-count field, which drives GetPartySlotAtIndex past the
        // party buffer and throws (issue #1003). GetSafePartyCount clamps to the 6-slot maximum.
        var partyCount = sav.GetSafePartyCount();
        for (var i = 0; i < partyCount; i++)
        {
            if (sav.TryGetPartySlot(i) is { Species: > 0 })
            {
                return true;
            }
        }

        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                if (sav.GetBoxSlotAtIndex(box, slot).Species != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async Task OpenBulkExportDialog()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<BulkExportDialog>("Export Pokémon as .pk* Files", options);
    }

    private async Task OpenBulkImportDialog()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        var dialog = await DialogService.ShowAsync<BulkImportDialog>("Bulk Import .pk* Files", options);

        await dialog.Result;
        InvalidateIllegalCount();
        RefreshService.RefreshBoxAndPartyState();
    }

    private async Task OpenBoxListDialog()
    {
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.ExtraExtraLarge);
        await DialogService.ShowAsync<BoxListDialog>("All Boxes", options);
    }

    private async Task OpenAddToBankDialogAsync()
    {
        if (AppService.EditFormPokemon is { Species: > 0 } editedPkm && AppState.SaveFile is { } sav
            && HasUnsavedEditFormChanges(editedPkm, sav))
        {
            var save = await DialogService.ShowMessageBoxAsync(
                "Unsaved Changes",
                "The open Pokémon has unsaved changes. Save before opening the Bank dialog?",
                yesText: "Save & Continue",
                cancelText: "Cancel");

            if (save is not true)
            {
                return;
            }

            AppService.SavePokemon(editedPkm);
        }

        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        await DialogService.ShowAsync<AddToBankDialog>("Add to Bank", options);
    }

    private bool HasUnsavedEditFormChanges(PKM editedPkm, SaveFile saveFile)
    {
        var selectedPokemonType =
            AppService.GetSelectedPokemonSlot(out var partySlot, out var boxNumber, out var boxSlot);

        PKM? slotPokemon = selectedPokemonType switch
        {
            SelectedPokemonType.Party => saveFile.GetPartySlotAtIndex(partySlot),
            SelectedPokemonType.Box => saveFile.GetBoxSlotAtIndex(boxNumber, boxSlot),
            SelectedPokemonType.None when saveFile is SAV7b && AppState.SelectedBoxSlotNumber is { } lgSlot =>
                saveFile.GetBoxSlotAtIndex(lgSlot / saveFile.BoxSlotCount, lgSlot % saveFile.BoxSlotCount),
            _ => null
        };

        if (slotPokemon is null || editedPkm.SIZE_STORED != slotPokemon.SIZE_STORED)
        {
            return false;
        }

        var editedBytes = new byte[editedPkm.SIZE_STORED];
        editedPkm.WriteDecryptedDataStored(editedBytes);

        var slotBytes = new byte[slotPokemon.SIZE_STORED];
        slotPokemon.WriteDecryptedDataStored(slotBytes);

        return !editedBytes.AsSpan().SequenceEqual(slotBytes);
    }

    private async Task LegalizeBoxAsync()
    {
        if (AppState.SaveFile is not { } saveFile || AppState.BoxEdit is not { } boxEdit)
        {
            return;
        }

        var box = boxEdit.CurrentBox;

        // Collect illegal or fishy slots in this box — skip empties and already-Legal ones.
        var targets = new List<(int Slot, PKM Pokemon)>();
        for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
        {
            var pkm = saveFile.GetBoxSlotAtIndex(box, slot);
            if (pkm is not { Species: > 0 })
            {
                continue;
            }

            var la = AppService.GetLegalityAnalysis(pkm);
            // Only target Illegal entries. Fishy is Valid per PKHeX — users expect the
            // box-level action to leave those alone. (The Legality Report tab still
            // lets users opt in to running Fishy through the engine.)
            if (LegalityUi.GetStatus(la) != LegalityStatus.Illegal)
            {
                continue;
            }

            targets.Add((slot, pkm));
        }

        illegalCountInBox = targets.Count;
        cachedBoxNumber = box;

        if (targets.Count == 0)
        {
            Snackbar.Add("No illegal Pokémon in this box.", Severity.Info);
            return;
        }

        legalizeBoxCts?.Dispose();
        legalizeBoxCts = new CancellationTokenSource();
        var ct = legalizeBoxCts.Token;

        isLegalizingBox = true;
        legalizeBoxPercent = 0;
        legalizeBoxStatusText = $"Legalizing 0/{targets.Count}…";
        StateHasChanged();

        await Task.Yield();

        var successCount = 0;
        var fishyCount = 0;
        var failureCount = 0;
        var processed = 0;
        var cancelled = false;

        for (var i = 0; i < targets.Count; i++)
        {
            if (ct.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            var (slot, pkm) = targets[i];
            var speciesName = AppService.GetPokemonSpeciesName(pkm.Species) ??
                              pkm.Species.ToString(CultureInfo.InvariantCulture);
            legalizeBoxStatusText =
                $"Legalizing {i + 1}/{targets.Count}: {speciesName} (Slot {slot + 1})";
            legalizeBoxPercent = (double)i / targets.Count * 100;
            StateHasChanged();

            // Task.Delay(1), not Task.Yield(): WASM single-threaded; Yield stays on the
            // same JS macrotask so the browser can't paint or process the Cancel click.
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
                // Tighter per-Pokémon timeout than the default 15s so a single slow
                // encounter search can't stall the sweep.
                result = await LegalizationService.LegalizeAsync(
                    pkm,
                    saveFile,
                    progress: null,
                    ct,
                    timeoutSeconds: 3);
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

            // EntityImportSettings.None skips UpdatePKM's "adapt as if traded in" path,
            // which can re-break handler/memory/trainer fields on a PKM we just made legal.
            saveFile.SetBoxSlotAtIndex(result.Pokemon, box, slot, EntityImportSettings.None);

            // Re-analyse the stored bytes so the counters match what the save actually holds.
            var storedPk = saveFile.GetBoxSlotAtIndex(box, slot);
            var storedLa = AppService.GetLegalityAnalysis(storedPk);
            switch (LegalityUi.GetStatus(storedLa))
            {
                case LegalityStatus.Legal:
                    successCount++;
                    break;
                case LegalityStatus.Fishy:
                    fishyCount++;
                    break;
                default:
                    failureCount++;
                    break;
            }
        }

        legalizeBoxPercent = 100;
        StateHasChanged();

        var summary = BuildLegalizeSummary(targets.Count, processed, successCount, fishyCount, failureCount, cancelled);
        Snackbar.Add(summary.Message, summary.Severity);

        isLegalizingBox = false;
        legalizeBoxStatusText = string.Empty;
        legalizeBoxPercent = 0;
        legalizeBoxCts.Dispose();
        legalizeBoxCts = null;

        InvalidateIllegalCount();
        RefreshService.RefreshBoxState();
    }

    private void CancelLegalizeBox() => legalizeBoxCts?.Cancel();

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
            legalizeBoxCts?.Cancel();
            legalizeBoxCts?.Dispose();
            legalizeBoxCts = null;
        }

        base.Dispose(disposing);
    }

    private void TogglePinCurrentBox()
    {
        if (AppState.BoxEdit is not { CurrentBox: var currentBox })
        {
            return;
        }

        if (AppState.PinnedBoxNumber == currentBox)
        {
            AppService.UnpinBox();
        }
        else
        {
            AppService.PinBox(currentBox);
        }
    }
}
