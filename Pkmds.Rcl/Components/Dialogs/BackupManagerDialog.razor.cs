namespace Pkmds.Rcl.Components.Dialogs;

public partial class BackupManagerDialog
{
    private List<BackupEntry> backups = [];
    private bool isLoading = true;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public SaveFile? SaveFile { get; set; }

    [Parameter]
    public string? FileName { get; set; }

    [Parameter]
    public SaveArchiveContext? ArchiveContext { get; set; }

    [Inject]
    private IBackupService BackupService { get; set; } = null!;

    [Inject]
    private ISettingsService SettingsService { get; set; } = null!;

    protected override async Task OnInitializedAsync() => await LoadBackups();

    private async Task LoadBackups()
    {
        isLoading = true;
        try
        {
            var entries = await BackupService.GetAllMetadataAsync();
            backups = entries.OrderByDescending(e => e.CreatedAt).ToList();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnCreateManualBackup()
    {
        if (SaveFile is null)
        {
            return;
        }

        SaveFile.PrepareForExport();
        var rawBytes = SaveFile.Write().ToArray();
        var data = ArchiveContext is not null
            ? SaveArchiveHelper.RebuildZip(ArchiveContext, rawBytes)
            : rawBytes;
        await BackupService.CreateBackupAsync(
            data,
            SaveFile,
            FileName,
            isManicEmu: ArchiveContext?.IsManicEmu == true,
            source: "manual");
        await BackupService.EnforceRetentionAsync(SettingsService.Settings.MaxBackupCount);
        await LoadBackups();
        Snackbar.Add("Backup created.", Severity.Success);
    }

    private async Task OnRestore(BackupEntry entry)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Restore Backup",
            $"This will replace your current save with the backup from {entry.CreatedAt.LocalDateTime:g}. Continue?",
            "Restore",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        var bytes = await BackupService.GetBackupBytesAsync(entry.Id);
        if (bytes is null)
        {
            Snackbar.Add("Backup data not found.", Severity.Error);
            return;
        }

        MudDialog.Close(DialogResult.Ok(new BackupRestoreResult(bytes, entry)));
    }

    private async Task OnExport(BackupEntry entry)
    {
        var bytes = await BackupService.GetBackupBytesAsync(entry.Id);
        if (bytes is null)
        {
            Snackbar.Add("Backup data not found.", Severity.Error);
            return;
        }

        MudDialog.Close(DialogResult.Ok(new BackupExportResult(bytes, entry)));
    }

    private async Task OnDelete(BackupEntry entry)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Backup",
            $"Delete the backup from {entry.CreatedAt.LocalDateTime:g}?",
            "Delete",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        await BackupService.DeleteAsync(entry.Id);
        await LoadBackups();
        Snackbar.Add("Backup deleted.", Severity.Info);
    }

    private async Task OnClearAll()
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Clear All Backups",
            "This will permanently delete all backups. Continue?",
            "Clear All",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        await BackupService.ClearAsync();
        await LoadBackups();
        Snackbar.Add("All backups cleared.", Severity.Info);
    }

    private void Close() => MudDialog.Close(DialogResult.Cancel());

    private static string FormatGame(BackupEntry entry) =>
        $"{entry.GameVersion} (Gen {entry.Generation})";

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}

/// <summary>Result returned when the user requests to restore a backup.</summary>
public sealed record BackupRestoreResult(byte[] SaveBytes, BackupEntry Entry);

/// <summary>Result returned when the user requests to export a backup to disk.</summary>
public sealed record BackupExportResult(byte[] SaveBytes, BackupEntry Entry);
