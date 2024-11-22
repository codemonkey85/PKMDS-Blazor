namespace Pkmds.Web.Components.MainTabPages;

public partial class MysteryGiftDatabaseTab
{
    [Parameter]
    public bool FilterUnavailableSpecies { get; set; } = true;

    private List<MysteryGift> MysteryGiftsList = [];

    private List<MysteryGift> PaginatedItems = [];
    private int CurrentPage = 1;
    private int PageSize = 20; // Number of items per page

    private int TotalPages => (int)Math.Ceiling((double)MysteryGiftsList.Count / PageSize);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LoadData();
    }

    private void LoadData()
    {
        if (AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        var encounterDatabase = EncounterEvent.GetAllEvents();

        if (FilterUnavailableSpecies)
        {
            encounterDatabase = saveFile switch
            {
                SAV9SV s9 => encounterDatabase.Where(IsPresent(s9.Personal)),
                SAV8SWSH s8 => encounterDatabase.Where(IsPresent(s8.Personal)),
                SAV8BS b8 => encounterDatabase.Where(IsPresent(b8.Personal)),
                SAV8LA a8 => encounterDatabase.Where(IsPresent(a8.Personal)),
                SAV7b => encounterDatabase.Where(z => z is WB7),
                SAV7 => encounterDatabase.Where(z => z.Generation < 7 || z is WC7),
                _ => encounterDatabase.Where(z => z.Generation <= saveFile.Generation),
            };
        }

        MysteryGiftsList = [.. encounterDatabase];

        foreach (var mysteryGift in MysteryGiftsList)
        {
            mysteryGift.GiftUsed = false;
        }

        UpdatePaginatedItems();

        static Func<MysteryGift, bool> IsPresent<TTable>(TTable pt) where TTable : IPersonalTable => z => pt.IsPresentInGame(z.Species, z.Form);
    }

    private void UpdatePaginatedItems() => PaginatedItems = MysteryGiftsList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

    private void GoToPage() => UpdatePaginatedItems();

    private void OnPageSizeChange()
    {
        CurrentPage = 1; // Reset to the first page
        UpdatePaginatedItems();
    }

    private async Task OnClickCopy(MysteryGift gift)
    {
        if (gift.Species == (ushort)Species.None || AppState is not { SaveFile: { } saveFile })
        {
            return;
        }

        var temp = gift.ConvertToPKM(saveFile);
        var pokemon = EntityConverter.ConvertToType(temp, saveFile.PKMType, out var c);

        if (!c.IsSuccess() || pokemon is null)
        {
            await DialogService.ShowMessageBox("Error", c.GetDisplayString(temp, saveFile.PKMType));
            return;
        }

        saveFile.AdaptPKM(pokemon);
        AppState.CopiedPokemon = pokemon.Clone();

        Snackbar.Add("The selected Pokémon has been copied.");
    }

    private static string RenderListAsHtml(IReadOnlyList<string> items, string tag = "p")
    {
        if (items == null || items.Count == 0)
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
