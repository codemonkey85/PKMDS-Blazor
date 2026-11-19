namespace Pkmds.Rcl.Components.Dialogs;

public partial class AddToBankDialog
{
    public enum AddToBankScope
    {
        Party,
        CurrentBox,
        AllBoxes
    }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Inject]
    private IBankService BankService { get; set; } = null!;

    private AddToBankScope scope = AddToBankScope.CurrentBox;
    private bool skipDuplicates = true;
    private bool isAdding;
    private bool isLoading = true;
    private bool isLetsGo;
    private List<CandidateItem> candidates = [];

    private int AddCount => skipDuplicates
        ? candidates.Count(c => c is { IsSelected: true, IsDuplicate: false })
        : candidates.Count(c => c.IsSelected);

    protected override async Task OnInitializedAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            isLoading = false;
            return;
        }

        isLetsGo = saveFile is SAV7b;
        await LoadCandidatesAsync();
    }

    private async Task LoadCandidatesAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            isLoading = false;
            return;
        }

        isLoading = true;
        StateHasChanged();

        var pokemon = CollectPokemon(saveFile);

        var (_, duplicates) = await BankService.PartitionDuplicatesAsync(
            pokemon.Select(p => p.Pokemon));

        HashSet<PKM> duplicateSet =
        [
            with(ReferenceEqualityComparer.Instance),
            .. duplicates
        ];

        candidates = pokemon.Select(p => new CandidateItem
        {
            Pokemon = p.Pokemon,
            IsParty = p.IsParty,
            SpeciesName = AppService.GetPokemonSpeciesName(p.Pokemon.Species)
                          ?? p.Pokemon.Species.ToString(CultureInfo.InvariantCulture),
            IsDuplicate = duplicateSet.Contains(p.Pokemon),
            IsSelected = true,
        }).ToList();

        if (skipDuplicates)
        {
            foreach (var c in candidates.Where(c => c.IsDuplicate))
            {
                c.IsSelected = false;
            }
        }

        isLoading = false;
    }

    private List<(PKM Pokemon, bool IsParty)> CollectPokemon(SaveFile saveFile)
    {
        var result = new List<(PKM, bool)>();

        if (saveFile is SAV7b lgsave)
        {
            var count = 0;
            foreach (var pkm in lgsave.BoxData)
            {
                if (pkm.Species != 0)
                {
                    var flags = lgsave.GetBoxSlotFlags(0, count);
                    var isParty = flags.IsParty() >= 0;
                    result.Add((pkm, isParty));
                }

                count++;
            }

            return result;
        }

        if (scope is AddToBankScope.Party)
        {
            for (var i = 0; i < saveFile.PartyCount; i++)
            {
                var pkm = saveFile.GetPartySlotAtIndex(i);
                if (pkm.Species != 0)
                {
                    result.Add((pkm, true));
                }
            }

            return result;
        }

        var boxes = scope switch
        {
            AddToBankScope.CurrentBox => new[] { AppState.BoxEdit?.CurrentBox ?? saveFile.CurrentBox },
            AddToBankScope.AllBoxes => Enumerable.Range(0, saveFile.BoxCount).ToArray(),
            _ => Array.Empty<int>()
        };

        foreach (var box in boxes)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var pkm = saveFile.GetBoxSlotAtIndex(box, slot);
                if (pkm.Species != 0)
                {
                    result.Add((pkm, false));
                }
            }
        }

        return result;
    }

    private void ToggleSelectAll(bool select)
    {
        foreach (var c in candidates)
        {
            if (!select || !skipDuplicates || !c.IsDuplicate)
            {
                c.IsSelected = select;
            }
        }
    }

    private void OnSkipDuplicatesChanged()
    {
        foreach (var c in candidates.Where(c => c.IsDuplicate))
        {
            c.IsSelected = !skipDuplicates;
        }
    }

    private async Task AddToBankAsync()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        var toAdd = (skipDuplicates
                ? candidates.Where(c => c is { IsSelected: true, IsDuplicate: false })
                : candidates.Where(c => c.IsSelected))
            .Select(c => c.Pokemon)
            .ToList();

        if (toAdd.Count == 0)
        {
            Snackbar.Add("No Pokémon selected.", Severity.Warning);
            return;
        }

        isAdding = true;
        StateHasChanged();

        try
        {
            var tid = saveFile.DisplayTID.ToString(AppService.GetIdFormatString());
            var gameName = SaveFileNameDisplay.FriendlyGameName(saveFile.Version);
            var sourceSave = $"{saveFile.OT} ({tid}, {gameName})";

            await BankService.AddRangeAsync(toAdd, sourceSave: sourceSave);

            var dupeCount = skipDuplicates
                ? 0
                : candidates.Count(c => c is { IsSelected: true, IsDuplicate: true });

            var msg = $"{toAdd.Count} Pokémon added to Bank.";
            if (dupeCount > 0)
            {
                msg += $" ({dupeCount} duplicate{(dupeCount == 1 ? string.Empty : "s")} included.)";
            }

            Snackbar.Add(msg, Severity.Success);
            MudDialog?.Close(DialogResult.Ok(toAdd.Count));
        }
        finally
        {
            isAdding = false;
        }
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());

    private sealed class CandidateItem
    {
        public required PKM Pokemon { get; init; }
        public required string SpeciesName { get; init; }
        public required bool IsParty { get; init; }
        public bool IsDuplicate { get; init; }
        public bool IsSelected { get; set; }
    }
}
