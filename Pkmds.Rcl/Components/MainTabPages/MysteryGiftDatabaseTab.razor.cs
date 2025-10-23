using Pkmds.Rcl.Extensions;

namespace Pkmds.Rcl.Components.MainTabPages;

public partial class MysteryGiftDatabaseTab
{
    private readonly int[] pagesSizes = [10, 20, 50, 100];
    private int currentPage = 1;
    private int itemsPerPage = 20; // Number of items per page

    private List<MysteryGift> mysteryGiftsList = [];

    private List<MysteryGift> paginatedItems = [];

    private string SearchText { get; set; } = string.Empty;

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
        mysteryGiftsList = [.. EncounterDatabase];

        if (!string.IsNullOrEmpty(SearchText))
        {
            mysteryGiftsList = mysteryGiftsList.Where(g => g.Name.Contains(SearchText)).ToList();
        }
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

            if (!convertedEntity.IsSuccess() || pokemon is null)
            {
                await DialogService.ShowMessageBox("Error", convertedEntity.GetDisplayString(tempPokemon, saveFile.PKMType));
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
        PokemonName = 0,
        EventDate = 1,
    }
}
