namespace Pkmds.Rcl.Components.MainTabPages;

public partial class TradeTab : RefreshAwareComponent
{
    private bool isLoading;

    // Preserve the slot-B ZIP context so Export Slot B can replace the inner save
    // without changing the wrapper expected by the originating emulator.
    private SaveArchiveContext? saveArchiveContextB;

    [Inject]
    private IBackupService BackupService { get; set; } = null!;

    [Inject]
    private ISettingsService SettingsService { get; set; } = null!;

    [Inject]
    private IDragDropService DragDropService { get; set; } = null!;

    [Inject]
    private ILogger<TradeTab> Logger { get; set; } = null!;

    private string SlotATitle =>
        $"Slot A — {AppState.SaveFileName ?? "Loaded save"}";

    private string SlotBTitle =>
        $"Slot B — {AppState.SaveFileNameB ?? "Loaded save"}";

    private async Task LoadSecondarySaveAsync()
    {
        // Guard the replace path: if slot B has uncommitted transfers, give the user
        // a chance to back out before the picker opens and we tear down the save.
        if (AppState.SaveFileB is not null && AppState.HasUnsavedChangesB)
        {
            var proceed = await DialogService.ShowMessageBoxAsync(
                "Unsaved changes in Slot B",
                "Slot B has transfers that haven't been exported yet. Replacing it will discard those changes.",
                yesText: "Discard and replace",
                cancelText: "Cancel");
            if (proceed != true)
            {
                return;
            }
        }

        const string message = "Choose a second save file";
        var dialogParameters = new DialogParameters
        {
            { nameof(FileUploadDialog.Message), message }
        };

        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small, backdropClick: false);
        var dialog = await DialogService.ShowAsync<FileUploadDialog>(
            "Load Second Save File",
            dialogParameters,
            options);

        var result = await dialog.Result;
        if (result is not { Data: IBrowserFile selectedFile })
        {
            return;
        }

