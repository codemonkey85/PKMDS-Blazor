using System.IO.Compression;

namespace Pkmds.Rcl.Components.Dialogs;

public partial class BankExportDialog : IDisposable
{
    public enum BankExportScope
    {
        Selected,
        All
    }

    // Yield cadence inside the zip-build loop. WASM is single-threaded — without periodic
    // yields the UI freezes for noticeable hitches on large bank dumps.
    private const int YieldEveryN = 16;

    private CancellationTokenSource? cts;
    private bool isExporting;
    private double progressPercent;
    private string statusText = string.Empty;

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public IReadOnlyList<BankEntry> Entries { get; set; } = [];

    [Parameter]
    public BankExportScope Scope { get; set; } = BankExportScope.All;

    public void Dispose()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private async Task ExportAsync()
    {
        if (Entries.Count == 0)
        {
            Snackbar.Add("No Pokémon to export.", Severity.Warning);
            return;
        }

        cts?.Dispose();
        cts = new CancellationTokenSource();
        var ct = cts.Token;

        isExporting = true;
        progressPercent = 0;
        statusText = $"Building zip ({Entries.Count} Pokémon)…";
        StateHasChanged();
        await Task.Yield();

        try
        {
            var zipBytes = await BuildZipAsync(Entries, ct);
            var fileName = Scope == BankExportScope.Selected
                ? "pkmds_bank_selected.zip"
                : "pkmds_bank_export.zip";

            statusText = "Saving…";
            progressPercent = 100;
            StateHasChanged();

            await WriteZipAsync(zipBytes, fileName, ct);

            Snackbar.Add($"Exported {Entries.Count} Pokémon.", Severity.Success);
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

    private async Task<byte[]> BuildZipAsync(IReadOnlyList<BankEntry> entries, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < entries.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var pkm = entries[i].Pokemon;
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
                progressPercent = (double)(i + 1) / entries.Count * 95;
                statusText = $"Adding {i + 1}/{entries.Count}: {entryName}";

                if ((i + 1) % YieldEveryN == 0 || i == entries.Count - 1)
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
        // Mirror BulkExportDialog: prefer the save picker specifically, then use the shared
        // download flow. Either path can report user cancellation as AbortError.
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
            // Surface both picker and tap-dialog dismissal as cancellation so callers do not
            // report a successful export when no file was saved.
            throw new OperationCanceledException("Save dialog dismissed.");
        }
    }

    private void CancelExport() => cts?.Cancel();

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());
}
