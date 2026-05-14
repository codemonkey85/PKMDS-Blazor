namespace Pkmds.Rcl.Components.MainTabPages;

public partial class TradeSlot : RefreshAwareComponent
{
    // Sprite cache is shared with PokemonSlotComponent via static helpers so the settings
    // "Clear Sprite Cache" action (which targets PokemonSlotComponent) clears this too.
    private bool highResLoaded;
    private byte lastLoadedForm;
    private uint lastLoadedFormArg;
    private bool lastLoadedIsFemale;
    private bool lastLoadedIsShiny;
    private ushort lastLoadedSpecies;
    private SpriteStyle lastLoadedSpriteStyle;

    private LegalityStatus? legalityStatus;
    private string? transferIneligibleReason;

    [Inject]
    private IDragDropService DragDropService { get; set; } = null!;

    [Parameter]
    public PKM? Pokemon { get; set; }

    [Parameter]
    public bool IsSelected { get; set; }

    [Parameter]
    public EventCallback OnSlotClick { get; set; }

    // Version of the save file owning this slot — Game-style high-res sprites are version-specific
    // (e.g. RB vs YW Gen 1 art) so each pane passes its own save's Version instead of piggybacking
    // on AppState.SaveFile which always points at Save A.
    [Parameter]
    public GameVersion OwnerVersion { get; set; } = GameVersion.Any;

    // Owning save file — used for legality analysis so Save B slots aren't adapted against
    // AppState.SaveFile (which is always Save A).
    [Parameter]
    public SaveFile? OwnerSaveFile { get; set; }

    // The paired save in the Trade tab (if any). When set, the slot computes whether this
    // Pokémon can be transferred to it and paints a dimmed / badged state when it can't.
    [Parameter]
    public SaveFile? CounterpartSaveFile { get; set; }

    [Parameter]
    public bool IsPartySlot { get; set; }

    // Box/slot coordinates used by drag/drop. Box slots pass a non-null BoxNumber; party
    // slots leave it null. SlotNumber is the index within that box or the party.
    [Parameter]
    public int? BoxNumber { get; set; }

    [Parameter]
    public int SlotNumber { get; set; }

    // Fired when a Pokémon is dropped onto this slot. The receiver (TradeTab) reads the
    // drag source from IDragDropService and routes to the transfer logic.
    [Parameter]
    public EventCallback<TradeSlotTarget> OnDrop { get; set; }

    // Resolve a species name via the full per-language table (GameInfo.GetStrings),
    // bypassing GameInfo.FilteredSources — the filtered source is pinned to whichever
    // save PKHeX last initialized against, so Gen 5 species render blank when Save A
    // is Gen 1. Always use the app language, not the save's game language.
    private static string GetSpeciesTitle(ushort species)
    {
        var names = GameInfo.GetStrings(GameInfo.CurrentLanguage).specieslist;
        return species < names.Length ? names[species] : "Unknown";
    }

    protected override void OnParametersSet()
    {
        ComputeLegality();
        ComputeTransferEligibility();
        UpdateSpriteState();
    }