        await LoadSecondarySaveFile(selectedFile);
    }

    private async Task LoadSecondarySaveFile(IBrowserFile selectedFile)
    {
        if (selectedFile.Size == 0)
        {
            await DialogService.ShowMessageBoxAsync("Error", "The selected file is empty.");
            return;
        }

        var fileExtension = Path.GetExtension(selectedFile.Name);
        if (fileExtension.Equals(".state", StringComparison.OrdinalIgnoreCase) ||
            fileExtension.Equals(".savestate", StringComparison.OrdinalIgnoreCase))
        {
            await DialogService.ShowMessageBoxAsync("Wrong file type",
                "This looks like an emulator save state, not a save file. " +
                "Please export the actual save file from your emulator instead.");
            return;
        }

        isLoading = true;
        StateHasChanged();

        SaveFile? loadedSave = null;
        SaveArchiveContext? archiveContext = null;

        try
        {
            await using var fileStream = selectedFile.OpenReadStream(Constants.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();

            if (SaveFileLoader.TryLoad(data, selectedFile.Name, out var saveFile, out archiveContext))
            {
                if (!saveFile.IsSupportedForEditing(out var unsupportedReason))
                {
                    await DialogService.ShowMessageBoxAsync("Unsupported save file", unsupportedReason);
                    return;
                }

                loadedSave = saveFile;
            }
            else
            {
                await DialogService.ShowMessageBoxAsync("Error",
                    "The selected save file is invalid. If this save file came from a ROM hack, it is not supported.");
                return;
            }

            // NOTE: Do NOT call ParseSettings.InitFromSaveFileData(loadedSave) here.
            // That mutates global ParseSettings.ActiveTrainer and would break legality
            // analysis on slot A. Slot B legality is read using the global settings
            // already tuned to slot A's trainer; some handler-state checks may be
            // slightly off for slot B Pokémon but the post-transfer confirmation dialog
            // picks up the important cases.

            AppState.SaveFileB = loadedSave;
            AppState.SaveFileNameB = selectedFile.Name;
            AppState.BoxEditB?.LoadBox(loadedSave.CurrentBox);
            AppState.SelectedBoxNumberB = loadedSave.CurrentBox;
            AppState.SelectedBoxSlotNumberB = null;
            AppState.SelectedPartySlotNumberB = null;
            saveArchiveContextB = archiveContext;

            // Per-slot on-load backup. Identical contract to the slot-A load path:
            // bytes come from the freshly loaded SaveFile (handles Manic EMU rebuild),
            // metadata is keyed by filename + save metadata + source so slot A and slot B
            // backups coexist in IndexedDB.
            if (SettingsService.Settings.IsAutoBackupEnabled)
            {
                try
                {
                    loadedSave.PrepareForExport();
                    var rawSave = loadedSave.Write().ToArray();
                    var backupBytes = archiveContext is not null
                        ? SaveArchiveHelper.RebuildZip(archiveContext, rawSave)
                        : rawSave;
                    await BackupService.CreateBackupAsync(
                        backupBytes, loadedSave, selectedFile.Name,
                        isManicEmu: archiveContext?.IsManicEmu == true, source: "auto");
                    await BackupService.EnforceRetentionAsync(SettingsService.Settings.MaxBackupCount);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Auto-backup failed for slot-B {FileName}", selectedFile.Name);
                }
            }

            Snackbar.Add($"Loaded second save: {selectedFile.Name}", Severity.Success);
            RefreshService.Refresh();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading slot-B save file: {FileName}", selectedFile.Name);
            await DialogService.ShowMessageBoxAsync("Error", $"Failed to load save: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task UnloadSecondarySaveAsync()
    {
        if (AppState.HasUnsavedChangesB)
        {
            var proceed = await DialogService.ShowMessageBoxAsync(
                "Unsaved changes in Slot B",
                "Slot B has transfers that haven't been exported yet. Closing it will discard those changes.",
                yesText: "Discard and close",
                cancelText: "Cancel");
            if (proceed != true)
            {
                return;
            }
        }

        AppState.SaveFileB = null;
        AppState.SaveFileNameB = null;
        saveArchiveContextB = null;
        RefreshService.Refresh();
    }

    private async Task ExportSecondarySaveAsync()
    {
        if (AppState.SaveFileB is not { } saveB)
        {
            return;
        }

        Logger.LogInformation("Exporting slot-B save file");
        saveB.PrepareForExport();
        var rawSaveBytes = saveB.Write().ToArray();
        var originalName = AppState.SaveFileNameB;

        bool wrote;
        // Same branching as MainLayout.ExportSaveFile — rebuild any captured ZIP wrapper.
        if (saveArchiveContextB is { } archiveContext)
        {
            var zipBytes = SaveArchiveHelper.RebuildZip(archiveContext, rawSaveBytes);
            if (archiveContext.IsManicEmu)
            {
                var (exportName, compoundExt) = ManicEmuSaveHelper.GetExportFileName(originalName);
                wrote = await WriteSlotBFileAsync(zipBytes, exportName, compoundExt);
            }
            else
            {
                var exportName = string.IsNullOrWhiteSpace(originalName) ? "save.zip" : originalName;
                wrote = await WriteSlotBFileAsync(zipBytes, exportName, ".zip");
            }
        }
        else if (string.IsNullOrWhiteSpace(originalName))
        {
            wrote = await WriteSlotBFileAsync(rawSaveBytes, "save.sav", ".sav");
        }
        else
        {
            var ext = Path.GetExtension(originalName);
            wrote = await WriteSlotBFileAsync(rawSaveBytes, originalName, ext);
        }

        if (wrote)
        {
            AppState.HasUnsavedChangesB = false;
        }
    }

    // Minimal duplicate of MainLayout's WriteFile: File System Access API when supported,
    // fall back to an anchor click for legacy browsers. Returns whether the file was
    // actually written (false on user cancel or failure) so the caller knows whether
    // to clear the dirty flag.
    private async Task<bool> WriteSlotBFileAsync(byte[] data, string fileName, string fileTypeExtension)
    {
        if (!await FileSystemAccessService.IsSupportedAsync())
        {
            var finalName = string.IsNullOrWhiteSpace(fileName) ? "save.sav" : fileName;
            var base64 = Convert.ToBase64String(data);
            var element = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "eval", "document.createElement('a')");
            await element.InvokeVoidAsync("setAttribute", "href",
                $"data:application/x-pokemon-savedata;base64,{base64}");
            await element.InvokeVoidAsync("setAttribute", "download", finalName);
            await element.InvokeVoidAsync("click");
            return true;
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("showFilePickerAndWrite",
                fileName, data, fileTypeExtension, "Save File");
            return true;
        }
        catch (JSException ex) when (ex.Message.Contains("AbortError", StringComparison.OrdinalIgnoreCase)
                                     || ex.Message.Contains("aborted a request", StringComparison.OrdinalIgnoreCase))
        {
            // User dismissed the picker — not an error.
            return false;
        }
        catch (JSException ex)
        {
            Logger.LogError(ex, "Error exporting slot-B save file: {FileName}", fileName);
            Snackbar.Add("Export failed. Please try again or use a different browser.", Severity.Error);
            return false;
        }
    }

    // ── Transfer logic ────────────────────────────────────────────────────────

    private bool CanTransferFromA()
    {
        if (AppState.SaveFile is not { } srcSave || AppState.SaveFileB is not { } destSave)
            return false;
        if (!TryGetSelectedSource(fromA: true, out _, out var isParty, out var boxNum, out var slotNum))
            return false;
        var pkm = isParty
            ? srcSave.TryGetPartySlot(slotNum)
            : boxNum.HasValue ? srcSave.GetBoxSlotAtIndex(boxNum.Value, slotNum) : null;
        return IsCrossTransferEligible(pkm, srcSave, destSave, AppState.IsHaXEnabled);
    }

    private bool CanTransferFromB()
    {
        if (AppState.SaveFileB is not { } srcSave || AppState.SaveFile is not { } destSave)
            return false;
        if (!TryGetSelectedSource(fromA: false, out _, out var isParty, out var boxNum, out var slotNum))
            return false;
        var pkm = isParty
            ? srcSave.TryGetPartySlot(slotNum)
            : boxNum.HasValue ? srcSave.GetBoxSlotAtIndex(boxNum.Value, slotNum) : null;
        return IsCrossTransferEligible(pkm, srcSave, destSave, AppState.IsHaXEnabled);
    }

    // Mirrors the eligibility rules in TradeSlot.ComputeTransferEligibility / HandleDrop so that
    // the arrow buttons enforce the same gate as drag-and-drop. HaX mode bypasses all checks.
    private static bool IsCrossTransferEligible(PKM? pkm, SaveFile srcSave, SaveFile destSave, bool haxEnabled)
    {
        if (pkm is not { Species: > 0 })
            return false;
        if (haxEnabled)
            return true;
        if (srcSave is SAV7b || destSave is SAV7b)
            return false;
        if (pkm.GetType() != destSave.PKMType
            && !EntityConverter.IsConvertibleToFormat(pkm, destSave.Generation))
            return false;
        return destSave.Personal.IsPresentInGame(pkm.Species, pkm.Form);
    }

    private bool TryGetSelectedSource(bool fromA, out SaveFile source, out bool isParty,
        out int? boxNumber, out int slotNumber)
    {
        source = null!;
        isParty = false;
        boxNumber = null;
        slotNumber = -1;

        var sav = fromA ? AppState.SaveFile : AppState.SaveFileB;
        if (sav is null)
        {
            return false;
        }

        var partySel = fromA ? AppState.SelectedPartySlotNumber : AppState.SelectedPartySlotNumberB;
        if (partySel is { } partySlot && partySlot < sav.PartyCount)
        {
            var pkm = sav.TryGetPartySlot(partySlot);
            if (pkm is { Species: > 0 })
            {
                source = sav;
                isParty = true;
                slotNumber = partySlot;
                return true;
            }
        }

        var boxSel = fromA ? AppState.SelectedBoxNumber : AppState.SelectedBoxNumberB;
        var boxSlotSel = fromA ? AppState.SelectedBoxSlotNumber : AppState.SelectedBoxSlotNumberB;
        if (boxSel is { } selBox && boxSlotSel is { } selSlot && sav.BoxCount > 0)
        {
            var pkm = sav.GetBoxSlotAtIndex(selBox, selSlot);
            if (pkm is { Species: > 0 })
            {
                source = sav;
                isParty = false;
                boxNumber = selBox;
                slotNumber = selSlot;
                return true;
            }
        }

        return false;
    }

    private async Task TransferSelectedAsync(bool fromA)
    {
        if (!TryGetSelectedSource(fromA, out var srcSave, out var srcIsParty,
                out var srcBox, out var srcSlot))
        {
            Snackbar.Add("Select a Pokémon in the source pane first.", Severity.Warning);
            return;
        }

        var destSave = fromA ? AppState.SaveFileB : AppState.SaveFile;
        if (destSave is null)
        {
            return;
        }

        // Belt-and-suspenders: same eligibility gate as CanTransferFrom* / TradeSlot.HandleDrop.
        if (!IsCrossTransferEligible(
                srcIsParty ? srcSave.TryGetPartySlot(srcSlot)
                           : srcBox.HasValue ? srcSave.GetBoxSlotAtIndex(srcBox.Value, srcSlot) : null,
                srcSave, destSave, AppState.IsHaXEnabled))
        {
            Snackbar.Add("This Pokémon can't be transferred to the destination save.", Severity.Warning);
            return;
        }

        // Prefer the user's selected dest slot when one is chosen; otherwise pick the
        // first empty slot (party first, then the currently-selected box, then any box).
        if (!TryGetSelectedDest(fromA: !fromA, destSave, out var destIsParty,
                out var destBox, out var destSlot)
            && !TryFindFirstEmptyDest(destSave, srcIsParty, fromA: !fromA,
                out destIsParty, out destBox, out destSlot))
        {
            Snackbar.Add("No empty slot in the destination save.", Severity.Warning);
            return;
        }

        await ExecuteTransferAsync(srcSave, srcIsParty, srcBox, srcSlot,
            destSave, destIsParty, destBox, destSlot);
    }

    private bool TryGetSelectedDest(bool fromA, SaveFile destSave, out bool isParty,
        out int? boxNumber, out int slotNumber)
    {
        isParty = false;
        boxNumber = null;
        slotNumber = -1;

        var partySel = fromA ? AppState.SelectedPartySlotNumber : AppState.SelectedPartySlotNumberB;
        if (partySel is { } partySlot && partySlot <= destSave.PartyCount && partySlot < 6)
        {
            isParty = true;
            slotNumber = partySlot;
            return true;
        }

        var boxSel = fromA ? AppState.SelectedBoxNumber : AppState.SelectedBoxNumberB;
        var boxSlotSel = fromA ? AppState.SelectedBoxSlotNumber : AppState.SelectedBoxSlotNumberB;
        if (boxSel is { } selBox && boxSlotSel is { } selSlot && destSave.BoxCount > 0)
        {
            boxNumber = selBox;
            slotNumber = selSlot;
            return true;
        }

        return false;
    }

    private static bool TryFindFirstEmptyDest(SaveFile destSave, bool srcIsParty, bool fromA,
        out bool isParty, out int? boxNumber, out int slotNumber)
    {
        _ = fromA;
        isParty = false;
        boxNumber = null;
        slotNumber = -1;

        // Prefer the party if the source came from a party slot and there's room.
        if (srcIsParty && destSave.PartyCount < 6)
        {
            isParty = true;
            slotNumber = destSave.PartyCount;
            return true;
        }

        // Scan boxes for the first empty slot.
        for (var box = 0; box < destSave.BoxCount; box++)
        {
            for (var slot = 0; slot < destSave.BoxSlotCount; slot++)
            {
                if (destSave.GetBoxSlotAtIndex(box, slot) is { Species: 0 })
                {
                    isParty = false;
                    boxNumber = box;
                    slotNumber = slot;
                    return true;
                }
            }
        }

        // Fall back to appending to the party if no box slot is available.
        if (destSave.PartyCount < 6)
        {
            isParty = true;
            slotNumber = destSave.PartyCount;
            return true;
        }

        return false;
    }

    private async Task HandleSlotDropAsync(TradeSlotTarget dest)
    {
        // Snapshot the drag source before ClearDrag runs — dragend fires concurrently.
        var srcPokemon = DragDropService.DraggedPokemon;
        var srcSave = DragDropService.DragSourceSaveFile ?? AppState.SaveFile;
        var srcBox = DragDropService.DragSourceBoxNumber;
        var srcSlot = DragDropService.DragSourceSlotNumber;
        var srcIsParty = DragDropService.IsDragSourceParty;
        DragDropService.ClearDrag();

        if (srcPokemon is null || srcSave is null || srcSlot < 0)
        {
            return;
        }

        await ExecuteTransferAsync(srcSave, srcIsParty, srcBox, srcSlot,
            dest.OwnerSaveFile, dest.IsParty, dest.BoxNumber, dest.SlotNumber);
    }

    private async Task ExecuteTransferAsync(
        SaveFile srcSave, bool srcIsParty, int? srcBox, int srcSlot,
        SaveFile destSave, bool destIsParty, int? destBox, int destSlot)
    {
        // Reject in-place moves outright.
        if (ReferenceEquals(srcSave, destSave)
            && srcIsParty == destIsParty
            && srcBox == destBox
            && srcSlot == destSlot)
        {
            return;
        }

        // Let's Go storage has its own set-semantics via the slot-index overload; keep
        // behavior parity with PokemonSlotComponent, which disables drag/drop for SAV7b.
        if (srcSave is SAV7b || destSave is SAV7b)
        {
            Snackbar.Add("Transfers involving Let's Go saves aren't supported yet.", Severity.Warning);
            return;
        }

        var srcPkm = srcIsParty
            ? srcSave.TryGetPartySlot(srcSlot)
            : srcBox.HasValue
                ? srcSave.GetBoxSlotAtIndex(srcBox.Value, srcSlot)
                : null;
        if (srcPkm is not { Species: > 0 })
        {
            return;
        }

        // Intra-save moves: delegate to the same move/swap logic used by the Party/Box tab,
        // but parameterised to the owning save file so slot B also works.
        if (ReferenceEquals(srcSave, destSave))
        {
            if (!MoveWithinSave(srcSave, srcIsParty, srcBox, srcSlot,
                    destIsParty, destBox, destSlot))
            {
                return;
            }

            MarkSlotBDirtyIfInvolved(srcSave, destSave);
            Haptics.Confirm();
            RefreshService.Refresh();
            return;
        }

        // Cross-save transfer.
        // Keep the destination party compact: if the user dropped past PartyCount on an
        // empty slot, clamp to PartyCount so SetPartySlotAtIndex appends rather than
        // leaving a gap (mirrors the intra-save clamp in MoveWithinSave).
        if (destIsParty && destSlot >= destSave.PartyCount)
        {
            destSlot = destSave.PartyCount;
        }

        var destPkmPrev = destIsParty
            ? destSave.TryGetPartySlot(destSlot)
            : destBox.HasValue
                ? destSave.GetBoxSlotAtIndex(destBox.Value, destSlot)
                : null;
        if (destPkmPrev is null)
        {
            return;
        }

        // Held-item handling: the mon leaves its save, so we strip its held item and return
        // it to that same save's bag. The item came from that game's item table in the first
        // place, so it normally fits — the failure cases are (1) the bag is full (no empty
        // slot and the existing stack is already at max), or (2) a HaX-edited nonsense item
        // ID no pouch recognizes. We pre-check returnability so we can warn the user up-front
        // and let them cancel to free up bag space, and defer the actual deposit to the final
        // commit so a cancellation at the legality step doesn't leave the bag out of sync
        // with the slot state.
        var srcHeldItem = (ushort)Math.Max(0, srcPkm.HeldItem);
        var destHeldItem = destPkmPrev.Species > 0
            ? (ushort)Math.Max(0, destPkmPrev.HeldItem)
            : (ushort)0;

        var itemLossLines = new List<string>(2);
        if (srcHeldItem > 0 && !CanReturnItemToBag(srcSave, srcHeldItem))
        {
            itemLossLines.Add(DescribeHeldItem(srcPkm, srcSave, srcHeldItem));
        }
        if (destHeldItem > 0 && !CanReturnItemToBag(destSave, destHeldItem))
        {
            itemLossLines.Add(DescribeHeldItem(destPkmPrev, destSave, destHeldItem));
        }

        if (itemLossLines.Count > 0)
        {
            var proceedLoss = await ConfirmHeldItemLossAsync(itemLossLines);
            if (!proceedLoss)
            {
                return;
            }
        }

        var haxEnabled = AppState.IsHaXEnabled;
        var srcClone = srcPkm.Clone();
        srcClone.HeldItem = 0;

        var converted = ConvertForSave(srcClone, destSave, haxEnabled, out var forwardMessage);
        if (converted is null)
        {
            ShowTransferIssueSnackbar("Could not transfer", forwardMessage,
                "Could not convert the source Pokémon to the destination's format.",
                Severity.Error);
            return;
        }
        destSave.AdaptToSaveFile(converted, destIsParty);

        // If the destination slot has a Pokémon, we swap by converting it back to the
        // source's format. Otherwise we clear the source slot after the write.
        PKM? convertedBack = null;
        if (destPkmPrev.Species > 0)
        {
            var destClone = destPkmPrev.Clone();
            destClone.HeldItem = 0;
            convertedBack = ConvertForSave(destClone, srcSave, haxEnabled, out var reverseMessage);
            if (convertedBack is null)
            {
                ShowTransferIssueSnackbar("Swap not possible", reverseMessage,
                    "Could not convert the destination Pokémon back to the source's format for a swap.",
                    Severity.Error);
                return;
            }
            srcSave.AdaptToSaveFile(convertedBack, srcIsParty);
        }

        // Apply writes before running legality — LegalityAnalysis re-runs below so we
        // catch any adapt-on-write surprises introduced by AdaptToSaveFile.
        WriteSlot(destSave, destIsParty, destBox, destSlot, converted);
        if (convertedBack is not null)
        {
            WriteSlot(srcSave, srcIsParty, srcBox, srcSlot, convertedBack);
        }
        else
        {
            ClearSlot(srcSave, srcIsParty, srcBox, srcSlot);
        }

        // Re-run legality on the destination-side result. In HaX mode we trust the user
        // and skip the confirmation; otherwise prompt before committing to the write.
        if (!AppState.IsHaXEnabled)
        {
            var proceed = await ConfirmLegalityAsync(converted, destSave);
            if (!proceed)
            {
                // Revert: restore the original destination and source PKMs.
                WriteSlot(destSave, destIsParty, destBox, destSlot, destPkmPrev);
                WriteSlot(srcSave, srcIsParty, srcBox, srcSlot, srcPkm);
                RefreshService.Refresh();
                return;
            }
        }

        // Transfer is committed — now deposit the held items we pre-checked. Items the user
        // agreed to lose (CanReturnItemToBag returned false) are simply dropped: HeldItem was
        // already cleared on the clones, so they don't ride along to the destination save.
        if (srcHeldItem > 0)
        {
            ReturnItemToBag(srcSave, srcHeldItem);
        }
        if (destHeldItem > 0)
        {
            ReturnItemToBag(destSave, destHeldItem);
        }

        if (!string.IsNullOrEmpty(forwardMessage))
        {
            ShowTransferIssueSnackbar("Warning", forwardMessage,
                "The transfer succeeded with warnings.", Severity.Warning);
        }

        MarkSlotBDirtyIfInvolved(srcSave, destSave);
        Haptics.Confirm();
        RefreshService.Refresh();
    }

    private void MarkSlotBDirtyIfInvolved(SaveFile srcSave, SaveFile destSave)
    {
        if (AppState.SaveFileB is { } slotB
            && (ReferenceEquals(srcSave, slotB) || ReferenceEquals(destSave, slotB)))
        {
            AppState.HasUnsavedChangesB = true;
        }
    }

    // Render conversion errors/warnings as a bulleted list when PKHeX returns a multi-line
    // message (e.g. "Cannot convert a PK7 to PK3\nCannot transfer this format..."). A plain
    // snackbar string collapses the newline and looks like one run-on sentence, which is what
    // prompted this. Falls back to a single-line snackbar when there's only one line.
    private static readonly string[] MessageLineSeparators = ["\r\n", "\n", "\r"];

    private void ShowTransferIssueSnackbar(string heading, string? details, string fallback, Severity severity)
    {
        var lines = string.IsNullOrWhiteSpace(details)
            ? [fallback]
            : details.Split(MessageLineSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length <= 1)
        {
            Snackbar.Add($"{heading}: {lines[0]}", severity);
            return;
        }

        var bullets = new StringBuilder();
        bullets.Append("<strong>").Append(WebUtility.HtmlEncode(heading)).Append(":</strong>");
        bullets.Append("<ul style=\"margin:4px 0 0 0;padding-left:1.25rem;\">");
        foreach (var line in lines)
        {
            bullets.Append("<li>").Append(WebUtility.HtmlEncode(line)).Append("</li>");
        }
        bullets.Append("</ul>");

        Snackbar.Add(new MarkupString(bullets.ToString()), severity);
    }

    // ── Held-item handoff ─────────────────────────────────────────────────────

    // Pre-flight check: is there room in any compatible pouch of `save` to receive one more
    // copy of `itemId`? Matches InventoryPouch.GiveItem's placement rules (existing slot with
    // room, or first empty slot in a pouch that accepts the item). No side effects.
    private static bool CanReturnItemToBag(SaveFile save, ushort itemId)
    {
        if (itemId == 0)
        {
            return true;
        }

        var bag = save.Inventory;
        foreach (var pouch in bag.Pouches)
        {
            if (!pouch.CanContain(itemId))
            {
                continue;
            }

            var max = bag.GetMaxCount(pouch.Type, itemId);
            if (max <= 0)
            {
                return false;
            }

            foreach (var item in pouch.Items)
            {
                if (item.Index == itemId && item.Count < max)
                {
                    return true;
                }
                if (item.Index == 0)
                {
                    return true;
                }
            }

            // Item is allowed in this pouch but every eligible slot is taken or maxed out.
            return false;
        }

        // No pouch in the bag accepts this item id (unsupported save or cross-gen item).
        return false;
    }

    // Deposits one unit of `itemId` back into `save`'s bag and persists it. Assumed to be
    // called only after CanReturnItemToBag returned true for the same (save, itemId) pair.
    private static void ReturnItemToBag(SaveFile save, ushort itemId)
    {
        if (itemId == 0)
        {
            return;
        }

        var bag = save.Inventory;
        foreach (var pouch in bag.Pouches)
        {
            if (!pouch.CanContain(itemId))
            {
                continue;
            }

            pouch.GiveItem(bag, itemId, 1);
            bag.CopyTo(save);
            return;
        }
    }

    private string DescribeHeldItem(PKM pkm, SaveFile save, ushort itemId)
    {
        var strings = GameInfo.GetStrings(GameInfo.CurrentLanguage);
        var speciesName = pkm.Species < strings.specieslist.Length
            ? strings.specieslist[pkm.Species]
            : $"Species #{pkm.Species}";
        // Held-item IDs are context-specific (e.g. Gen 3 ID 199 = Metal Coat,
        // but modern ID 199 = Babiri Berry). Look up using the Pokémon's own context.
        var items = strings.GetItemStrings(pkm.Context, save.Version);
        var itemName = itemId < items.Length
            ? items[itemId]
            : $"Item #{itemId}";
        var bagLabel = ReferenceEquals(save, AppState.SaveFile) ? "Slot A" : "Slot B";
        return $"{speciesName}’s {itemName} (from {bagLabel}’s bag)";
    }

    private async Task<bool> ConfirmHeldItemLossAsync(IReadOnlyList<string> lines)
    {
        var bullets = new StringBuilder();
        bullets.Append("<p>These held items can’t be returned to their save’s bag and will be lost if you continue:</p>");
        bullets.Append("<ul style=\"margin:8px 0 0 0;padding-left:1.25rem;\">");
        foreach (var line in lines)
        {
            bullets.Append("<li>").Append(WebUtility.HtmlEncode(line)).Append("</li>");
        }
        bullets.Append("</ul>");
        bullets.Append("<p>Cancel and make room in the bag to keep the item, or continue to transfer anyway.</p>");

        var result = await DialogService.ShowMessageBoxAsync(
            "Held items will be lost",
            new MarkupString(bullets.ToString()),
            yesText: "Continue",
            cancelText: "Cancel");
        return result == true;
    }

    private static PKM? ConvertForSave(PKM pkm, SaveFile destSave, bool haxEnabled, out string message)
    {
        message = string.Empty;
        var destType = destSave.PKMType;
        if (pkm.GetType() == destType)
        {
            return pkm;
        }

        var converted = EntityConverter.ConvertToType(pkm, destType, out var result);
        if (converted is not null && result.IsSuccess)
        {
            return converted;
        }

        // HaX fallback mirrors PokemonSlotComponent / PokemonBankTab — retry with
        // AllowIncompatibleAll so DLC-gated species (e.g. Bibarel into pre-Teal-Mask
        // Violet) can still transfer. The original message becomes a warning.
        if (haxEnabled)
        {
            var previous = EntityConverter.AllowIncompatibleConversion;
            EntityConverter.AllowIncompatibleConversion = EntityCompatibilitySetting.AllowIncompatibleAll;
            try
            {
                converted = EntityConverter.ConvertToType(pkm, destType, out result);
            }
            finally
            {
                EntityConverter.AllowIncompatibleConversion = previous;
            }

            if (converted is not null && result.IsSuccess)
            {
                message = result.GetDisplayString(pkm, destType);
                return converted;
            }
        }

        message = result.GetDisplayString(pkm, destType);
        return null;
    }

    private static void WriteSlot(SaveFile save, bool isParty, int? boxNumber, int slotNumber, PKM pkm)
    {
        if (isParty)
        {
            save.SetPartySlotAtIndex(pkm, slotNumber);
            save.CompactParty();
        }
        else if (boxNumber.HasValue)
        {
            save.SetBoxSlotAtIndex(pkm, boxNumber.Value, slotNumber);
            save.CompactBoxIfGen12(boxNumber.Value);
        }
    }

    private static void ClearSlot(SaveFile save, bool isParty, int? boxNumber, int slotNumber)
    {
        if (isParty)
        {
            // DeletePartySlot keeps the party compact, matching AppService.MovePokemon.
            save.DeletePartySlot(slotNumber);
        }
        else if (boxNumber.HasValue)
        {
            save.SetBoxSlotAtIndex(save.BlankPKM, boxNumber.Value, slotNumber);
            save.CompactBoxIfGen12(boxNumber.Value);
        }
    }

    private async Task<bool> ConfirmLegalityAsync(PKM pkm, SaveFile destSave)
    {
        // Adapt-aware analysis: the converted PKM has already had UpdateHandler run
        // against destSave (via AdaptToSaveFile), so its HT fields match destSave's
        // trainer. HistoryVerifier.VerifyHandlerState, however, checks those HT fields
        // against the *global* ParseSettings.ActiveTrainer — which is pinned to Save A
        // throughout the app's lifetime so slot-A legality stays correct. When the
        // destination is Save B, the global still points at Save A and the handler
        // check fires false-positive "Handling trainer does not match" errors.
        // Repoint ActiveTrainer at destSave for the duration of this analysis, then
        // restore it so the rest of the app keeps seeing Save A as the active trainer.
        LegalityAnalysis la;
        if (AppState.SaveFile is { } savA && !ReferenceEquals(savA, destSave))
        {
            ParseSettings.InitFromSaveFileData(destSave);
            try
            {
                la = new LegalityAnalysis(pkm);
            }
            finally
            {
                ParseSettings.InitFromSaveFileData(savA);
            }
        }
        else
        {
            la = new LegalityAnalysis(pkm);
        }
        var invalid = la.Results.Any(r => r.Judgement == PKHeX.Core.Severity.Invalid)
                      || !MoveResult.AllValid(la.Info.Moves)
                      || !MoveResult.AllValid(la.Info.Relearn);
        var fishy = !invalid && la.Results.Any(r => r.Judgement == PKHeX.Core.Severity.Fishy);
        if (!invalid && !fishy)
        {
            return true;
        }

        // LegalityLocalizationContext is a ref struct, so we can't use LINQ over it —
        // materialise the humanized strings via a plain loop.
        var ctx = LegalityLocalizationContext.Create(la);
        var messages = new List<string>(3);
        foreach (var r in la.Results)
        {
            if (r.Judgement is not (PKHeX.Core.Severity.Invalid or PKHeX.Core.Severity.Fishy))
            {
                continue;
            }
            var humanized = ctx.Humanize(in r, verbose: false);
            if (!string.IsNullOrWhiteSpace(humanized))
            {
                messages.Add(humanized);
            }
            if (messages.Count >= 3)
            {
                break;
            }
        }

        var severity = invalid ? "illegal" : "fishy";
        // MudBlazor's message box collapses whitespace in plain strings, so a bulleted list
        // has to be rendered as HTML via the MarkupString overload (same pattern as
        // ConfirmHeldItemLossAsync) — otherwise the bullets all wrap onto one line.
        var body = new StringBuilder();
        if (messages.Count > 0)
        {
            body.Append("<p>The converted Pokémon is ").Append(severity).Append(":</p>");
            body.Append("<ul style=\"margin:8px 0 0 0;padding-left:1.25rem;\">");
            foreach (var msg in messages)
            {
                body.Append("<li>").Append(WebUtility.HtmlEncode(msg)).Append("</li>");
            }
            body.Append("</ul>");
            body.Append("<p>Proceed with the transfer?</p>");
        }
        else
        {
            body.Append("<p>The converted Pokémon is ").Append(severity).Append(". Proceed with the transfer?</p>");
        }
        var result = await DialogService.ShowMessageBoxAsync(
            invalid ? "Illegal after conversion" : "Fishy after conversion",
            new MarkupString(body.ToString()),
            yesText: "Transfer anyway",
            cancelText: "Cancel");
        return result == true;
    }

    // ── Intra-save move (both slot A→A and slot B→B) ──────────────────────────

    // Deliberately duplicates the branching in AppService.MovePokemon so slot B has the
    // same move/swap + Gen 1/2 compacting semantics without taking a dependency on
    // AppState.SaveFile. Returns false when the move is a no-op (e.g. trying to move
    // the last battle-ready party member out).
    private static bool MoveWithinSave(SaveFile save,
        bool srcIsParty, int? srcBox, int srcSlot,
        bool destIsParty, int? destBox, int destSlot)
    {
        var source = srcIsParty
            ? save.TryGetPartySlot(srcSlot)
            : srcBox.HasValue ? save.GetBoxSlotAtIndex(srcBox.Value, srcSlot) : null;
        if (source is null)
        {
            return false;
        }

        var dest = destIsParty
            ? save.TryGetPartySlot(destSlot)
            : destBox.HasValue ? save.GetBoxSlotAtIndex(destBox.Value, destSlot) : null;
        if (dest is null)
        {
            return false;
        }

        var isSwap = source.Species > 0 && dest.Species > 0;

        // Party safety: don't let the last non-Egg party member get moved out.
        if (srcIsParty && !destIsParty && !isSwap)
        {
            var battleReady = 0;
            for (var i = 0; i < save.PartyCount; i++)
            {
                if (i == srcSlot)
                {
                    continue;
                }
                var partyMon = save.TryGetPartySlot(i);
                if (partyMon is { Species: > 0, IsEgg: false })
                {
                    battleReady++;
                }
            }
            if (battleReady == 0)
            {
                return false;
            }
        }

        if (isSwap)
        {
            WriteSlot(save, srcIsParty, srcBox, srcSlot, dest);
            WriteSlot(save, destIsParty, destBox, destSlot, source);
            return true;
        }

        if (srcIsParty && !destIsParty)
        {
            if (destBox.HasValue)
            {
                save.SetBoxSlotAtIndex(source, destBox.Value, destSlot);
                save.CompactBoxIfGen12(destBox.Value);
            }
            save.DeletePartySlot(srcSlot);
            return true;
        }

        if (!srcIsParty && destIsParty)
        {
            if (srcBox.HasValue)
            {
                save.SetBoxSlotAtIndex(save.BlankPKM, srcBox.Value, srcSlot);
                save.CompactBoxIfGen12(srcBox.Value);
            }
            var target = destSlot >= save.PartyCount ? save.PartyCount : destSlot;
            save.SetPartySlotAtIndex(source, target);
            save.CompactParty();
            return true;
        }

        WriteSlot(save, destIsParty, destBox, destSlot, source);
        ClearSlot(save, srcIsParty, srcBox, srcSlot);
        return true;
    }
}
