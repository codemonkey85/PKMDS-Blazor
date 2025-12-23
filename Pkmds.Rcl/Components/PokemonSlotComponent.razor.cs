namespace Pkmds.Rcl.Components;

public partial class PokemonSlotComponent : IDisposable
{
    [Parameter, EditorRequired]
    public int SlotNumber { get; set; }

    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnSlotClick { get; set; }

    [Parameter, EditorRequired]
    public Func<string>? GetClassFunction { get; set; }

    [Parameter]
    public bool IsPartySlot { get; set; }

    [Inject]
    private IDragDropService DragDropService { get; set; } = default!;

    protected virtual int? BoxNumber => null;

    private bool _isDragOverWithFile = false;

    public void Dispose() =>
        RefreshService.OnAppStateChanged -= StateHasChanged;

    private async Task HandleClick() =>
        await OnSlotClick.InvokeAsync();

    private string GetClass() =>
        GetClassFunction?.Invoke() ?? string.Empty;

    protected override void OnInitialized() =>
        RefreshService.OnAppStateChanged += StateHasChanged;

    private string GetPokemonTitle() => Pokemon is { Species: > 0 }
        ? AppService.GetPokemonSpeciesName(Pokemon.Species) ?? "Unknown"
        : "Unknown";

    private async Task HandleDragStart(DragEventArgs e)
    {
        if (Pokemon is not { Species: > 0 })
        {
            return;
        }

        // Check if this is the last battle-ready Pokémon in the party
        if (IsPartySlot && IsLastBattleReadyPokemon())
        {
            // Don't allow dragging the last battle-ready Pokémon
            return;
        }

        DragDropService.StartDrag(Pokemon, BoxNumber, SlotNumber, IsPartySlot);
        e.DataTransfer.EffectAllowed = "copyMove";
    }

    private void HandleDragEnd(DragEventArgs e)
    {
        DragDropService.ClearDrag();
    }

    private bool IsLastBattleReadyPokemon()
    {
        if (!IsPartySlot || AppState.SaveFile is not { } saveFile)
        {
            return false;
        }

        int battleReadyCount = GetBattleReadyCount(saveFile);

        // This is the last battle-ready Pokémon if there's only 1 and this is it
        return battleReadyCount == 1 && Pokemon is { Species: > 0, IsEgg: false };
    }

    private int GetBattleReadyCount(SaveFile saveFile)
    {
        // Count battle-ready Pokémon in party (non-Eggs with Species > 0)
        int count = 0;
        for (int i = 0; i < saveFile.PartyCount; i++)
        {
            var partyMon = saveFile.GetPartySlotAtIndex(i);
            if (partyMon?.Species > 0 && !partyMon.IsEgg)
            {
                count++;
            }
        }
        return count;
    }

    private void HandleDragEnter(DragEventArgs e)
    {
        // Check if this is a file drag from external source
        if (!DragDropService.IsDragging && e.DataTransfer.Files.Length > 0)
        {
            _isDragOverWithFile = true;
        }
    }

    private void HandleDragOver(DragEventArgs e)
    {
        // Required for drop to work - preventDefault handled by global handler
        // Keep drop indicator active during drag over
        if (!DragDropService.IsDragging && e.DataTransfer.Files.Length > 0)
        {
            _isDragOverWithFile = true;
        }
    }

    private void HandleDragLeave(DragEventArgs e)
    {
        _isDragOverWithFile = false;
    }

    private async Task HandleDrop(DragEventArgs e)
    {
        // Clear drag over state
        _isDragOverWithFile = false;

        // Check for internal drag first - this takes priority over file drops
        if (DragDropService.IsDragging)
        {
            // Don't drop onto the same slot
            if (DragDropService.IsDragSourceParty == IsPartySlot &&
                DragDropService.DragSourceBoxNumber == BoxNumber &&
                DragDropService.DragSourceSlotNumber == SlotNumber)
            {
                DragDropService.ClearDrag();
                StateHasChanged();
                return;
            }

            // Move the Pokémon
            AppService.MovePokemon(
                DragDropService.DragSourceBoxNumber,
                DragDropService.DragSourceSlotNumber,
                DragDropService.IsDragSourceParty,
                BoxNumber,
                SlotNumber,
                IsPartySlot
            );

            DragDropService.ClearDrag();
            StateHasChanged();
            return;
        }

        // Check if this is a file drop from external source
        // Only process if we're not in an internal drag operation
        if (e.DataTransfer.Files.Length > 0)
        {
            await HandleFileDropAsync(e.DataTransfer.Files);
            return;
        }
    }

