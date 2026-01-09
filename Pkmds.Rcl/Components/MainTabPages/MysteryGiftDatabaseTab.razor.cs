namespace Pkmds.Rcl.Components.MainTabPages;

public partial class MysteryGiftDatabaseTab
{
    private readonly int[] pagesSizes = [10, 20, 50, 100];
    private int currentPage = 1;
    private int itemsPerPage = 20; // Number of items per page

    private List<MysteryGift> mysteryGiftsList = [];

    private List<MysteryGift> paginatedItems = [];

    private string SearchText { get; set; } = string.Empty;

    private bool ShinyOnly { get; set; }

    private SortOptions SelectedSortOption { get; set; } = SortOptions.None;

    private SortDirection SelectedSortDirection { get; set; } = SortDirection.Ascending;

    private IEnumerable<MysteryGift> EncounterDatabase { get; set; } = [];

    [Parameter]
    public bool FilterUnavailableSpecies { get; set; } = true;

    private int TotalPages => (int)Math.Ceiling((double)mysteryGiftsList.Count / itemsPerPage);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LoadData();
    }

    private void OnSortOrFilterChanged()
    {
        IEnumerable<MysteryGift> mysteryGifts = [.. EncounterDatabase];

        if (!string.IsNullOrEmpty(SearchText))
        {
            mysteryGifts = mysteryGifts
                .Where(g => TextMatch(g, SearchText));
        }

        if (ShinyOnly)
        {
            mysteryGifts =
                mysteryGifts.Where(g => g.Shiny.IsShiny());
        }

        if (SelectedSortOption == SortOptions.PokemonId)
        {
            mysteryGifts = SelectedSortDirection switch
            {
                SortDirection.Ascending => mysteryGifts.OrderBy(g => g.Species),
                SortDirection.Descending => mysteryGifts.OrderByDescending(g => g.Species),
                _ => mysteryGifts
            };
        }

        mysteryGiftsList = mysteryGifts.ToList();
        UpdatePaginatedItems();

        static bool TextMatch(MysteryGift g, string searchText) =>
            g.GetTextLines()
                .Any(tl => tl.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private void LoadData()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        EncounterDatabase = EncounterEvent.GetAllEvents();

        if (FilterUnavailableSpecies)
        {
            EncounterDatabase = saveFile switch
            {
                SAV9SV sav9Sv => EncounterDatabase.Where(IsPresent(sav9Sv.Personal)),
                SAV8SWSH sav8Swsh => EncounterDatabase.Where(IsPresent(sav8Swsh.Personal)),
                SAV8BS sav8Bs => EncounterDatabase.Where(IsPresent(sav8Bs.Personal)),
                SAV8LA sav8La => EncounterDatabase.Where(IsPresent(sav8La.Personal)),
                SAV7b => EncounterDatabase.Where(mysteryGift => mysteryGift is WB7),
                SAV7 => EncounterDatabase.Where(mysteryGift => mysteryGift.Generation < 7 || mysteryGift is WC7),
                _ => EncounterDatabase.Where(mysteryGift => mysteryGift.Generation <= saveFile.Generation)
            };
        }

        mysteryGiftsList = [.. EncounterDatabase];

        foreach (var mysteryGift in mysteryGiftsList)
        {
            mysteryGift.GiftUsed = false;
        }

        UpdatePaginatedItems();

        static Func<MysteryGift, bool> IsPresent<TTable>(TTable personalTable) where TTable : IPersonalTable =>
            mysteryGift => personalTable.IsPresentInGame(mysteryGift.Species, mysteryGift.Form);
    }

    private void UpdatePaginatedItems() => paginatedItems =
    [
        .. mysteryGiftsList
            .Skip((currentPage - 1) * itemsPerPage)
            .Take(itemsPerPage)
    ];

    private void GoToPage() => UpdatePaginatedItems();

    private void OnPageSizeChange()
    {
        currentPage = 1; // Reset to the first page
        UpdatePaginatedItems();
    }

    private async Task OnClickCopy(MysteryGift mysteryGift)
    {
        if (mysteryGift.Species.IsInvalidSpecies() || AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        var tempPokemon = mysteryGift.ConvertToPKM(saveFile);
        var pokemon = tempPokemon.Clone();

        if (tempPokemon.GetType() != saveFile.PKMType)
        {
            pokemon = EntityConverter.ConvertToType(tempPokemon, saveFile.PKMType, out var convertedEntity);

            if (!convertedEntity.IsSuccess || pokemon is null)
            {
                await DialogService.ShowMessageBox("Error",
                    convertedEntity.GetDisplayString(tempPokemon, saveFile.PKMType));
                return;
            }
        }

        saveFile.AdaptToSaveFile(pokemon);
        AppState.CopiedPokemon = pokemon.Clone();

        Snackbar.Add("The selected Pokémon has been copied.");
    }

    private async Task OnClickImport(MysteryGift mysteryGift)
    {
        if (mysteryGift is not DataMysteryGift dataMysteryGift)
        {
            return;
        }

        await AppService.ImportMysteryGift(dataMysteryGift, out var isSuccessful, out var resultsMessage);
        Snackbar.Add(resultsMessage, isSuccessful
            ? Severity.Success
            : Severity.Error);
    }

    private static string RenderListAsHtml(IReadOnlyList<string> items, string tag = "p")
    {
        if (items.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var item in items)
        {
            builder.AppendFormat("<{0}>{1}</{0}>", tag, WebUtility.HtmlEncode(item));
        }

        return builder.ToString();
    }

    private enum SortOptions
    {
        None,

        [Description("Pokémon")]
        PokemonId
    }

    private enum SortDirection
    {
        Ascending,
        Descending
    }
}
