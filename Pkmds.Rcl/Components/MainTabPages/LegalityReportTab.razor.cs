using PKHexSeverity = PKHeX.Core.Severity;

namespace Pkmds.Rcl.Components.MainTabPages;

public partial class LegalityReportTab : RefreshAwareComponent
{
    private List<LegalityReportEntry> allEntries = [];
    private List<LegalityReportEntry> filteredEntries = [];
    private bool hasRun;
    private bool isScanning;
    private LegalityStatus? statusFilter;

    /// <summary>
    /// Callback invoked after a row is clicked to jump to the Party / Box tab.
    /// </summary>
    [Parameter]
    public EventCallback OnJumpToPartyBox { get; set; }

    private int LegalCount => allEntries.Count(e => e.Status == LegalityStatus.Legal);
    private int FishyCount => allEntries.Count(e => e.Status == LegalityStatus.Fishy);
    private int IllegalCount => allEntries.Count(e => e.Status == LegalityStatus.Illegal);

    private async Task RunScanAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        isScanning = true;
        hasRun = false;
        allEntries = [];
        filteredEntries = [];
        statusFilter = null;
        StateHasChanged();

        // Yield once so the spinner renders before the CPU-bound sweep begins.
        await Task.Yield();

        var entries = new List<LegalityReportEntry>();

        // --- Party slots ---
        for (var i = 0; i < saveFile.PartyCount; i++)
        {
            var pkm = saveFile.GetPartySlotAtIndex(i);
            if (pkm is not { Species: > 0 })
            {
                continue;
            }

            var la = AppService.GetLegalityAnalysis(pkm);
            entries.Add(BuildEntry(pkm, la, isParty: true, box: 0, slot: i));
        }

        // --- Box slots (yield after every box to keep the UI responsive) ---
        for (var box = 0; box < saveFile.BoxCount; box++)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var pkm = saveFile.GetBoxSlotAtIndex(box, slot);
                if (pkm is not { Species: > 0 })
                {
                    continue;
                }

                var la = AppService.GetLegalityAnalysis(pkm);
                entries.Add(BuildEntry(pkm, la, isParty: false, box: box, slot: slot));
            }

            // Yield after every box so the progress spinner stays animated.
            await Task.Yield();
        }

        allEntries = entries;
        ApplyFilter();
        isScanning = false;
        hasRun = true;
        StateHasChanged();
    }

    private LegalityReportEntry BuildEntry(PKM pkm, LegalityAnalysis la, bool isParty, int box, int slot)
    {
        var speciesName = AppService.GetPokemonSpeciesName(pkm.Species) ?? pkm.Species.ToString(CultureInfo.InvariantCulture);
        var location = isParty
            ? $"Party {slot + 1}"
            : $"Box {box + 1}, Slot {slot + 1}";

        return new LegalityReportEntry
        {
            Pokemon = pkm,
            SpeciesName = speciesName,
            Location = location,
            Status = GetStatus(la),
            FirstIssue = GetFirstIssue(la),
            IsParty = isParty,
            BoxNumber = box,
            SlotNumber = slot
        };
    }

    private static LegalityStatus GetStatus(LegalityAnalysis la)
    {
        var hasInvalid = la.Results.Any(r => r.Judgement == PKHexSeverity.Invalid)
                         || !MoveResult.AllValid(la.Info.Moves)
                         || !MoveResult.AllValid(la.Info.Relearn);

        if (hasInvalid)
        {
            return LegalityStatus.Illegal;
        }

        var hasFishy = la.Results.Any(r => r.Judgement == PKHexSeverity.Fishy);
        return hasFishy
            ? LegalityStatus.Fishy
            : LegalityStatus.Legal;
    }

    private static string GetFirstIssue(LegalityAnalysis la)
    {
        var ctx = LegalityLocalizationContext.Create(la);

        foreach (var result in la.Results)
        {
            if (!result.Valid)
            {
                return ctx.Humanize(in result, verbose: false);
            }
        }

        if (!MoveResult.AllValid(la.Info.Moves))
        {
            return "Invalid move detected.";
        }

        if (!MoveResult.AllValid(la.Info.Relearn))
        {
            return "Invalid relearn move detected.";
        }

        return string.Empty;
    }

    private void SetFilter(LegalityStatus? filter)
    {
        statusFilter = filter;
        ApplyFilter();
    }

    private void ApplyFilter() =>
        filteredEntries = statusFilter is null
            ? [.. allEntries]
            : [.. allEntries.Where(e => e.Status == statusFilter)];

    private async Task OnRowClickAsync(TableRowClickEventArgs<LegalityReportEntry> args)
    {
        if (args.Item is not { } entry)
        {
            return;
        }

        if (entry.IsParty)
        {
            AppService.SetSelectedPartyPokemon(entry.Pokemon, entry.SlotNumber);
        }
        else if (AppState.SaveFile is SAV7b lgsave)
        {
            // Let's Go renders all boxes as a single flat scrollable list.
            // Convert box+slot to the flat index (0..999) that SetSelectedLetsGoPokemon expects.
            var flatSlot = entry.BoxNumber * lgsave.BoxSlotCount + entry.SlotNumber;
            AppService.SetSelectedLetsGoPokemon(entry.Pokemon, flatSlot);
        }
        else
        {
            AppService.SetSelectedBoxPokemon(entry.Pokemon, entry.BoxNumber, entry.SlotNumber);
        }

        await OnJumpToPartyBox.InvokeAsync();
    }

    private static Color GetStatusColor(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => Color.Success,
        LegalityStatus.Fishy => Color.Warning,
        LegalityStatus.Illegal => Color.Error,
        _ => Color.Default
    };

    private static string GetStatusIcon(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => Icons.Material.Filled.CheckCircle,
        LegalityStatus.Fishy => Icons.Material.Filled.Warning,
        LegalityStatus.Illegal => Icons.Material.Filled.Cancel,
        _ => Icons.Material.Filled.Help
    };

    private static string GetStatusLabel(LegalityStatus status) => status switch
    {
        LegalityStatus.Legal => "Legal",
        LegalityStatus.Fishy => "Fishy",
        LegalityStatus.Illegal => "Illegal",
        _ => "Unknown"
    };
}
