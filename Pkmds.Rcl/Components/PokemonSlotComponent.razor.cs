namespace Pkmds.Rcl.Components;

public partial class PokemonSlotComponent : IDisposable
{
    // Mirrors PKHeX's internal SaveFile.MaxPartyCount (private there, so we can't reuse it).
    private const int MaxPartyCount = 6;

    // Tracks (species, form, formArg, isShiny, isFemale, spriteStyle) tuples whose high-res sprites have loaded
    // at least once this session. Shared across all instances so switching boxes doesn't re-flash.
    // SpriteStyle is included so switching the setting mid-session never shows stale cached state.
    private static readonly
        HashSet<(ushort Species, byte Form, uint FormArg, bool IsShiny, bool IsFemale, SpriteStyle Style)>
        HighResLoadedSpecies = [];

    private bool highResLoaded;
    private bool isDragOverWithExternalFile;
    private byte lastLoadedForm;
    private uint lastLoadedFormArg;
    private bool lastLoadedIsFemale;
    private bool lastLoadedIsShiny;
    private ushort lastLoadedSpecies;
    private SpriteStyle lastLoadedSpriteStyle;

    private LegalityStatus? legalityStatus;

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
        RefreshService.OnAppStateChanged -= RefreshLegality;

    /// <summary>Clears the session sprite cache, forcing all high-res sprites to reload.</summary>
    public static void ClearSpriteCache() => HighResLoadedSpecies.Clear();

    /// <summary>Returns whether the given sprite combo has loaded high-res in this session.</summary>
    public static bool IsHighResLoaded(ushort species, byte form, uint formArg, bool isShiny, bool isFemale,
        SpriteStyle style) =>
        HighResLoadedSpecies.Contains((species, form, formArg, isShiny, isFemale, style));

    /// <summary>Marks the given sprite combo as high-res-loaded for this session.</summary>
    public static void MarkHighResLoaded(ushort species, byte form, uint formArg, bool isShiny, bool isFemale,
        SpriteStyle style) =>
        HighResLoadedSpecies.Add((species, form, formArg, isShiny, isFemale, style));

    private async Task HandleClick() =>
        await OnSlotClick.InvokeAsync();

