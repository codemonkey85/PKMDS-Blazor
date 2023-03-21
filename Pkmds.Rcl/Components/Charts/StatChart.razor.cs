namespace Pkmds.Rcl.Components.Charts;

public partial class StatChart
{
    [Parameter, EditorRequired]
    public PKM Pokemon { get; set; } = default!;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var stats = Pokemon.GetStats(Pokemon.PersonalInfo);
        Data = new[]
        {
            stats[(int)Stats.Hp].ToString(),
            stats[(int)Stats.Attack].ToString(),
            stats[(int)Stats.Defense].ToString(),
            stats[(int)Stats.Speed].ToString(),
            stats[(int)Stats.SpecialDefense].ToString(),
            stats[(int)Stats.SpecialAttack].ToString(),
        };
    }

    private enum Stats
    {
        Hp,
        Attack,
        Defense,
        Speed,
        SpecialAttack,
        SpecialDefense,
    }

    private string[] Data { get; set; } = Array.Empty<string>();

    private static string[] Labels => new[]
    {
        "HP",
        "Attack",
        "Defense",
        "Speed",
        "Special Defense",
        "Special Attack",
    };

    private static string[] BackgroundColor => new[]
    {
        "#A6A6A688"
    };
}
