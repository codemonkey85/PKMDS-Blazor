namespace Pkmds.Rcl.Components;

public partial class StatsChart : IDisposable
{
    private const int DefaultMaxStatValue = 650;
    private const int StatStepSize = 50;

    private readonly RadarChartOptions radarChartOptions = new()
    {
        ShowLegend = false,
        ShowDataMarkers = true,
        DataPointRadius = 4,
        ShowAxisLabels = true,
        ShowAxisValues = false,
        FillOpacity = 0.3,
        StrokeWidth = 2,
        ShowGridLines = true,
        GridLevels = 5
    };

    private string[] chartLabels = [];
    private List<ChartSeries<double>> chartSeries = [];
    private PKM? previousPokemon;
    private int previousStatsHash;

    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    public void Dispose() => RefreshService.OnAppStateChanged -= OnStateChanged;

    protected override void OnInitialized()
    {
        RefreshService.OnAppStateChanged += OnStateChanged;
        InitializeChartData();
    }

    private void OnStateChanged()
    {
        InitializeChartData();
        StateHasChanged();
    }

    protected override void OnParametersSet()
    {
        var statsChanged = Pokemon is not null && GetStatsHash() != previousStatsHash;

        if (Pokemon != previousPokemon || statsChanged)
        {
            InitializeChartData();
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

    private static List<string> GetBaseLabels(byte generation, PKM? pkm) =>
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

        var baseLabels = GetBaseLabels(saveGeneration, Pokemon);
        var stats = GetPokemonStats();
        var natureModifiers = GetNatureModifiers(saveGeneration);

        chartLabels =
        [
            .. baseLabels.Select((label, i) =>
            {
                var modifier = i < natureModifiers.Count
                    ? natureModifiers[i]
                    : 0;
                var arrow = modifier switch
                {
                    1 => " +",
                    -1 => " -",
                    _ => string.Empty
                };
                var value = i < stats.Count
                    ? stats[i]
                    : 0;
                return $"{label}{arrow} ({value})";
            })
        ];

        chartSeries =
        [
            new ChartSeries<double> { Name = "Stats", Data = stats.Select(s => (double)s).ToArray() }
        ];

        var maxStatValue = CalculateMaxStatValue(stats);
        radarChartOptions.GridLevels = Math.Max(maxStatValue / StatStepSize, 2);
    }

    private List<int> GetPokemonStats()
    {
        if (Pokemon is null)
        {
            return [];
        }

        if (AppState.SaveFile?.Generation == 1 && Pokemon is PK1 pk1)
        {
            return [pk1.Stat_HPMax, pk1.Stat_ATK, pk1.Stat_DEF, pk1.Stat_SPE, pk1.Stat_SPC];
        }

        // Reorder from PKM.Stats [HP, Atk, Def, Spe, SpA, SpD]
        // to chart axis order  [HP, Atk, Def, Spe, SpD, SpA] (Sp. Def before Sp. Atk)
        var s = Pokemon.Stats;
        return [s[0], s[1], s[2], s[3], s[5], s[4]];
    }

    private List<int> GetNatureModifiers(byte saveGeneration)
    {
        if (Pokemon is null)
        {
            return [0, 0, 0, 0, 0, 0];
        }

        if (saveGeneration == 1 && Pokemon is PK1)
        {
            return [0, 0, 0, 0, 0];
        }

        // Indices follow chart axis order: [HP(0), Atk(1), Def(2), Spe(3), SpD(4), SpA(5)]
        return Pokemon.StatNature switch
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
            Nature.Rash => [0, 0, 0, 0, -1, 1],
            Nature.Relaxed => [0, 0, 1, -1, 0, 0],
            Nature.Sassy => [0, 0, 0, -1, 1, 0],
            Nature.Timid => [0, -1, 0, 1, 0, 0],
            _ => [0, 0, 0, 0, 0, 0]
        };
    }

    private static int CalculateMaxStatValue(List<int> stats)
    {
        if (stats.Count == 0)
        {
            return DefaultMaxStatValue;
        }

        var maxStat = stats.Max();
        var roundedMax = (int)Math.Ceiling((double)maxStat / StatStepSize) * StatStepSize;

        return Math.Max(roundedMax + StatStepSize, StatStepSize * 2);
    }
}
