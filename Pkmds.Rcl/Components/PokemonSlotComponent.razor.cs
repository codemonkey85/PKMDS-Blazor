namespace Pkmds.Rcl.Components;

public partial class PokemonSlotComponent : IDisposable
{
    // Removed isDragOverWithFile field - no longer showing drag indicators

    [Parameter]
    [EditorRequired]
    public int SlotNumber { get; set; }

    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [Parameter]
    [EditorRequired]
    public EventCallback OnSlotClick { get; set; }

    [Parameter]
    [EditorRequired]
    public Func<string>? GetClassFunction { get; set; }

    [Parameter]
    public bool IsPartySlot { get; set; }

    [Inject]
    private IDragDropService DragDropService { get; set; } = null!;

    protected virtual int? BoxNumber => null;

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

    private bool IsDraggable()
    {
        // Don't allow dragging if no Pokémon
        if (Pokemon is not { Species: > 0 })
        {
            return false;
        }

        // Don't allow dragging the last battle-ready Pokémon in the party
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (IsPartySlot && IsLastBattleReadyPokemon())
        {
            return false;
        }

        return true;
    }

    private void HandleDragStart(DragEventArgs e)
    {
        if (Pokemon is not { Species: > 0 })
        {
            return;
        }

        DragDropService.StartDrag(Pokemon, BoxNumber, SlotNumber, IsPartySlot);
        e.DataTransfer.EffectAllowed = "copyMove";
    }

    private void HandleDragEnd(DragEventArgs e) => DragDropService.ClearDrag();

    private bool IsLastBattleReadyPokemon()
    {
        if (!IsPartySlot || AppState.SaveFile is not { } saveFile)
        {
            return false;
        }

        var battleReadyCount = GetBattleReadyCount(saveFile);

        // This is the last battle-ready Pokémon if there's only 1 and this is it
        return battleReadyCount == 1 && Pokemon is { Species: > 0, IsEgg: false };
    }

    private static int GetBattleReadyCount(SaveFile saveFile)
    {
        // Count battle-ready Pokémon in party (non-Eggs with Species > 0)
        var count = 0;
        for (var i = 0; i < saveFile.PartyCount; i++)
        {
            var partyMon = saveFile.GetPartySlotAtIndex(i);
            if (partyMon is { Species: > 0, IsEgg: false })
            {
                count++;
            }
        }

        return count;
    }

    private void HandleDragEnter(DragEventArgs e)
    {
        // No visual indicators - just allow drop to work
    }

    private void HandleDragOver(DragEventArgs e)
    {
        // Required for drop to work - preventDefault handled in razor
    }

    private void HandleDragLeave(DragEventArgs e)
    {
        // No visual indicators to clear
    }

    private async Task HandleDrop(DragEventArgs e)
    {
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

            // For Let's Go games, disable party-box dragging (but allow party reordering)
            if (AppState.SaveFile is SAV7b)
            {
                var isPartyToBoxDrag = DragDropService.IsDragSourceParty && !IsPartySlot;
                var isBoxToPartyDrag = !DragDropService.IsDragSourceParty && IsPartySlot;

                if (isPartyToBoxDrag || isBoxToPartyDrag)
                {
                    // Silently prevent party-box dragging for Let's Go games
                    DragDropService.ClearDrag();
                    StateHasChanged();
                    return;
                }
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
                if (!c.IsSuccess || pokemon is null)
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
            await Console.Error.WriteLineAsync($"Error in HandleFileDropAsync: {ex}");
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }
    }

    private string GetDragClass()
    {
        // No visual indicators - removed to prevent persistent trail issue

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

        // No drop indicators
        return string.Empty;
    }
}