    private async Task HandleFileDropAsync(string[] fileNames)
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            Snackbar.Add("No save file loaded. Please load a save file first.", Severity.Warning);
            return;
        }

        if (fileNames.Length == 0)
        {
            return;
        }

        var fileName = fileNames[0];

        try
        {
            AppState.ShowProgressIndicator = true;

            // Use JavaScript interop to read the file
            var fileDataBase64 = await JSRuntime.InvokeAsync<string>("readDroppedFile", 0);
            if (string.IsNullOrEmpty(fileDataBase64))
            {
                Snackbar.Add("Failed to read the dropped file.", Severity.Error);
                return;
            }

            var data = Convert.FromBase64String(fileDataBase64);

            // Try to parse as Pokemon file
            if (!FileUtil.TryGetPKM(data, out var pkm, Path.GetExtension(fileName), saveFile))
            {
                Snackbar.Add($"The file '{fileName}' is not a supported Pokémon file.", Severity.Error);
                return;
            }

            var pokemon = pkm.Clone();

            // Convert if needed
            if (pkm.GetType() != saveFile.PKMType)
            {
                pokemon = EntityConverter.ConvertToType(pkm, saveFile.PKMType, out var c);
                if (!c.IsSuccess() || pokemon is null)
                {
                    Snackbar.Add($"Failed to convert Pokémon: {c.GetDisplayString(pkm, saveFile.PKMType)}", Severity.Error);
                    return;
                }
            }

            saveFile.AdaptToSaveFile(pokemon);

            // Place the Pokemon in the dropped slot
            if (IsPartySlot)
            {
                saveFile.SetPartySlotAtIndex(pokemon, SlotNumber);
                RefreshService.RefreshPartyState();
            }
            else if (BoxNumber.HasValue)
            {
                saveFile.SetBoxSlotAtIndex(pokemon, BoxNumber.Value, SlotNumber);
                RefreshService.RefreshBoxState();
            }
            else // LetsGo storage
            {
                saveFile.SetBoxSlotAtIndex(pokemon, SlotNumber);
                RefreshService.RefreshBoxState();
            }

            Snackbar.Add($"Successfully imported {AppService.GetPokemonSpeciesName(pokemon.Species) ?? "Pokémon"} from {fileName}", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error importing Pokémon: {ex.Message}", Severity.Error);
            // TODO: Add proper logging with ILogger when available
            Console.Error.WriteLine($"Error in HandleFileDropAsync: {ex}");
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }
    }

    private string GetDragClass()
    {
        // Show drop indicator when file is being dragged over
        if (_isDragOverWithFile)
        {
            return "slot-drop-target slot-file-drop";
        }

        if (!DragDropService.IsDragging)
        {
            return string.Empty;
        }

        // Check if this is the source slot
        if (DragDropService.IsDragSourceParty == IsPartySlot &&
            DragDropService.DragSourceBoxNumber == BoxNumber &&
            DragDropService.DragSourceSlotNumber == SlotNumber)
        {
            return "slot-dragging";
        }

        // Don't show drop indicator if this is a party slot and the drag source
        // is the last battle-ready Pokémon (which shouldn't be allowed to move out of party)
        if (IsPartySlot && DragDropService.IsDragSourceParty && IsDragSourceLastBattleReadyPokemon())
        {
            // Don't show drop indicators when dragging the last battle-ready Pokémon
            return string.Empty;
        }

        // This is a potential drop target
        return "slot-drop-target";
    }

    private bool IsDragSourceLastBattleReadyPokemon()
    {
        if (!DragDropService.IsDragSourceParty || AppState.SaveFile is not { } saveFile)
        {
            return false;
        }

        int battleReadyCount = GetBattleReadyCount(saveFile);

        // Check if the drag source is the last battle-ready Pokémon
        var dragSourcePokemon = DragDropService.DraggedPokemon;
        return battleReadyCount == 1 && dragSourcePokemon is { Species: > 0, IsEgg: false };
    }
}
