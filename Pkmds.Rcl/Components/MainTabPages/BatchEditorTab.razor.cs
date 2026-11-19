namespace Pkmds.Rcl.Components.MainTabPages;

public partial class BatchEditorTab : RefreshAwareComponent
{
    private static readonly IReadOnlyList<(string Name, string Script)> ExampleScripts =
    [
        ("Give all boxes Pokérus", ".IsPokerus=True"),
        ("Max friendship (party)", "=IsPartySlot=True\n.CurrentFriendship=255"),
        ("Remove held items (boxes)", "=IsBoxSlot=True\n.HeldItem=0"),
        ("Set all to level 100", ".CurrentLevel=100")
    ];

    private List<PropertyRow> allProperties = [];
    private bool isBusy;
    private TabMode mode = TabMode.Reference;
    private string newPresetName = string.Empty;

    private List<BatchEditorPreset> presets = [];

    private List<BatchEditorPreviewEntry> previewResults = [];

    private string propertySearch = string.Empty;
    private BatchEditorScope scope = BatchEditorScope.All;

    private string script = string.Empty;
    private string? selectedPresetName;
    private bool showPropertiesPanel = true;
    private bool showSavePresetDialog;
    private bool showUnchanged = true;

    [Inject]
    private IBatchEditorService BatchEditorService { get; set; } = null!;

    private IEnumerable<BatchEditorPreviewEntry> FilteredPreview =>
        showUnchanged
            ? previewResults
            : previewResults.Where(r => r.HasChanges);

    private IEnumerable<PropertyRow> FilteredProperties =>
        string.IsNullOrWhiteSpace(propertySearch)
            ? allProperties
            : allProperties.Where(p =>
                p.Name.Contains(propertySearch, StringComparison.OrdinalIgnoreCase) ||
                p.TypeName.Contains(propertySearch, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        LoadPropertyList();
        presets = (await BatchEditorService.GetPresetsAsync()).ToList();
    }

    private void LoadPropertyList()
    {
        var editor = EntityBatchEditor.Instance;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<PropertyRow>();

        foreach (var typeProps in editor.Properties)
        {
            foreach (var name in typeProps)
            {
                if (!seen.Add(name))
                {
                    continue;
                }

                editor.TryGetPropertyType(name, out var typeName);
                rows.Add(new PropertyRow(name, typeName ?? "?"));
            }
        }

        allProperties = [.. rows.OrderBy(r => r.Name, StringComparer.Ordinal)];
    }

    private async Task RunPreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return;
        }

        isBusy = true;
        mode = TabMode.Preview;
        previewResults = [];
        StateHasChanged();

        await Task.Yield();

        try
        {
            var results = await BatchEditorService.PreviewAsync(script, scope);
            previewResults = results.ToList();
            showPropertiesPanel = false;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Preview failed: {ex.Message}", Severity.Error);
            mode = TabMode.Reference;
            showPropertiesPanel = true;
        }
        finally
        {
            isBusy = false;
            StateHasChanged();
        }
    }

    private async Task ConfirmApplyAsync()
    {
        var confirm = await DialogService.ShowMessageBoxAsync(
            "Apply Batch Edit",
            "This will modify Pokémon in your save file. You can undo this afterward using the Undo button. Continue?",
            yesText: "Apply",
            cancelText: "Cancel");

        if (confirm is not true)
        {
            return;
        }

        if (!BatchEditorService.CreateSnapshot())
        {
            Snackbar.Add("Unable to create an undo snapshot. Apply was cancelled.", Severity.Error);
            return;
        }

        await ApplyAsync();
    }

    private async Task ApplyAsync()
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return;
        }

        isBusy = true;
        mode = TabMode.Apply;
        StateHasChanged();

        await Task.Yield();

        try
        {
            var summary = await BatchEditorService.ApplyAsync(script, scope);
            Snackbar.Add(
                $"Modified {summary.Modified}, skipped {summary.Skipped}.",
                Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Apply failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            isBusy = false;
            mode = TabMode.Reference;
            StateHasChanged();
        }
    }

    private async Task UndoAsync()
    {
        if (!BatchEditorService.HasSnapshot)
        {
            return;
        }

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Undo Batch Edit",
            "Restore the save file to the state before the last Apply?",
            yesText: "Undo",
            cancelText: "Cancel");

        if (confirm is not true)
        {
            return;
        }

        if (BatchEditorService.RestoreSnapshot())
        {
            Snackbar.Add("Undo successful.", Severity.Success);
        }
        else
        {
            Snackbar.Add("Undo failed — snapshot could not be restored.", Severity.Error);
        }
    }

    private void OnPresetSelected(string? name)
    {
        selectedPresetName = name;
        if (name is null)
        {
            return;
        }

        var preset = presets.FirstOrDefault(p => p.Name == name);
        if (preset is not null)
        {
            script = preset.Script;
        }
    }

    private void OpenSavePresetDialog()
    {
        newPresetName = selectedPresetName ?? string.Empty;
        showSavePresetDialog = true;
    }

    private async Task SavePresetAsync()
    {
        if (string.IsNullOrWhiteSpace(newPresetName))
        {
            return;
        }

        var preset = new BatchEditorPreset { Name = newPresetName.Trim(), Script = script, SavedAt = DateTimeOffset.UtcNow };

        await BatchEditorService.SavePresetAsync(preset);
        presets = (await BatchEditorService.GetPresetsAsync()).ToList();
        selectedPresetName = preset.Name;
        showSavePresetDialog = false;
        newPresetName = string.Empty;
        Snackbar.Add($"Preset \"{preset.Name}\" saved.", Severity.Success);
    }

    private async Task DeletePresetAsync()
    {
        if (selectedPresetName is null)
        {
            return;
        }

        await BatchEditorService.DeletePresetAsync(selectedPresetName);
        presets = (await BatchEditorService.GetPresetsAsync()).ToList();
        selectedPresetName = null;
        Snackbar.Add("Preset deleted.", Severity.Info);
    }

    private static string GetPreviewRowStyle(BatchEditorPreviewEntry entry, int _) =>
        entry.HasChanges
            ? string.Empty
            : "opacity: 0.5;";

    private enum TabMode { Reference, Preview, Apply }

    private sealed record PropertyRow(string Name, string TypeName);
}
