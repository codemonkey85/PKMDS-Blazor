using System.IO.Compression;

namespace Pkmds.Rcl.Components.Dialogs;

public partial class BulkExportDialog : IDisposable
{
    public enum BulkExportScope
    {
        Party,
        CurrentBox,
        AllBoxes,
        Everything
    }

    // Yield cadence inside the zip-build loop. WASM is single-threaded — without periodic
    // yields the UI freezes for noticeable hitches on large bank dumps.
    private const int YieldEveryN = 16;

    private int currentBoxCount;
    private CancellationTokenSource? cts;
    private bool isExporting;
    private int partyCount;
    private double progressPercent;
    private BulkExportScope scope = BulkExportScope.CurrentBox;
    private string statusText = string.Empty;
    private int totalBoxCount;

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    public void Dispose()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    protected override void OnInitialized()
    {
        if (AppState.SaveFile is not { } sav)
        {
            return;
        }

        var currentBox = AppState.BoxEdit?.CurrentBox ?? sav.CurrentBox;
        currentBoxCount = CountBox(sav, currentBox);

        var total = 0;
        for (var box = 0; box < sav.BoxCount; box++)
        {
            total += CountBox(sav, box);
        }

        totalBoxCount = total;
        partyCount = CountParty(sav);

        // Default scope: prefer current box, then any box, then party.
        if (currentBoxCount > 0)
        {
            scope = BulkExportScope.CurrentBox;
        }
        else if (totalBoxCount > 0)
        {
            scope = BulkExportScope.AllBoxes;
        }
        else if (partyCount > 0)
        {
            scope = BulkExportScope.Party;
        }
    }

    private static int CountBox(SaveFile sav, int box)
    {
        var count = 0;
        for (var slot = 0; slot < sav.BoxSlotCount; slot++)
        {
            if (sav.GetBoxSlotAtIndex(box, slot).Species != 0)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountParty(SaveFile sav)
    {
        var count = 0;
        for (var i = 0; i < sav.PartyCount; i++)
        {
            if (sav.GetPartySlotAtIndex(i).Species != 0)
            {
                count++;
            }
        }

        return count;
    }

    private int GetScopedCount() => scope switch
    {
        BulkExportScope.Party => partyCount,
        BulkExportScope.CurrentBox => currentBoxCount,
        BulkExportScope.AllBoxes => totalBoxCount,
        BulkExportScope.Everything => partyCount + totalBoxCount,
        _ => 0
    };

    private async Task ExportAsync()
    {
        if (AppState.SaveFile is not { } sav)
        {
            return;
        }

        var pokemonToExport = CollectPokemon(sav);
        if (pokemonToExport.Count == 0)
        {
            Snackbar.Add("No Pokémon to export.", Severity.Warning);
            return;
        }

        cts?.Dispose();
        cts = new CancellationTokenSource();
        var ct = cts.Token;

        isExporting = true;
        progressPercent = 0;
        statusText = $"Building zip ({pokemonToExport.Count} Pokémon)…";
        StateHasChanged();
        await Task.Yield();

        try
        {
            var zipBytes = await BuildZipAsync(pokemonToExport, ct);
            var fileName = scope switch
            {
                BulkExportScope.Party => "pokemon_export_party.zip",
                BulkExportScope.CurrentBox =>
                    $"pokemon_export_box{(AppState.BoxEdit?.CurrentBox ?? sav.CurrentBox) + 1}.zip",
                BulkExportScope.AllBoxes => "pokemon_export_boxes.zip",
                BulkExportScope.Everything => "pokemon_export.zip",
                _ => "pokemon_export.zip"
            };

            statusText = "Saving…";
            progressPercent = 100;
            StateHasChanged();

            await WriteZipAsync(zipBytes, fileName, ct);

            Snackbar.Add($"Exported {pokemonToExport.Count} Pokémon.", Severity.Success);
            MudDialog?.Close();
        }
        catch (OperationCanceledException)
        {
            Snackbar.Add("Export cancelled.", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            // Capture before nulling so a concurrent component Dispose (which also nulls
            // cts) can't cause us to leak the CTS — exactly one path disposes it.
            var localCts = cts;
            cts = null;
            localCts?.Dispose();
            isExporting = false;
            StateHasChanged();
        }
    }

    private List<PKM> CollectPokemon(SaveFile sav)
    {
        var result = new List<PKM>();

        if (scope is BulkExportScope.Party or BulkExportScope.Everything)
        {
            for (var i = 0; i < sav.PartyCount; i++)
            {
                var pkm = sav.GetPartySlotAtIndex(i);
                if (pkm.Species != 0)
                {
                    result.Add(pkm);
                }
            }
        }

        var boxes = scope switch
        {
            BulkExportScope.CurrentBox => new[] { AppState.BoxEdit?.CurrentBox ?? sav.CurrentBox },
            BulkExportScope.AllBoxes or BulkExportScope.Everything => Enumerable.Range(0, sav.BoxCount).ToArray(),
            _ => []
        };

        foreach (var box in boxes)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                var pkm = sav.GetBoxSlotAtIndex(box, slot);
                if (pkm.Species != 0)
                {
                    result.Add(pkm);
                }
            }
        }

        return result;
    }

    private async Task<byte[]> BuildZipAsync(IReadOnlyList<PKM> pokemon, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < pokemon.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var pkm = pokemon[i];
                pkm.RefreshChecksum();
                var bytes = new byte[pkm.SIZE_PARTY];
                pkm.WriteDecryptedDataParty(bytes);

                var entryName = GetUniqueEntryName(usedNames, AppService.GetCleanFileName(pkm));
                var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                using (var es = entry.Open())
                {
                    es.Write(bytes);
                }

                // Reserve 0→95% for zip building; the final 5% covers the save dialog.
                progressPercent = (double)(i + 1) / pokemon.Count * 95;
                statusText = $"Adding {i + 1}/{pokemon.Count}: {entryName}";

                if ((i + 1) % YieldEveryN == 0 || i == pokemon.Count - 1)
                {
                    StateHasChanged();
                    await Task.Delay(1, ct);
                }
            }
        }

