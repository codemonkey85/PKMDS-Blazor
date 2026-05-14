namespace Pkmds.Rcl.Services;

/// <summary>
/// JS interop bridge for embedded hosts. Receives <c>loadSave</c> /
/// <c>requestExport</c> calls from <c>host.js</c> and posts outbound messages
/// back via <c>window.PKMDS.host._sendMessage</c>. Standalone web app behaviour
/// is unaffected — the bridge is constructed eagerly but its methods are only
/// reached when an embedded host calls in.
/// </summary>
/// <remarks>
/// Follows the same singleton-with-static-Instance pattern as
/// <c>RefreshService</c> so the static <c>[JSInvokable]</c> entry points
/// (required by <c>DotNet.invokeMethodAsync</c>) can resolve injected
/// services without a service-locator anti-pattern.
/// </remarks>
public sealed partial class EmbeddedHostBridge
{
    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IRefreshService _refreshService;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<EmbeddedHostBridge> _logger;

    public EmbeddedHostBridge(
        IAppState appState,
        IAppService appService,
        IRefreshService refreshService,
        IJSRuntime jsRuntime,
        ILogger<EmbeddedHostBridge> logger)
    {
        _appState = appState;
        _appService = appService;
        _refreshService = refreshService;
        _jsRuntime = jsRuntime;
        _logger = logger;
        Instance = this;
    }

    private static EmbeddedHostBridge? Instance { get; set; }

    // Preserved under TrimMode=full via PreserveJSInvokable.xml in Pkmds.Web — the linker
    // can't see JS-side calls via DotNet.invokeMethodAsync, and [JSInvokable] alone is not
    // a trim root. Without preservation these methods get stripped and the embedded host
    // bridge silently breaks.
    [JSInvokable(nameof(LoadSaveFromHost))]
    public static Task<bool> LoadSaveFromHost(string bytesBase64, string? fileName) =>
        Task.FromResult(Instance?.LoadSaveFromHostInternal(bytesBase64, fileName) ?? false);

    [JSInvokable(nameof(RequestExportFromHost))]
    public static async Task<bool> RequestExportFromHost()
    {
        if (Instance is not { } self)
        {
            return false;
        }

        return await self.RequestExportFromHostInternal();
    }

    private bool LoadSaveFromHostInternal(string bytesBase64, string? fileName)
    {
        byte[] data;
        try
        {
            data = Convert.FromBase64String(bytesBase64);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(
                ex,
                "Host sent malformed base64 save data ({FileName}). " +
                "loadSave() expects base64-encoded raw save bytes — not a file path or filename. " +
                "Use loadSaveFromUrl(url, fileName) for browser testing if you need to load from a URL.",
                fileName);
            return false;
        }

        _appService.ClearSelection();
        ParseSettings.ClearActiveTrainer();
        _appState.SaveFile = null;
        _appState.ManicEmuSaveContext = null;
        _appState.ShowProgressIndicator = true;

        try
        {
            if (!SaveFileLoader.TryLoad(data, fileName, out var saveFile, out var manicContext))
            {
                _logger.LogError("Host save load failed: invalid format ({FileName})", fileName);
                return false;
            }

            if (!saveFile.State.Exportable)
            {
                _logger.LogWarning("Host save load rejected: not exportable ({FileName})", fileName);
                return false;
            }

            _appState.ManicEmuSaveContext = manicContext;
            // Mirror the standalone load path: InitFromSaveFileData populates
            // ParseSettings.ActiveTrainer and AllowGBCartEra for legality
            // checks (see notes on FinishLoadingSaveFile in MainLayout).
            ParseSettings.InitFromSaveFileData(saveFile);
            _appState.SaveFile = saveFile;
            _appState.SaveFileName = fileName;
            _appState.BoxEdit?.LoadBox(saveFile.CurrentBox);

            _refreshService.RefreshBoxAndPartyState();
            _logger.LogInformation(
                "Host loaded save: {SaveType} gen {Generation} ({FileName})",
                saveFile.GetType().Name, saveFile.Generation, fileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Host save load threw ({FileName})", fileName);
            return false;
        }
        finally
        {
            _appState.ShowProgressIndicator = false;
        }
    }

    private async Task<bool> RequestExportFromHostInternal()
    {
        if (_appState.SaveFile is not { } saveFile)
        {
            _logger.LogWarning("Host requested export but no save file is loaded");
            return false;
        }

        try
        {
            // Note: this emits raw save bytes only. Manic EMU ZIP rebuild is
            // not applied here because the host is expected to deal in raw
            // save bytes directly — embedded contexts don't carry the ZIP
            // wrapper that the standalone web upload flow has to preserve.
            var bytes = saveFile.Write().ToArray();
            var base64 = Convert.ToBase64String(bytes);
            var fileName = _appState.SaveFileName ?? "save.sav";

            // Serialize via source-gen and pass JsonDocument.RootElement (trim-safe
            // primitive) so Blazor's IJS marshaler writes the underlying JSON object
            // across the boundary. host.js receives an object directly — works against
            // both the current parser-tolerant _sendMessage and any older cached host.js
            // that would have mishandled an opaque string payload.
            using var payloadDoc = JsonSerializer.SerializeToDocument(
                new SaveExportPayload(base64, fileName),
                EmbeddedHostJsonContext.Default.SaveExportPayload);

            await _jsRuntime.InvokeVoidAsync(
                "PKMDS.host._sendMessage",
                "saveExport",
                payloadDoc.RootElement);

            _logger.LogInformation("Host export emitted: {ByteCount} bytes ({FileName})",
                bytes.Length, fileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Host export failed");
            return false;
        }
    }

    // ── DTO + source-gen context for the saveExport outbound payload ──────

    private sealed record SaveExportPayload(
        [property: JsonPropertyName("data")] string Data,
        [property: JsonPropertyName("fileName")] string FileName);

    [JsonSerializable(typeof(SaveExportPayload))]
    private sealed partial class EmbeddedHostJsonContext : JsonSerializerContext;
}
