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

        DragDropService.StartDrag(Pokemon, BoxNumber, SlotNumber, IsPartySlot);
        e.DataTransfer.EffectAllowed = "copyMove";
        
        // Prepare for potential external drag (export)
        await PrepareExternalDrag();
    }

    private async Task PrepareExternalDrag()
    {
        if (Pokemon is null)
        {
            return;
        }

        try
        {
            // Store the Pokemon data for potential export
            Pokemon.RefreshChecksum();
            var cleanFileName = AppService.GetCleanFileName(Pokemon);
            var data = Pokemon.DecryptedPartyData;
            
            // Store in JavaScript for potential download
            await JSRuntime.InvokeVoidAsync("storePokemonForExport", cleanFileName, data);
        }
        catch (Exception ex)
        {
            // Non-critical failure - just log, don't show to user
            Console.Error.WriteLine($"Error preparing external drag: {ex}");
        }
    }

    private async Task HandleDragEnd(DragEventArgs e)
    {
        // Check if drag ended outside the app (potential export)
        // Note: DropEffect may not be reliable across all browsers
        // As a fallback, we also check if the drag didn't result in an internal move
        var wasInternalDrop = DragDropService.IsDragging && e.DataTransfer.DropEffect != "none";
        
        if (!wasInternalDrop && Pokemon is { Species: > 0 })
        {
            // Drag ended without internal drop - likely dragged outside, trigger export
            await TriggerPokemonExport();
        }

        DragDropService.EndDrag();
        StateHasChanged();
    }

    private async Task TriggerPokemonExport()
    {
        if (Pokemon is null)
        {
            return;
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("downloadStoredPokemon");
            Snackbar.Add($"Exported {AppService.GetPokemonSpeciesName(Pokemon.Species) ?? "Pokémon"}", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to export Pokémon", Severity.Error);
            // TODO: Add proper logging with ILogger when available
            Console.Error.WriteLine($"Error exporting Pokemon: {ex}");
        }
    }

    private async Task HandleDrop(DragEventArgs e)
    {
        // Check if this is a file drop from external source
        if (e.DataTransfer.Files.Length > 0)
        {
            await HandleFileDropAsync(e.DataTransfer.Files);
            return;
        }

        // Handle internal drag and drop
        if (!DragDropService.IsDragging)
        {
            return;
        }

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

        // This is a potential drop target
        return "slot-drop-target";
    }
}
