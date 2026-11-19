namespace Pkmds.Rcl.Components.Pages;

// ReSharper disable once UnusedType.Global
public partial class Home : IDisposable
{
    private const long MaxTestPickerFileSize = 32 * 1024 * 1024; // 32 MB upper bound for save files

    private string? testPickerStatus;
    private bool testPickerError;

    public void Dispose() => RefreshService.OnAppStateChanged -= StateHasChanged;

    protected override void OnInitialized() => RefreshService.OnAppStateChanged += StateHasChanged;

    /// <summary>
    /// Browser-only dev affordance: when running with <c>?host=test</c>, lets the
    /// developer pick a save file from disk and feeds it through the same JS bridge
    /// path a real WKWebView host would use. Avoids needing to drop files in
    /// <c>wwwroot/</c>, base64-encode by hand, or wrestle with Safari's user-gesture
    /// requirements for programmatic file picker clicks.
    /// </summary>
    private async Task OnTestFilePicked(InputFileChangeEventArgs e)
    {
        var file = e.File;

        if (file.Size > MaxTestPickerFileSize)
        {
            testPickerError = true;
            testPickerStatus = $"File is too large ({file.Size:N0} bytes). Save files shouldn't exceed {MaxTestPickerFileSize / 1024 / 1024} MB.";
            return;
        }

        testPickerError = false;
        testPickerStatus = $"Loading {file.Name}…";
        StateHasChanged();

        try
        {
            await using var stream = file.OpenReadStream(MaxTestPickerFileSize);
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            var base64 = Convert.ToBase64String(memory.ToArray());

            // Round-trip through the JS bridge so we exercise the exact path a
            // real host would use, including the JSInvokable -> EmbeddedHostBridge
            // hop. If something's broken end-to-end the test catches it here.
            var success = await JSRuntime.InvokeAsync<bool>(
                "window.PKMDS.host.loadSave", base64, file.Name);

            testPickerError = !success;
            testPickerStatus = success
                ? $"Loaded {file.Name} ({memory.Length:N0} bytes)."
                : $"Bridge rejected the save (see browser console for details).";
        }
        catch (Exception ex)
        {
            testPickerError = true;
            testPickerStatus = $"Load failed: {ex.Message}";
        }
    }
}
