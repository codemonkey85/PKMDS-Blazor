using System.ComponentModel.DataAnnotations;

namespace Pkmds.Rcl.Components.Dialogs;

public partial class BugReportDialog
{
    private const int MinPrimaryLength = 20;

    // Match the Azure Function's MaxSaveFileSizeBytes so we reject oversized attachments up front
    // instead of silently filing an issue whose save never uploads.
    private const long MaxAttachmentBytes = 8 * 1024 * 1024;

    private static readonly EmailAddressAttribute EmailValidator = new();

    private BugReportCategory category = BugReportCategory.Bug;
    private bool attachSaveFile;

    // Bug-category fields.
    private string actual = string.Empty;
    private string steps = string.Empty;
    private string expected = string.Empty;
    private string reportedSaveSource = string.Empty;

    // Feature / Feedback field.
    private string details = string.Empty;

    private string email = string.Empty;
    private string name = string.Empty;
    private bool isSubmitting;
    private string? submitError;

    // Manually-attached file (for the load-failure case where no save is loaded).
    private IBrowserFile? uploadedFile;
    private byte[]? uploadedBytes;
    private string? uploadedFileName;

    private bool IsEmailValid => !string.IsNullOrWhiteSpace(email) && EmailValidator.IsValid(email);

    // Save attachment only makes sense for bug reports; feature/feedback don't need a save.
    private bool ShowSaveAttach => CapturedException is not null || category == BugReportCategory.Bug;

    private string PrimaryText => category == BugReportCategory.Bug ? actual : details;

    private bool IsPrimaryValid =>
        CapturedException is not null || PrimaryText.Trim().Length >= MinPrimaryLength;

    private bool IsFormValid => IsPrimaryValid && IsEmailValid && !string.IsNullOrWhiteSpace(name);

    private static Func<string, string?> PrimaryValidation => v =>
        (v?.Trim().Length ?? 0) >= MinPrimaryLength
            ? null
            : $"Please provide at least {MinPrimaryLength} characters of detail.";

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public bool HasSaveFile { get; set; }

    [Parameter]
    public string AppVersion { get; set; } = string.Empty;

    [Parameter]
    public Exception? CapturedException { get; set; }

    [Inject]
    private IBugReportService BugReportService { get; set; } = null!;

    protected override void OnInitialized()
    {
        attachSaveFile = HasSaveFile;

        if (CapturedException is not null)
        {
            // Crashes are always bugs, and the structured split doesn't fit a stack dump — route
            // the pre-filled details through the single "actual" field.
            category = BugReportCategory.Bug;
            actual =
                $"[Crash] {CapturedException.GetType().Name}: {CapturedException.Message}" +
                $"\n\nStack trace:\n{CapturedException.StackTrace}" +
                "\n\n--- Additional details ---\nPlease describe what you were doing when this crash occurred:\n\n";
        }
    }

