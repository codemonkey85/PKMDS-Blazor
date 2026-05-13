namespace Pkmds.Rcl.Components.MainTabPages.Trainer;

public partial class TrainerInfoSav9ZaSection
{
    [Parameter]
    [EditorRequired]
    public SAV9ZA SaveFile { get; set; } = null!;

    private static string GetZaStreetName(SAV9ZA za) =>
        za.GetString(za.Blocks.GetBlock(SaveBlockAccessor9ZA.KStreetName).Data);

    private static void SetZaStreetName(SAV9ZA za, string value) =>
        za.SetString(za.Blocks.GetBlock(SaveBlockAccessor9ZA.KStreetName).Data, value, 18, StringConverterOption.ClearZero);

    private void CollectZaTechnicalMachines()
    {
        var count = TechnicalMachine9a.SetAllTechnicalMachines(SaveFile, true);
        Snackbar.Add(count == 0
            ? "All Technical Machines already collected."
            : $"Collected Technical Machines ×{count}.", Severity.Success);
    }

    private void CollectZaColorfulScrews()
    {
        var count = ColorfulScrew9a.CollectScrews(SaveFile);
        Snackbar.Add(count == 0
            ? "All Colorful Screws already collected."
            : $"Collected Colorful Screws ×{count}.", Severity.Success);
    }

    private async Task OpenFashionDialogAsync()
    {
        var parameters = new DialogParameters<Fashion9Dialog>
        {
            { x => x.SaveFile, SaveFile },
        };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Large);
        await DialogService.ShowAsync<Fashion9Dialog>("Fashion Editor", parameters, options);
    }

    private async Task OpenDonutDialogAsync()
    {
        var parameters = new DialogParameters<Donut9Dialog>
        {
            { x => x.SaveFile, SaveFile },
        };
        var options = await DialogOptionsHelper.BuildAsync(MaxWidth.Large);
        await DialogService.ShowAsync<Donut9Dialog>("Donut Editor", parameters, options);
    }

    // ZA-specific currencies. Hyperspace Survey Points are post-DLC 1 only.
    private uint RoyalePoints
    {
        get => SaveFile.TicketPointsRoyale;
        set => SaveFile.TicketPointsRoyale = value;
    }

    private uint RoyalePointsInfinite
    {
        get => SaveFile.TicketPointsRoyaleInfinite;
        set => SaveFile.TicketPointsRoyaleInfinite = value;
    }

    private uint HyperspaceSurveyPoints
    {
        get => SaveFile.Blocks.GetBlockValue<uint>(SaveBlockAccessor9ZA.KHyperspaceSurveyPoints);
        set => SaveFile.Blocks.SetBlockValue(SaveBlockAccessor9ZA.KHyperspaceSurveyPoints, value);
    }
}
