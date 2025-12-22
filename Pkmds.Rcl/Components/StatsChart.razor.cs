using BlazorExpress.ChartJS;

namespace Pkmds.Rcl.Components;

public partial class StatsChart : IDisposable
{
    private const int MinStatValue = 0;
    private const int DefaultMaxStatValue = 650;
    private const int StatStepSize = 50;
    private ChartData chartData = null!;
    private bool isChartInitialized;
    private int maxStatValue = DefaultMaxStatValue;
    private PKM? previousPokemon;
    private int previousStatsHash;

    private RadarChart radarChart = null!;
    private RadarChartOptions radarChartOptions = null!;

    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    public void Dispose()
    {
        RefreshService.OnAppStateChanged -= OnStateChanged;
        RefreshService.OnThemeChanged -= OnThemeChanged;
    }

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += OnStateChanged;
        RefreshService.OnThemeChanged += OnThemeChanged;
        InitializeChartData();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OnThemeChanged(bool isDarkMode)
    {
        Console.WriteLine($"StatsChart.OnThemeChanged called with isDarkMode: {isDarkMode}. Chart initialized: {isChartInitialized}");
        if (!isChartInitialized)
        {
            return;
        }

        try
        {
            Console.WriteLine($"Calling refreshChartColorsWithTheme for chart: {radarChart.Id}");
            await JSRuntime.InvokeVoidAsync("chartHelper.refreshChartColorsWithTheme", radarChart.Id, isDarkMode);
            Console.WriteLine("refreshChartColorsWithTheme completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing chart colors: {ex.Message}");
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OnStateChanged()
    {
        StateHasChanged();
        if (!isChartInitialized)
        {
            return;
        }

        InitializeChartData();
        await UpdateChartAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        var statsChanged = Pokemon is not null && GetStatsHash() != previousStatsHash;

        if ((Pokemon != previousPokemon || statsChanged) && isChartInitialized)
        {
            InitializeChartData();
            await UpdateChartAsync();
        }

        previousPokemon = Pokemon;
        previousStatsHash = GetStatsHash();
    }

    private int GetStatsHash()
    {
        if (Pokemon is null)
        {
            return 0;
        }

        var stats = GetPokemonStats();
        var hash = new HashCode();
        foreach (var stat in stats)
        {
            hash.Add(stat);
        }

        return hash.ToHashCode();
    }

    private static List<string> GetLabels(byte generation, PKM? pkm) =>
        (generation, pkm) switch
        {
            (1, PK1) => ["HP", "Attack", "Defense", "Speed", "Special"],
            _ => ["HP", "Attack", "Defense", "Speed", "Sp. Def", "Sp. Atk"]
        };

    private void InitializeChartData()
    {
        if (Pokemon is null || AppState.SaveFile is not { Generation: var saveGeneration })
        {
            return;
        }

        var labels = GetLabels(saveGeneration, Pokemon);

        var stats = GetPokemonStats();

        maxStatValue = CalculateMaxStatValue(stats);

        chartData = new ChartData
        {
            Labels = labels,
            Datasets =
            [
                new RadarChartDataset
                {
                    Label = "Stats",
                    Data = stats,
                    Fill = true,
                    BackgroundColor = "rgba(100, 181, 246, 0.4)",
                    BorderColor = "rgb(66, 165, 245)",
                    BorderWidth = 2,
                    PointBackgroundColor = ["rgb(66, 165, 245)"],
                    PointBorderColor = ["rgba(255, 255, 255, 0.8)"],
                    PointHoverBackgroundColor = ["rgba(255, 255, 255, 0.8)"],
                    PointHoverBorderColor = ["rgb(66, 165, 245)"]
                }
            ]
        };
        radarChartOptions = new() { Responsive = true };
    }

    private List<double?> GetPokemonStats() => Pokemon is null
        ? []
        : AppState.SaveFile?.Generation == 1 && Pokemon is PK1 pk1
            ?
            [
                pk1.Stat_HPMax,
                pk1.Stat_ATK,
                pk1.Stat_DEF,
                pk1.Stat_SPE,
                pk1.Stat_SPC
            ]
            : [.. Pokemon.Stats];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await radarChart.InitializeAsync(chartData, radarChartOptions);
            isChartInitialized = true;

            // Set radar scale using MinStatValue and dynamically calculated maxStatValue for better visualization of stats
            await JSRuntime.InvokeVoidAsync("chartHelper.setRadarScale", radarChart.Id, MinStatValue, maxStatValue, StatStepSize);

            await UpdateChartLabelsAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task UpdateChartAsync()
    {
        if (!isChartInitialized || Pokemon is null)
        {
            return;
        }

        await radarChart.UpdateAsync(chartData, radarChartOptions);

        // Reapply scale customizations after update
        // chartId, min, max, stepSize
        await JSRuntime.InvokeVoidAsync("chartHelper.setRadarScale", radarChart.Id, MinStatValue, maxStatValue, StatStepSize);
        await UpdateChartLabelsAsync();
    }

    private async Task UpdateChartLabelsAsync()
    {
        if (Pokemon is null || AppState.SaveFile is not { Generation: var saveGeneration })
        {
            return;
        }

        var labels = GetLabels(saveGeneration, Pokemon);

        var values = GetPokemonStats();

        List<int> natureModifiers = Pokemon.StatNature switch
        {
            Nature.Adamant => [0, 1, 0, 0, 0, -1],
            Nature.Bold => [0, -1, 1, 0, 0, 0],
            Nature.Brave => [0, 1, 0, -1, 0, 0],
            Nature.Calm => [0, -1, 0, 0, 1, 0],
            Nature.Careful => [0, 0, 0, 0, 1, -1],
            Nature.Gentle => [0, 0, -1, 0, 1, 0],
            Nature.Hasty => [0, 0, -1, 1, 0, 0],
            Nature.Impish => [0, 0, 1, 0, 0, -1],
            Nature.Jolly => [0, 0, 0, 1, 0, -1],
            Nature.Lax => [0, 0, 1, 0, -1, 0],
            Nature.Lonely => [0, 1, -1, 0, 0, 0],
            Nature.Mild => [0, 0, -1, 0, 0, 1],
            Nature.Modest => [0, -1, 0, 0, 0, 1],
            Nature.Naive => [0, 0, 0, 1, -1, 0],
            Nature.Naughty => [0, 1, 0, 0, -1, 0],
            Nature.Quiet => [0, 0, 0, -1, 0, 1],
            Nature.Rash => [0, 0, -1, 0, 0, 1],
            Nature.Relaxed => [0, 0, 1, -1, 0, 0],
            Nature.Sassy => [0, 0, 0, -1, 1, 0],
            Nature.Timid => [0, -1, 0, 1, 0, 0],
            _ => [0, 0, 0, 0, 0, 0]
        };

        // For Gen I, use only 5 nature modifiers (no nature modifiers exist in Gen I anyway)
        if (saveGeneration == 1 && Pokemon is PK1)
        {
            natureModifiers = [0, 0, 0, 0, 0];
        }

        await JSRuntime.InvokeVoidAsync("chartHelper.updateLabelsWithValues", radarChart.Id, labels, values, natureModifiers);
    }

    private static int CalculateMaxStatValue(List<double?> stats)
    {
        if (stats.Count == 0)
        {
            return DefaultMaxStatValue;
        }

        var maxStat = stats.Where(s => s.HasValue).Max(s => s!.Value);

        var roundedMax = (int)Math.Ceiling(maxStat / StatStepSize) * StatStepSize;

        return Math.Max(roundedMax + StatStepSize, StatStepSize * 2);
    }
}