    // Proactive eligibility hint. Only flags cases where the conversion is impossible at
    // the *type/format* level — the user still sees an error snackbar for per-mon failures
    // (e.g. DLC-gated species) that only surface during a real ConvertToType call. HaX mode
    // disables the hint because the reflection fallback can force some of these through.
    private void ComputeTransferEligibility()
    {
        transferIneligibleReason = null;

        if (Pokemon is not { Species: > 0 } pk
            || OwnerSaveFile is null
            || CounterpartSaveFile is not { } counterpart
            || AppState.IsHaXEnabled)
        {
            return;
        }

        // Let's Go transfers aren't wired up in either direction (see TradeTab.ExecuteTransferAsync).
        if (OwnerSaveFile is SAV7b || counterpart is SAV7b)
        {
            transferIneligibleReason = "Transfers involving Let’s Go saves aren’t supported.";
            return;
        }

        if (pk.GetType() != counterpart.PKMType
            && !EntityConverter.IsConvertibleToFormat(pk, counterpart.Generation))
        {
            transferIneligibleReason =
                $"Can’t transfer {pk.GetType().Name} to {counterpart.PKMType.Name} (incompatible generation).";
            return;
        }

        // Species/form availability in the destination game. IsConvertibleToFormat only
        // covers the format-level transition (e.g. PK7→PK9); species that don't exist in
        // the destination's dex (e.g. Zygarde in Scarlet/Violet) still need to be blocked.
        if (!counterpart.Personal.IsPresentInGame(pk.Species, pk.Form))
        {
            transferIneligibleReason =
                $"{GetSpeciesTitle(pk.Species)} can’t exist in {counterpart.Version} — species/form isn’t in that game’s dex.";
        }
    }

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
        highResLoaded = lastLoadedSpecies > 0
                        && PokemonSlotComponent.IsHighResLoaded(lastLoadedSpecies, lastLoadedForm, lastLoadedFormArg,
                            lastLoadedIsShiny, lastLoadedIsFemale, lastLoadedSpriteStyle);
    }

    // ReSharper disable once UnusedMember.Local
    private void OnHighResSpriteLoaded()
    {
        highResLoaded = true;
        if (lastLoadedSpecies > 0)
        {
            PokemonSlotComponent.MarkHighResLoaded(lastLoadedSpecies, lastLoadedForm, lastLoadedFormArg,
                lastLoadedIsShiny, lastLoadedIsFemale, lastLoadedSpriteStyle);
        }

        StateHasChanged();
    }

    // ReSharper disable once UnusedMember.Local
    private static void OnHighResSpriteError()
    {
        /* keep showing the bundled sprite — highResLoaded is already false */
    }

    private async Task HandleClick() => await OnSlotClick.InvokeAsync();

    private bool IsDraggable()
    {
        if (Pokemon is not { Species: > 0 } || OwnerSaveFile is null)
        {
            return false;
        }

        // Drag and drop is not supported for Let's Go — mirrors PokemonSlotComponent.
        if (OwnerSaveFile is SAV7b)
        {
            return false;
        }

        return true;
    }

    private void HandleDragStart(DragEventArgs e)
    {
        if (Pokemon is not { Species: > 0 } || OwnerSaveFile is null)
        {
            return;
        }

        DragDropService.StartDrag(Pokemon, OwnerSaveFile, BoxNumber, SlotNumber, IsPartySlot);
        e.DataTransfer.EffectAllowed = "move";
        Haptics.Tap();

        // iOS Safari cancels a drag whose dragstart ends with an empty DataTransfer,
        // which is why long-press lifts the slot then the drag aborts. PokemonSlotComponent
        // sidesteps this by writing DownloadURL for OS drag-out; here we just set a
        // text/plain marker so the drag survives to dragover/drop on touch devices.
        if (JSRuntime is IJSInProcessRuntime inProcessRuntime)
        {
            inProcessRuntime.InvokeVoid("setSlotDragMarker", "pkmds-trade");
        }
    }

    private void HandleDragEnd(DragEventArgs e) => DragDropService.ClearDrag();

    private async Task HandleDrop(DragEventArgs e)
    {
        if (!DragDropService.IsDragging || OwnerSaveFile is null)
        {
            return;
        }

        // Don't fire when dropping onto the exact same slot that the drag started from.
        if (ReferenceEquals(DragDropService.DragSourceSaveFile, OwnerSaveFile)
            && DragDropService.IsDragSourceParty == IsPartySlot
            && DragDropService.DragSourceBoxNumber == BoxNumber
            && DragDropService.DragSourceSlotNumber == SlotNumber)
        {
            DragDropService.ClearDrag();
            return;
        }

        // Silently reject drops of cross-save incompatible Pokémon — the source pane already
        // dims these with a Block badge, so firing an error snackbar on drop would be noise.
        // Same format-level rule as ComputeTransferEligibility; HaX mode bypasses the block
        // to match the source-side dim behaviour.
        if (!AppState.IsHaXEnabled
            && DragDropService.DraggedPokemon is { Species: > 0 } dragged
            && DragDropService.DragSourceSaveFile is { } dragSource
            && !ReferenceEquals(dragSource, OwnerSaveFile))
        {
            if (dragSource is SAV7b || OwnerSaveFile is SAV7b)
            {
                DragDropService.ClearDrag();
                return;
            }

            if (dragged.GetType() != OwnerSaveFile.PKMType
                && !EntityConverter.IsConvertibleToFormat(dragged, OwnerSaveFile.Generation))
            {
                DragDropService.ClearDrag();
                return;
            }

            if (!OwnerSaveFile.Personal.IsPresentInGame(dragged.Species, dragged.Form))
            {
                DragDropService.ClearDrag();
                return;
            }
        }

        var target = new TradeSlotTarget(OwnerSaveFile, IsPartySlot, BoxNumber, SlotNumber);
        await OnDrop.InvokeAsync(target);
    }

    private string GetDragClass()
    {
        if (!DragDropService.IsDragging)
        {
            return string.Empty;
        }

        if (ReferenceEquals(DragDropService.DragSourceSaveFile, OwnerSaveFile)
            && DragDropService.IsDragSourceParty == IsPartySlot
            && DragDropService.DragSourceBoxNumber == BoxNumber
            && DragDropService.DragSourceSlotNumber == SlotNumber)
        {
            return "slot-dragging";
        }

        return string.Empty;
    }

    private bool ShouldShow(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => AppState.ShowLegalIndicator,
        LegalityStatus.Fishy => AppState.ShowFishyIndicator,
        LegalityStatus.Illegal => AppState.ShowIllegalIndicator,
        _ => false
    };

    private static string StatusTitle(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => "Legal",
        LegalityStatus.Fishy => "Fishy",
        _ => "Illegal"
    };

    // Use solid glyphs rather than the *Circle / Cancel variants — those are drawn as
    // cutouts and inherit the sprite behind them. The coloured disc comes from the
    // wrapper's CSS class, so the glyph itself just needs to be a solid white symbol.
    private static string StatusIcon(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => Icons.Material.Filled.Check,
        LegalityStatus.Fishy => Icons.Material.Filled.PriorityHigh,
        _ => Icons.Material.Filled.Close
    };

    private static string StatusClass(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => "legality-indicator-icon--legal",
        LegalityStatus.Fishy => "legality-indicator-icon--fishy",
        _ => "legality-indicator-icon--illegal"
    };

    private void ComputeLegality()
    {
        if (Pokemon is not { Species: > 0 } || AppState.IsHaXEnabled)
        {
            legalityStatus = null;
            return;
        }

        // Analyse against the owning save so Save B slots aren't adapted to Save A.
        // Mirrors AppService.GetLegalityAnalysis's adapt-on-write behaviour locally.
        LegalityAnalysis la;
        if (OwnerSaveFile is { } owner && Pokemon.GetType() == owner.PKMType)
        {
            var clone = Pokemon.Clone();
            owner.AdaptToSaveFile(clone, IsPartySlot);
            la = new LegalityAnalysis(clone);
        }
        else
        {
            la = new LegalityAnalysis(Pokemon);
        }
        var hasInvalid = la.Results.Any(r => r.Judgement == PKHeX.Core.Severity.Invalid)
                         || !MoveResult.AllValid(la.Info.Moves)
                         || !MoveResult.AllValid(la.Info.Relearn);
        if (hasInvalid)
        {
            legalityStatus = LegalityStatus.Illegal;
            return;
        }

        var hasFishy = la.Results.Any(r => r.Judgement == PKHeX.Core.Severity.Fishy);
        legalityStatus = hasFishy
            ? LegalityStatus.Fishy
            : LegalityStatus.Legal;
    }
}
