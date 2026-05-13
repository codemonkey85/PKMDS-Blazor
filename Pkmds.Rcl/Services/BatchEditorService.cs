namespace Pkmds.Rcl.Services;

public sealed class BatchEditorService(
    IAppState appState,
    IRefreshService refreshService,
    IJSRuntime jsRuntime) : IBatchEditorService
{
    private const string PresetsKey = "pkmds.batcheditor.presets";

    private byte[]? snapshot;

    public bool HasSnapshot => snapshot is not null;

    public async Task<IReadOnlyList<BatchEditorPreviewEntry>> PreviewAsync(string script, BatchEditorScope scope)
    {
        if (appState.SaveFile is not { } sav)
        {
            return [];
        }

        var lines = ParseLines(script);
        if (lines.Length == 0)
        {
            return [];
        }

        var sets = StringInstructionSet.GetBatchSets(lines.AsSpan());
        foreach (var set in sets)
        {
            EntityBatchEditor.ScreenStrings(set.Filters);
            EntityBatchEditor.ScreenStrings(set.Instructions);
        }

        var editor = EntityBatchEditor.Instance;
        var results = new List<BatchEditorPreviewEntry>();

        await foreach (var (pkm, location) in EnumerateScopeAsync(sav, scope))
        {
            if (pkm is not { Species: > 0 })
            {
                continue;
            }

            var clone = pkm.Clone();
            var changes = (
                    from set in sets
                    where editor.IsFilterMatch(set.Filters, clone)
                    from cmd in set.Instructions
                    let before = TryGetPropertyValue(editor, clone, cmd.PropertyName)
                    let result = editor.TryModify(clone, [], [cmd])
                    where result == ModifyResult.Modified
                    let after = TryGetPropertyValue(editor, clone, cmd.PropertyName)
                    where before != after
                    select $"{cmd.PropertyName}: {before} → {after}")
                .ToList();

            results.Add(new BatchEditorPreviewEntry { SpeciesName = GetSpeciesName(pkm), Location = location, Changes = changes });
        }

        return results;
    }

    public async Task<BatchEditorSummary> ApplyAsync(string script, BatchEditorScope scope)
    {
        if (appState.SaveFile is not { } sav)
        {
            return new BatchEditorSummary(0, 0);
        }

        var lines = ParseLines(script);
        if (lines.Length == 0)
        {
            return new BatchEditorSummary(0, 0);
        }

        var sets = StringInstructionSet.GetBatchSets(lines.AsSpan());
        foreach (var set in sets)
        {
            EntityBatchEditor.ScreenStrings(set.Filters);
            EntityBatchEditor.ScreenStrings(set.Instructions);
        }

        var processor = new EntityBatchProcessor();
        var modified = 0;
        var skipped = 0;

        await foreach (var (pkm, _, writeBack) in EnumerateScopeWithWriteBackAsync(sav, scope))
        {
            if (pkm is not { Species: > 0 })
            {
                continue;
            }

            var wasModified = false;

            foreach (var set in sets)
            {
                if (processor.Process(pkm, set.Filters, set.Instructions))
                {
                    wasModified = true;
                }
            }

            if (wasModified)
            {
                writeBack(pkm);
                modified++;
            }
            else
            {
                skipped++;
            }
        }

        refreshService.Refresh();
        return new BatchEditorSummary(modified, skipped);
    }

    public bool CreateSnapshot()
    {
        if (appState.SaveFile is not { } sav)
        {
            return false;
        }

        snapshot = sav.Write().ToArray();
        return true;
    }

    public bool RestoreSnapshot()
    {
        if (snapshot is null)
        {
            return false;
        }

        if (!SaveUtil.TryGetSaveFile(snapshot, out var restored))
        {
            return false;
        }

        ParseSettings.InitFromSaveFileData(restored);
        appState.SaveFile = restored;
        appState.BoxEdit?.LoadBox(restored.CurrentBox);
        snapshot = null;
        refreshService.Refresh();
        return true;
    }

    public async Task<IReadOnlyList<BatchEditorPreset>> GetPresetsAsync()
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", PresetsKey);
            if (json is null)
            {
                return [];
            }

            return JsonSerializer.Deserialize(json, PkmdsJsonContext.Default.BatchEditorPresets) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task SavePresetAsync(BatchEditorPreset preset)
    {
        var presets = (await GetPresetsAsync()).ToList();
        var existing = presets.FindIndex(p => p.Name == preset.Name);
        if (existing >= 0)
        {
            presets[existing] = preset;
        }
        else
        {
            presets.Add(preset);
        }

        await PersistPresetsAsync(presets);
    }

    public async Task DeletePresetAsync(string name)
    {
        var presets = (await GetPresetsAsync()).Where(p => p.Name != name).ToList();
        await PersistPresetsAsync(presets);
    }

    private async Task PersistPresetsAsync(List<BatchEditorPreset> presets)
    {
        try
        {
            var json = JsonSerializer.Serialize(presets, PkmdsJsonContext.Default.BatchEditorPresets);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", PresetsKey, json);
        }
        catch
        {
            // Ignore localStorage failures.
        }
    }

    private static string[] ParseLines(string script) =>
        script.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#'))
            .ToArray();

    private static string GetSpeciesName(PKM pkm)
    {
        var strings = GameInfo.GetStrings(GameInfo.CurrentLanguage);
        var species = strings.Species;
        return pkm.Species < species.Count
            ? species[pkm.Species]
            : pkm.Species.ToString(CultureInfo.InvariantCulture);
    }

    private static string? TryGetPropertyValue(EntityBatchEditor editor, PKM pkm, string propertyName)
    {
        if (!editor.TryGetHasProperty(pkm, propertyName.AsSpan(), out var pi))
        {
            return null;
        }

        return pi.GetValue(pkm)?.ToString();
    }

    private static async IAsyncEnumerable<(PKM Pkm, string Location)> EnumerateScopeAsync(
        SaveFile sav, BatchEditorScope scope)
    {
        if (scope is BatchEditorScope.Party or BatchEditorScope.All)
        {
            for (var i = 0; i < sav.PartyCount; i++)
            {
                yield return (sav.GetPartySlotAtIndex(i), $"Party {i + 1}");
            }
        }

        if (scope is not (BatchEditorScope.Boxes or BatchEditorScope.All))
        {
            yield break;
        }

        for (var box = 0; box < sav.BoxCount; box++)
        {
            for (var slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                yield return (sav.GetBoxSlotAtIndex(box, slot), $"Box {box + 1}, Slot {slot + 1}");
            }

            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<(PKM Pkm, string Location, Action<PKM> WriteBack)>
        EnumerateScopeWithWriteBackAsync(SaveFile sav, BatchEditorScope scope)
    {
        if (scope is BatchEditorScope.Party or BatchEditorScope.All)
        {
            for (var i = 0; i < sav.PartyCount; i++)
            {
                var slot = i;
                yield return (sav.GetPartySlotAtIndex(slot), $"Party {slot + 1}",
                    pkm => sav.SetPartySlotAtIndex(pkm, slot));
            }
        }

        if (scope is not (BatchEditorScope.Boxes or BatchEditorScope.All))
        {
            yield break;
        }

        {
            for (var box = 0; box < sav.BoxCount; box++)
            {
                for (var slot = 0; slot < sav.BoxSlotCount; slot++)
                {
                    var b = box;
                    var s = slot;
                    yield return (sav.GetBoxSlotAtIndex(b, s), $"Box {b + 1}, Slot {s + 1}",
                        pkm => sav.SetBoxSlotAtIndex(pkm, b, s));
                }

                await Task.Yield();
            }
        }
    }
}
