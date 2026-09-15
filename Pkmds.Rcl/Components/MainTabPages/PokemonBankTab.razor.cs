using System.IO.Compression;

namespace Pkmds.Rcl.Components.MainTabPages;

public partial class PokemonBankTab : RefreshAwareComponent
{
    private const string BackupReminderDismissedKey = "pkmds.bank.backup-reminder-dismissed";

    private static readonly string ImportAcceptList = BuildImportAcceptList();

    private static readonly int[] PageSizes = [20, 50, 100];
    private readonly HashSet<long> selectedIds = [];
    private int currentPage = 1;

    private List<BankEntry> entries = [];
    private List<BankEntry> filteredEntries = [];

    private IBrowserFile? importFile;
    private bool isBusy;
    private bool isLoading = true;
    private int pageSize = 20;
    private List<BankEntry> paginatedEntries = [];

    private string searchText = string.Empty;
    private bool shinyOnly;
    private bool showBackupReminder;

    private int TotalPages => Math.Max(1, (int)Math.Ceiling((double)filteredEntries.Count / pageSize));

    [Inject]
    private IBankService BankService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var dismissed = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", BackupReminderDismissedKey);
        showBackupReminder = dismissed is null;

        await ReloadAsync();
    }

    private async Task DismissBackupReminderAsync()
    {
        showBackupReminder = false;
        await JSRuntime.InvokeVoidAsync("localStorage.setItem", BackupReminderDismissedKey, "1");
    }

    private async Task ReloadAsync()
    {
        isLoading = true;
        StateHasChanged();

        entries = [.. await BankService.GetAllAsync()];

        isLoading = false;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<BankEntry> filtered = entries;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(e =>
                e.SpeciesName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                e.Pokemon.Nickname.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (shinyOnly)
        {
            filtered = filtered.Where(e => e.Pokemon.IsShiny);
        }

        filteredEntries = [.. filtered];
        currentPage = 1;
        UpdatePagination();
    }

    private void UpdatePagination() =>
        paginatedEntries =
        [
            .. filteredEntries
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
        ];

    private void OnPageSizeChanged()
    {
        currentPage = 1;
        UpdatePagination();
    }

    private void ToggleSelect(long id, bool selected)
    {
        if (selected)
        {
            selectedIds.Add(id);
        }
        else
        {
            selectedIds.Remove(id);
        }
    }

    // ── Import from save ──────────────────────────────────────────────────

    private async Task ImportFromSaveAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        isBusy = true;
        StateHasChanged();

        // Collect all non-empty party + box Pokémon.
        var allPokemon = new List<PKM>();

        for (var i = 0; i < saveFile.PartyCount; i++)
        {
            var pkm = saveFile.GetPartySlotAtIndex(i);
            if (pkm.Species != 0)
            {
                allPokemon.Add(pkm);
            }
        }

        for (var box = 0; box < saveFile.BoxCount; box++)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var pkm = saveFile.GetBoxSlotAtIndex(box, slot);
                if (pkm.Species != 0)
                {
                    allPokemon.Add(pkm);
                }
            }
        }

        if (allPokemon.Count == 0)
        {
            isBusy = false;
            Snackbar.Add("No Pokémon found in the save file.", Severity.Warning);
            return;
        }

        // Duplicate detection — single bank read, O(N+M) via PartitionDuplicatesAsync.
        var (unique, duplicates) = await BankService.PartitionDuplicatesAsync(allPokemon);
        var toAdd = unique.ToList();

        if (duplicates.Count > 0)
        {
            var skipDuplicates = await DialogService.ShowMessageBoxAsync(
                "Duplicates Detected",
                $"{duplicates.Count} duplicate(s) found. Skip them and add only the {unique.Count} unique Pokémon?",
                yesText: "Skip Duplicates",
                noText: "Add All",
                cancelText: "Cancel");

            switch (skipDuplicates)
            {
                case null:
                    isBusy = false;
                    StateHasChanged();
                    return;
                case false:
                    toAdd = allPokemon;
                    break;
            }
        }

        var tid = saveFile.DisplayTID.ToString(AppService.GetIdFormatString());
        var gameName = SaveFileNameDisplay.FriendlyGameName(saveFile.Version);
        var sourceSave = $"{saveFile.OT} ({tid}, {gameName})";
        await BankService.AddRangeAsync(toAdd, sourceSave: sourceSave);
        await ReloadAsync();

        isBusy = false;
        Snackbar.Add($"{toAdd.Count} Pokémon added to the bank.", Severity.Success);
    }

    // ── Import from file ──────────────────────────────────────────────────

    private async Task OnImportFileChangedAsync()
    {
        if (importFile is null)
        {
            return;
        }

        await OnImportFileAsync(importFile);
        importFile = null;
    }

    private async Task OnImportFileAsync(IBrowserFile file)
    {
        isBusy = true;
        StateHasChanged();

        try
        {
            await using var stream = file.OpenReadStream(Constants.MaxFileSize);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var data = ms.ToArray();

            var ext = Path.GetExtension(file.Name);

            if (ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                await BankService.ImportAsync(data);
                await ReloadAsync();
                Snackbar.Add("Bank imported successfully.", Severity.Success);
                return;
            }

            var pokemon = ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)
                ? ReadPokemonFromZip(data)
                : ReadSinglePokemon(data, ext);

            if (pokemon.Count == 0)
            {
                Snackbar.Add("No Pokémon found in file.", Severity.Warning);
                return;
            }

            await ImportPokemonAsync(pokemon, sourceSave: $"Import: {file.Name}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Import failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            isBusy = false;
            StateHasChanged();
        }
    }

    private async Task ImportPokemonAsync(List<PKM> pokemon, string sourceSave)
    {
        var (unique, duplicates) = await BankService.PartitionDuplicatesAsync(pokemon);
        var toAdd = unique.ToList();

        if (duplicates.Count > 0)
        {
            var skipDuplicates = await DialogService.ShowMessageBoxAsync(
                "Duplicates Detected",
                $"{duplicates.Count} duplicate(s) found. Skip them and add only the {unique.Count} unique Pokémon?",
                yesText: "Skip Duplicates",
                noText: "Add All",
                cancelText: "Cancel");

            switch (skipDuplicates)
            {
                case null:
                    return;
                case false:
                    toAdd = pokemon;
                    break;
            }
        }

        await BankService.AddRangeAsync(toAdd, sourceSave: sourceSave);
        await ReloadAsync();

        var skipped = pokemon.Count - toAdd.Count;
        Snackbar.Add(skipped > 0
                ? $"{toAdd.Count} Pokémon imported ({skipped} duplicates skipped)."
                : $"{toAdd.Count} Pokémon imported.",
            Severity.Success);
    }

    private static List<PKM> ReadSinglePokemon(byte[] data, string ext) =>
        FileUtil.TryGetPKM(data, out var pkm, ext) && pkm.Species != 0
            ? [pkm]
            : [];

    private static List<PKM> ReadPokemonFromZip(byte[] data)
    {
        var result = new List<PKM>();
        using var ms = new MemoryStream(data);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            var entryExt = Path.GetExtension(entry.Name);
            if (!IsPokemonExtension(entryExt))
            {
                continue;
            }

            using var es = entry.Open();
            using var ems = new MemoryStream();
            es.CopyTo(ems);
            var bytes = ems.ToArray();

            if (FileUtil.TryGetPKM(bytes, out var pkm, entryExt) && pkm.Species != 0)
            {
                result.Add(pkm);
            }
        }

        return result;
    }

    private static bool IsPokemonExtension(string ext)
    {
        if (string.IsNullOrEmpty(ext))
        {
            return false;
        }

        var stripped = ext.StartsWith('.') ? ext[1..] : ext;
        foreach (var valid in EntityFileExtension.GetExtensionsAll())
        {
            if (stripped.Equals(valid, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildImportAcceptList()
    {
        var pkmExtensions = EntityFileExtension.GetExtensionsAll().Select(e => "." + e);
        return string.Join(",", pkmExtensions.Concat([".zip", ".json"]));
    }

    // ── Export ────────────────────────────────────────────────────────────

    // Single-entry export: write the raw .pk* file directly, no dialog or zip.
    private async Task ExportSingleAsync(BankEntry entry)
    {
        isBusy = true;
        StateHasChanged();

        try
        {
            var pkm = entry.Pokemon;
            pkm.RefreshChecksum();
            var bytes = new byte[pkm.SIZE_PARTY];
            pkm.WriteDecryptedDataParty(bytes);
            var fileName = AppService.GetCleanFileName(pkm);
            var ext = $".{pkm.Extension}";

            if (await JSRuntime.InvokeAsync<bool>("pkmdsSupportsSaveFilePicker"))
            {
                await JSRuntime.InvokeVoidAsync("showFilePickerAndWrite", fileName, bytes, ext, "Pokémon File");
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("downloadBlob", fileName, bytes, "application/x-pokemon-savedata");
            }

            Snackbar.Add($"{entry.SpeciesName} exported.", Severity.Success);
        }
        catch (JSException ex) when (ex.Message.Contains("AbortError", StringComparison.OrdinalIgnoreCase) ||
                                     ex.Message.Contains("aborted a request", StringComparison.OrdinalIgnoreCase))
        {
            Snackbar.Add("Export cancelled.", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            isBusy = false;
            StateHasChanged();
        }
    }

    private async Task ExportSelectedAsync()
    {
        var selected = entries.Where(e => selectedIds.Contains(e.Id)).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        if (selected.Count == 1)
        {
            await ExportSingleAsync(selected[0]);
            return;
        }

        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        var parameters = new DialogParameters<BankExportDialog>
        {
            { x => x.Entries, selected },
            { x => x.Scope, BankExportDialog.BankExportScope.Selected }
        };
        await DialogService.ShowAsync<BankExportDialog>("Export Selected Pokémon", parameters, options);
    }

    private async Task ExportAllAsync()
    {
        if (entries.Count == 0)
        {
            return;
        }

        if (entries.Count == 1)
        {
            await ExportSingleAsync(entries[0]);
            return;
        }

        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);
        var parameters = new DialogParameters<BankExportDialog>
        {
            { x => x.Entries, entries },
            { x => x.Scope, BankExportDialog.BankExportScope.All }
        };
        await DialogService.ShowAsync<BankExportDialog>("Export All Pokémon", parameters, options);
    }

    private async Task ExportAsync()
    {
        isBusy = true;
        StateHasChanged();

        try
        {
            var data = await BankService.ExportAsync();
            await JSRuntime.InvokeVoidAsync(
                "showFilePickerAndWrite", "pkmds-bank.json", data, ".json", "Pokémon Bank Export");

            Snackbar.Add("Bank exported.", Severity.Success);
        }
        catch (JSException ex) when (ex.Message.Contains("AbortError", StringComparison.OrdinalIgnoreCase) ||
                                     ex.Message.Contains("aborted a request", StringComparison.OrdinalIgnoreCase))
        {
            Snackbar.Add("Export cancelled.", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            isBusy = false;
            StateHasChanged();
        }
    }

    // ── Send to save ──────────────────────────────────────────────────────

    // Core send logic with no UI state management — safe to call in a batch loop.
    // Returns (true, success message) or (false, error message); never throws.
    private (bool Success, string Message) TrySendToSaveCore(BankEntry entry, SaveFile saveFile)
    {
        try
        {
            var pkm = entry.Pokemon.Clone();
            if (pkm.GetType() != saveFile.PKMType)
            {
                // Mirror the AllowIncompatibleConversion pattern used in PokemonSlotComponent
                // so cross-game conversions (e.g. Gen 8 → Gen 9) are attempted even when
                // PKHeX considers them "incompatible".
                var previous = EntityConverter.AllowIncompatibleConversion;
                EntityConverter.AllowIncompatibleConversion = EntityCompatibilitySetting.AllowIncompatibleAll;
                try
                {
                    pkm = EntityConverter.ConvertToType(pkm, saveFile.PKMType, out var result);
                    if (pkm is null || !result.IsSuccess)
                    {
                        return (false, $"Could not convert {entry.SpeciesName} to the current save format.");
                    }
                }
                finally
                {
                    EntityConverter.AllowIncompatibleConversion = previous;
                }
            }

            saveFile.AdaptToSaveFile(pkm);

            return !AppService.TryPlacePokemonInFirstAvailableSlot(pkm)
                ? (false, "No empty slots available in the save file.")
                : (true, $"{entry.SpeciesName} sent to save.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to send {entry.SpeciesName}: {ex.Message}");
        }
    }

    private void SendToSave(BankEntry entry)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        isBusy = true;
        StateHasChanged();

        var (success, message) = TrySendToSaveCore(entry, saveFile);

        isBusy = false;
        Snackbar.Add(message, success
            ? Severity.Success
            : Severity.Warning);
        StateHasChanged();
    }

    private async Task SendSelectedToSaveAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        var toSend = entries.Where(e => selectedIds.Contains(e.Id)).ToList();
        if (toSend.Count == 0)
        {
            return;
        }

        // Set isBusy once around the whole batch to keep the operation atomic
        // from the UI's perspective — no mid-loop UI flicker or re-enabled actions.
        isBusy = true;
        StateHasChanged();

        var sent = 0;
        foreach (var entry in toSend)
        {
            var (success, message) = TrySendToSaveCore(entry, saveFile);
            if (success)
            {
                sent++;
            }
            else
            {
                Snackbar.Add(message, Severity.Error);
            }
        }

        selectedIds.Clear();
        isBusy = false;

        if (sent > 0)
        {
            Snackbar.Add($"{sent} Pokémon sent to save.", Severity.Success);
        }

        StateHasChanged();
    }

    // ── Delete ────────────────────────────────────────────────────────────

    private async Task DeleteOneAsync(BankEntry entry)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Remove Pokémon",
            $"Remove {entry.SpeciesName} from the bank?",
            yesText: "Remove",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        isBusy = true;
        StateHasChanged();

        await BankService.DeleteAsync(entry.Id);
        selectedIds.Remove(entry.Id);
        await ReloadAsync();

        isBusy = false;
        Snackbar.Add($"{entry.SpeciesName} removed from bank.");
    }

    private async Task DeleteSelectedAsync()
    {
        if (selectedIds.Count == 0)
        {
            return;
        }

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Remove Selected",
            $"Remove {selectedIds.Count} Pokémon from the bank?",
            yesText: "Remove",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        isBusy = true;
        StateHasChanged();

        foreach (var id in selectedIds.ToList())
        {
            await BankService.DeleteAsync(id);
        }

        selectedIds.Clear();
        await ReloadAsync();

        isBusy = false;
        Snackbar.Add("Selected Pokémon removed from bank.");
    }
}