    private string GetClass() =>
        GetClassFunction?.Invoke() ?? string.Empty;

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += RefreshLegality;
        ComputeLegalityValid();
    }

    protected override void OnParametersSet()
    {
        ComputeLegalityValid();
        UpdateSpriteState();
    }

    // Updates sprite tracking state from current Pokemon + AppState.SpriteStyle.
    // Called from both OnParametersSet (parent-driven re-render) and RefreshLegality
    // (event-driven re-render) so the two paths stay in sync — particularly important
    // when the sprite style changes mid-session via the settings dialog.
    private void UpdateSpriteState()
    {
        var currentIsShiny = Pokemon?.GetIsShinySafe() ?? false;
        var currentForm = Pokemon?.Form ?? 0;
        var currentFormArg = Pokemon?.GetFormArgument(0) ?? 0;
        var currentIsFemale = Pokemon is not null && PokeApiSpriteUrls.HasFemaleHomeSprite(Pokemon.Species, Pokemon.Gender);
        var currentSpriteStyle = AppState.SpriteStyle;
        if (Pokemon?.Species == lastLoadedSpecies
            && currentForm == lastLoadedForm
            && currentFormArg == lastLoadedFormArg
            && currentIsShiny == lastLoadedIsShiny
            && currentIsFemale == lastLoadedIsFemale
            && currentSpriteStyle == lastLoadedSpriteStyle)
        {
            return;
        }

        lastLoadedSpecies = Pokemon?.Species ?? 0;
        lastLoadedForm = currentForm;
        lastLoadedFormArg = currentFormArg;
        lastLoadedIsShiny = currentIsShiny;
        lastLoadedIsFemale = currentIsFemale;
        lastLoadedSpriteStyle = currentSpriteStyle;
        // If this combo has already loaded high-res in this session, skip the bundled sprite entirely.
        highResLoaded = lastLoadedSpecies > 0
                        && IsHighResLoaded(lastLoadedSpecies, lastLoadedForm, lastLoadedFormArg,
                            lastLoadedIsShiny, lastLoadedIsFemale, lastLoadedSpriteStyle);
    }

    // Gen I/II transparent sprites are 40×40 px — scale up to fill the slot.
    // X/Y and OR/AS sprites are tightly cropped 60×60 px — scale down slightly.
    private string GetHiResSizeClass()
    {
        if (AppState.SpriteStyle != SpriteStyle.Game)
        {
            return string.Empty;
        }

        return AppState.SaveFile?.Version switch
        {
            GameVersion.RD or GameVersion.GN or GameVersion.BU
                or GameVersion.RB or GameVersion.RBY or GameVersion.YW
                or GameVersion.GD or GameVersion.GS or GameVersion.SI or GameVersion.C
                => "pkm-sprite-hires--lg",
            GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS
                => "pkm-sprite-hires--sm",
            _ => string.Empty
        };
    }

    // ReSharper disable once UnusedMember.Local
    private void OnHighResSpriteLoaded()
    {
        highResLoaded = true;
        if (lastLoadedSpecies > 0)
        {
            MarkHighResLoaded(lastLoadedSpecies, lastLoadedForm, lastLoadedFormArg, lastLoadedIsShiny,
                lastLoadedIsFemale, lastLoadedSpriteStyle);
        }

        StateHasChanged();
    }

    // ReSharper disable once UnusedMember.Local
    private static void OnHighResSpriteError()
    {
        /* keep showing the bundled sprite — _highResLoaded is already false */
    }

    private void RefreshLegality()
    {
        ComputeLegalityValid();
        UpdateSpriteState();
        StateHasChanged();
    }

    private void ComputeLegalityValid()
    {
        if (Pokemon is not { Species: > 0 } || AppState.IsHaXEnabled)
        {
            legalityStatus = null;
            return;
        }

        var la = AppService.GetLegalityAnalysis(Pokemon, isParty: IsPartySlot);
        legalityStatus = LegalityUi.GetStatus(la);
    }

    private string GetPokemonTitle() => Pokemon is { Species: > 0 }
        ? AppService.GetPokemonSpeciesName(Pokemon.Species) ?? "Unknown"
        : "Unknown";

    private string GetItemTitle() => Pokemon is { HeldItem: > 0 }
        ? AppService.GetItemComboItem(Pokemon.HeldItem).Text
        : "Unknown";

    private bool IsAlphaPokemon() => Pokemon switch
    {
        IAlpha alpha => alpha.IsAlpha,
        IAlphaReadOnly alphaReadOnly => alphaReadOnly.IsAlpha,
        _ => false
    };

    /// <returns>
    /// The tri-state legality status, or <see langword="null" /> when no Pokémon is in
    /// the slot, HaX mode is on, or the user has disabled the indicator for this status
    /// via settings.
    /// </returns>
    private LegalityStatus? GetLegalityStatus() =>
        legalityStatus switch
        {
            LegalityStatus.Legal when AppState.ShowLegalIndicator => LegalityStatus.Legal,
            LegalityStatus.Fishy when AppState.ShowFishyIndicator => LegalityStatus.Fishy,
            LegalityStatus.Illegal when AppState.ShowIllegalIndicator => LegalityStatus.Illegal,
            _ => null
        };

    private string? GetStatusOverlaySpriteFileName() =>
        ImageHelper.GetStatusOverlaySpriteFileName(Pokemon);

    private (int TeamNumber, bool IsLocked)? GetBattleTeamInfo()
    {
        // Battle team indicators only apply to box slots, not party slots
        if (IsPartySlot || BoxNumber is not { } box || Pokemon is not { Species: > 0 })
        {
            return null;
        }

        if (AppState.SaveFile is not { } sav)
        {
            return null;
        }

        var flags = sav.GetBoxSlotFlags(box, SlotNumber);
        var team = flags.IsBattleTeam();
        if (team < 0)
        {
            return null;
        }

        var isLocked = flags.HasFlag(StorageSlotSource.Locked);
        return (team + 1, isLocked); // Convert to 1-based for display
    }

    private int? GetLetsGoPartySlotNumber()
    {
        // Only show party slot indicators for Let's Go games in the BOX view
        // Don't show indicators in the party view itself
        if (AppState.SaveFile is not SAV7b saveFile || Pokemon is null || Pokemon.Species == 0 || IsPartySlot)
        {
            return null;
        }

        // Use PKHeX's GetBoxSlotFlags to determine if this box slot is a party member
        // In Let's Go, party members are stored in the box and GetBoxSlotFlags returns
        // the party slot index via the IsParty() method
        var flags = saveFile.GetBoxSlotFlags(0, SlotNumber); // Let's Go uses box 0
        var partySlot = flags.IsParty();

        if (partySlot >= 0)
        {
            return partySlot + 1; // Convert 0-based to 1-based for display
        }

        return null;
    }

    private bool IsDraggable()
    {
        // Don't allow dragging if no Pokémon
        if (Pokemon is not { Species: > 0 })
        {
            return false;
        }

        // Drag and drop is not supported for Let's Go games
        if (AppState.SaveFile is SAV7b)
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
        Haptics.Tap();

        // Set drag-out data so the PKM file can be dragged to the OS desktop (Chrome/Edge only).
        // Uses IJSInProcessRuntime for a synchronous call — required because dataTransfer can only
        // be modified during the synchronous dragstart event handler.
        Pokemon.RefreshChecksum();
        var filename = AppService.GetCleanFileName(Pokemon);
        var partyData = new byte[Pokemon.SIZE_PARTY];
        Pokemon.WriteDecryptedDataParty(partyData);
        var base64 = Convert.ToBase64String(partyData);
        if (JSRuntime is IJSInProcessRuntime inProcessRuntime)
        {
            inProcessRuntime.InvokeVoid("setDragDownloadData", filename, base64);
        }
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
        // Clamp PartyCount to the canonical 6-slot maximum. SAV3.PartyCount reads a single
        // raw byte from the save block, and a corrupt or unusual GBA save can report > 6,
        // which would slice past the party buffer in GetPartySlotAtIndex and throw
        // ArgumentOutOfRangeException during render. Mirrors PKHeX's own SaveFile.PartyData
        // getter (issue #844).
        var partyCount = Math.Min(saveFile.PartyCount, MaxPartyCount);
        var count = 0;
        for (var i = 0; i < partyCount; i++)
        {
            // TryGetPartySlot tolerates LGPE (SAV7b) phantom slots that throw on read (issues #942–#948).
            var partyMon = saveFile.TryGetPartySlot(i);
            if (partyMon is { Species: > 0, IsEgg: false })
            {
                count++;
            }
        }

        return count;
    }

    private void HandleDragEnter(DragEventArgs e)
    {
        // Show visual feedback when an external file is dragged over the slot.
        // Internal drags never include "Files" in DataTransfer.Types, so this check
        // is authoritative — don't gate on DragDropService.IsDragging, because that
        // state can be stale after a drag-out-to-OS where `dragend` didn't fire.
        if (!e.DataTransfer.Types.Any(t => t.Equals("Files", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        isDragOverWithExternalFile = true;
        StateHasChanged();
    }

    private void HandleDragLeave(DragEventArgs e)
    {
        if (!isDragOverWithExternalFile)
        {
            return;
        }

        isDragOverWithExternalFile = false;
        StateHasChanged();
    }

    private async Task HandleDrop(DragEventArgs e)
    {
        isDragOverWithExternalFile = false;

        // Prefer external file drops over the internal-drag state. After a successful
        // drag-out-to-OS, Chrome's DownloadURL transfer doesn't always fire `dragend`
        // on the source slot (the drop is consumed by the OS), so DragDropService can
        // still report IsDragging when the user drags the resulting file back in. If
        // we see real files on this drop, treat it as an import and clear any stale
        // internal drag state so the next interaction starts clean.
        if (e.DataTransfer.Files.Length > 0)
        {
            DragDropService.ClearDrag();
            // Render now so the slot's external-drop highlight (driven by
            // isDragOverWithExternalFile above) clears before the potentially
            // long-running import; otherwise the next render only happens after
            // HandleFileDropAsync completes, leaving the highlight on during it.
            StateHasChanged();
            await HandleFileDropAsync(e.DataTransfer.Files);
            return;
        }

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

        // Drag and drop is not supported for Let's Go games
        if (AppState.SaveFile is SAV7b)
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

        // Clear the selection so the editor panel closes after the move
        AppService.ClearSelection();

        DragDropService.ClearDrag();
        Haptics.Confirm();
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

        // Multi-file drop: route through BulkImportDialog with the files preloaded.
        // The first file still lands in the dropped slot via the single-file path below
        // only when exactly one file is dropped.
        if (fileNames.Length > 1)
        {
            await HandleBulkFileDropAsync(fileNames);
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

                // In HaX mode, retry with AllowIncompatibleAll when the normal route fails.
                // This mirrors PKHeX WinForms HaX behaviour and handles cases such as a
                // species that exists only in a DLC region of the target game (e.g. Bibarel
                // dropping into a Violet save before the Teal Mask DLC is detected).
                if ((!c.IsSuccess || pokemon is null) && AppState.IsHaXEnabled)
                {
                    var previous = EntityConverter.AllowIncompatibleConversion;
                    EntityConverter.AllowIncompatibleConversion = EntityCompatibilitySetting.AllowIncompatibleAll;
                    try
                    {
                        pokemon = EntityConverter.ConvertToType(pkm, saveFile.PKMType, out c);
                    }
                    finally
                    {
                        EntityConverter.AllowIncompatibleConversion = previous;
                    }

                    if (c.IsSuccess && pokemon is not null)
                    {
                        Snackbar.Add(
                            $"Warning: {c.GetDisplayString(pkm, saveFile.PKMType)}",
                            Severity.Warning);
                    }
                }

                if (!c.IsSuccess || pokemon is null)
                {
                    Snackbar.Add($"Failed to convert Pokémon: {c.GetDisplayString(pkm, saveFile.PKMType)}",
                        Severity.Error);
                    return;
                }
            }

            saveFile.AdaptToSaveFile(pokemon);

            // Place the Pokemon in the dropped slot. Party is always a packed list in every
            // generation, and Gen 1/2 boxes are packed lists too — compact after the write so
            // dropping past the last filled slot collapses into the next free slot instead of
            // leaving a gap.
            if (IsPartySlot)
            {
                // TrySetPartySlot returns false for an LGPE (SAV7b) party slot whose pointer is the
                // SLOT_EMPTY sentinel — writing there throws in PKHeX and corrupts the in-memory
                // party count (issues #942–#948). Let's Go has no standalone party buffer, so an
                // empty party slot can't be written through the index API.
                if (saveFile.TrySetPartySlot(pokemon, SlotNumber))
                {
                    saveFile.CompactParty();
                    RefreshService.RefreshPartyState();
                }
                else
                {
                    Snackbar.Add(
                        "That party slot can't be edited for Let's Go saves. Use the box instead.",
                        Severity.Warning);
                }
            }
            else if (BoxNumber.HasValue)
            {
                saveFile.SetBoxSlotAtIndex(pokemon, BoxNumber.Value, SlotNumber);
                saveFile.CompactBoxIfGen12(BoxNumber.Value);
                RefreshService.RefreshBoxState();
            }
            else // LetsGo storage
            {
                saveFile.SetBoxSlotAtIndex(pokemon, SlotNumber);
                RefreshService.RefreshBoxState();
            }

            Snackbar.Add(
                $"Successfully imported {AppService.GetPokemonSpeciesName(pokemon.Species) ?? "Pokémon"} from {fileName}",
                Severity.Success);
            Haptics.Success();
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

    private async Task HandleBulkFileDropAsync(string[] fileNames)
    {
        try
        {
            AppState.ShowProgressIndicator = true;

            var preloaded = new List<(string FileName, byte[] Data)>(fileNames.Length);
            for (var i = 0; i < fileNames.Length; i++)
            {
                var base64 = await JSRuntime.InvokeAsync<string>("readDroppedFile", i);
                if (string.IsNullOrEmpty(base64))
                {
                    continue;
                }

                preloaded.Add((fileNames[i], Convert.FromBase64String(base64)));
            }

            if (preloaded.Count == 0)
            {
                Snackbar.Add("Failed to read the dropped files.", Severity.Error);
                return;
            }

            var parameters = new DialogParameters<BulkImportDialog>
            {
                { x => x.PreloadedFiles, preloaded },
                // Box slot drop → fill boxes first; party slot drop → fill party first.
                { x => x.FillBoxesFirst, !IsPartySlot }
            };
            var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Small);

            var dialog = await DialogService.ShowAsync<BulkImportDialog>(
                "Bulk Import .pk* Files", parameters, options);
            await dialog.Result;
            RefreshService.RefreshBoxAndPartyState();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error importing Pokémon: {ex.Message}", Severity.Error);
            await Console.Error.WriteLineAsync($"Error in HandleBulkFileDropAsync: {ex}");
        }
        finally
        {
            AppState.ShowProgressIndicator = false;
        }
    }

    private string GetDragClass()
    {
        if (isDragOverWithExternalFile)
        {
            return "slot-file-drop";
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

        return string.Empty;
    }
}
