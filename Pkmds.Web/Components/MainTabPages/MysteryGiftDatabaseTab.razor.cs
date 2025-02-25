namespace Pkmds.Web.Components.MainTabPages;

public partial class MysteryGiftDatabaseTab
{
    [Parameter]
    public bool FilterUnavailableSpecies { get; set; } = true;

    private List<MysteryGift> mysteryGiftsList = [];

    private List<MysteryGift> paginatedItems = [];
    private int currentPage = 1;
    private int pageSize = 20; // Number of items per page
    private readonly int[] pagesSizes = [10, 20, 50, 100];

    private int TotalPages => (int)Math.Ceiling((double)mysteryGiftsList.Count / pageSize);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LoadData();
    }

    private void LoadData()
    {
        if (AppState.SaveFile is not { } saveFile)
        {
            return;
        }

        var encounterDatabase = EncounterEvent.GetAllEvents();

        if (FilterUnavailableSpecies)
        {
            encounterDatabase = saveFile switch
            {
                SAV9SV sav9sv => encounterDatabase.Where(IsPresent(sav9sv.Personal)),
                SAV8SWSH sav8swsh => encounterDatabase.Where(IsPresent(sav8swsh.Personal)),
                SAV8BS sav8bs => encounterDatabase.Where(IsPresent(sav8bs.Personal)),
                SAV8LA sav8la => encounterDatabase.Where(IsPresent(sav8la.Personal)),
                SAV7b => encounterDatabase.Where(mysteryGift => mysteryGift is WB7),
                SAV7 => encounterDatabase.Where(mysteryGift => mysteryGift.Generation < 7 || mysteryGift is WC7),
                _ => encounterDatabase.Where(mysteryGift => mysteryGift.Generation <= saveFile.Generation),
            };
        }

        mysteryGiftsList = [.. encounterDatabase];

        foreach (var mysteryGift in mysteryGiftsList)
        {
            mysteryGift.GiftUsed = false;
        }

        UpdatePaginatedItems();

        static Func<MysteryGift, bool> IsPresent<TTable>(TTable personalTable) where TTable : IPersonalTable =>
            mysteryGift => personalTable.IsPresentInGame(mysteryGift.Species, mysteryGift.Form);
    }

    private void UpdatePaginatedItems() => paginatedItems = [.. mysteryGiftsList
        .Skip((currentPage - 1) * pageSize)
        .Take(pageSize)];

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
        await AppService.ImportMysteryGift(mysteryGift, out var isSuccessful, out var resultsMessage);
        Snackbar.Add(resultsMessage, isSuccessful ? MudBlazor.Severity.Success : MudBlazor.Severity.Error);
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
}