    private async Task OnUploadFileChangedAsync()
    {
        if (uploadedFile is null)
        {
            return;
        }

        submitError = null;

        if (uploadedFile.Size > MaxAttachmentBytes)
        {
            submitError = "That file is larger than the 8 MB attachment limit and can't be attached.";
            uploadedFile = null;
            uploadedBytes = null;
            uploadedFileName = null;
            return;
        }

        try
        {
            // Read the bytes immediately — IBrowserFile is only valid while the picker is mounted,
            // and any later re-render can invalidate the handle.
            await using var stream = uploadedFile.OpenReadStream(MaxAttachmentBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            uploadedBytes = ms.ToArray();
            uploadedFileName = uploadedFile.Name;
            // A manual attachment supersedes the current-save toggle.
            attachSaveFile = false;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read manually-attached bug report file");
            submitError = "Couldn't read that file. Please try attaching it again.";
            uploadedFile = null;
            uploadedBytes = null;
            uploadedFileName = null;
        }
    }

    private void ClearUploadedFile()
    {
        uploadedFile = null;
        uploadedBytes = null;
        uploadedFileName = null;
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private async Task Submit()
    {
        if (!IsPrimaryValid)
        {
            submitError = $"Please provide at least {MinPrimaryLength} characters of detail.";
            return;
        }

        if (!IsEmailValid || string.IsNullOrWhiteSpace(name))
        {
            submitError = "Please provide a valid email address and name before submitting.";
            return;
        }

        isSubmitting = true;
        submitError = null;
        StateHasChanged();

        try
        {
            byte[]? saveBytes = null;
            string? saveFileName = null;
            string? saveGameName = null;
            string? saveRevision = null;
            string? saveFileSource = null;
            string? saveFileType = null;

            // Save diagnostics + attachment only apply to bug reports (and crashes).
            if (ShowSaveAttach && AppState.SaveFile is { } sf)
            {
                saveGameName = SaveFileNameDisplay.FriendlyGameName(sf.Version);
                saveRevision = (sf as ISaveFileRevision)?.SaveRevisionString;
                // Always populate source/type diagnostics — triagers need to know whether a Gen
                // 1 save came from a VC dump or a physical cartridge even when the user opts
                // out of attaching the save bytes, since legality rules diverge between them.
                saveFileSource = SaveSourceDetector.Detect(sf, AppState.SaveFileName, AppState.ManicEmuSaveContext is not null);
                saveFileType = sf.GetType().Name;
            }

            // A manually-attached file takes precedence; it covers the load-failure case where no
            // save is loaded at all (the whole reason the user is reporting).
            if (uploadedBytes is { Length: > 0 })
            {
                saveBytes = uploadedBytes;
                saveFileName = uploadedFileName ?? "save.bin";
            }
            else if (ShowSaveAttach && attachSaveFile && AppState.SaveFile is { } currentSave)
            {
                var rawBytes = currentSave.Write().ToArray();
                // If the current save was loaded from a Manic EMU .3ds.sav ZIP, rebuild the
                // archive so the bug report preserves the wrapper. Without this the submitted
                // bytes are the bare inner save and we can never diagnose ZIP round-trip
                // issues from user reports (see issue #750). The attachment name must carry
                // the compound extension so triagers can see at a glance the payload is a ZIP
                // and not a bare .sav — a generic save.bin fallback would mask that.
                //
                // RebuildZip can throw (InvalidDataException on oversized non-save entries,
                // corrupt archives, etc.). Getting the report through matters more than the
                // wrapper, so on failure we fall back to the bare save — the submission
                // itself must not be blocked by an attach-side issue.
                if (AppState.ManicEmuSaveContext is { } ctx)
                {
                    try
                    {
                        saveBytes = ManicEmuSaveHelper.RebuildZip(ctx, rawBytes);
                        saveFileName = ManicEmuSaveHelper.GetExportFileName(AppState.SaveFileName).ExportName;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to rebuild Manic EMU ZIP for bug report attachment; falling back to bare save");
                        saveBytes = rawBytes;
                        saveFileName = AppState.SaveFileName ?? "save.bin";
                    }
                }
                else
                {
                    saveBytes = rawBytes;
                    saveFileName = AppState.SaveFileName ?? "save.bin";
                }
            }

            var userAgent = await JSRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
            var isBug = category == BugReportCategory.Bug;
            var request = new BugReportRequest(name, email, category, AppVersion, userAgent,
                Actual: isBug ? actual : null,
                Steps: isBug && !string.IsNullOrWhiteSpace(steps) ? steps : null,
                Expected: isBug && !string.IsNullOrWhiteSpace(expected) ? expected : null,
                ReportedSaveSource: isBug && !string.IsNullOrWhiteSpace(reportedSaveSource) ? reportedSaveSource : null,
                Details: isBug ? null : details,
                PkhexVersion: IAppState.PkhexVersion,
                SaveFileBytes: saveBytes, SaveFileName: saveFileName, SaveGameName: saveGameName,
                SaveRevision: saveRevision, SaveFileSource: saveFileSource,
                SaveFileType: saveFileType, CapturedException: CapturedException);
            var result = await BugReportService.SubmitBugReportAsync(request);

            if (result.Success)
            {
                MudDialog.Close(DialogResult.Ok(result.IssueUrl));
            }
            else
            {
                submitError = result.ErrorMessage ?? "Submission failed. Please try again.";
            }
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }
}