        return ms.ToArray();
    }

    private static string GetUniqueEntryName(HashSet<string> used, string desired)
    {
        if (used.Add(desired))
        {
            return desired;
        }

        var ext = Path.GetExtension(desired);
        var stem = Path.GetFileNameWithoutExtension(desired);
        for (var i = 2; ; i++)
        {
            var candidate = $"{stem}_{i}{ext}";
            if (used.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private async Task WriteZipAsync(byte[] data, string fileName, CancellationToken ct)
    {
        // Mirror MainLayout.WriteFile: prefer the save picker specifically, then use the
        // shared download flow.
        // Thread `ct` through to both JS calls so Cancel stays responsive through the
        // "Saving…" phase (e.g. while the File System Access picker is open).
        try
        {
            if (await JSRuntime.InvokeAsync<bool>("pkmdsSupportsSaveFilePicker"))
            {
                await JSRuntime.InvokeVoidAsync(
                    "showFilePickerAndWrite",
                    ct,
                    fileName,
                    data,
                    ".zip",
                    "Pokémon Export");
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("downloadBlob", ct, fileName, data, "application/zip");
            }
        }
        catch (JSException ex) when (ex.Message.Contains("AbortError", StringComparison.OrdinalIgnoreCase) ||
                                     ex.Message.Contains("aborted a request", StringComparison.OrdinalIgnoreCase))
        {
            // User dismissed either the picker or the tap dialog. Throw so ExportAsync surfaces
            // "cancelled" rather than reporting a successful export with no file written.
            throw new OperationCanceledException("Save dialog dismissed.");
        }
    }

    private void CancelExport() => cts?.Cancel();

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());
}
