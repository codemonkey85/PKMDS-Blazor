namespace Pkmds.Tests;

/// <summary>
/// Tests for PKHaX (Illegal Mode) stat-editing behaviour.
/// </summary>
public class HaXModeTests
{
    private const string TestFilesPath = "../../../TestFiles";

    // ── helpers ──────────────────────────────────────────────────────────────

    private static PK5 LoadLucario()
    {
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pkm, ".pk5").Should().BeTrue();
        return (PK5)pkm!;
    }

    // ── IsHaXEnabled default ─────────────────────────────────────────────────

    [Fact]
    public void AppState_IsHaXEnabled_DefaultsFalse()
    {
        var appState = new TestAppState();
        appState.IsHaXEnabled.Should().BeFalse();
    }

    // ── IAppState toggle ──────────────────────────────────────────────────────

    [Fact]
    public void AppState_IsHaXEnabled_CanBeToggled()
    {
        var appState = new TestAppState();
        appState.IsHaXEnabled.Should().BeFalse();

        appState.IsHaXEnabled = true;
        appState.IsHaXEnabled.Should().BeTrue();

        appState.IsHaXEnabled = false;
        appState.IsHaXEnabled.Should().BeFalse();
    }

    // ── HP stat handler ───────────────────────────────────────────────────────

    [Fact]
    public void ApplyHaXStatHp_WhenHaXDisabled_DoesNotChangeStat()
    {
        var pkm = LoadLucario();
        var originalHp = pkm.Stat_HPMax;

        StatsTab.ApplyHaXStatHp(pkm, false, 9999);

        pkm.Stat_HPMax.Should().Be(originalHp, "stat should not change when HaX is disabled");
    }

    [Fact]
    public void ApplyHaXStatHp_WhenHaXEnabled_WritesBothHpFields()
    {
        var pkm = LoadLucario();

        StatsTab.ApplyHaXStatHp(pkm, true, 9999);

        pkm.Stat_HPMax.Should().Be(9999);
        pkm.Stat_HPCurrent.Should().Be(9999);
    }

    // ── Other stat handler ────────────────────────────────────────────────────

    [Fact]
    public void ApplyHaXStat_WhenHaXEnabled_WritesAttack()
    {
        var pkm = LoadLucario();

        StatsTab.ApplyHaXStat(pkm, true, 65535, (s, val) => s.Stat_ATK = val);

        pkm.Stat_ATK.Should().Be(65535);
    }

    [Fact]
    public void ApplyHaXStat_WhenHaXDisabled_DoesNotChangeAttack()
    {
        var pkm = LoadLucario();
        var originalAtk = pkm.Stat_ATK;

        StatsTab.ApplyHaXStat(pkm, false, 65535, (s, val) => s.Stat_ATK = val);

        pkm.Stat_ATK.Should().Be(originalAtk);
    }

    [Fact]
    public void ApplyHaXStat_WhenHaXEnabled_WritesDefense()
    {
        var pkm = LoadLucario();

        StatsTab.ApplyHaXStat(pkm, true, 1234, (s, val) => s.Stat_DEF = val);

        pkm.Stat_DEF.Should().Be(1234);
    }

    [Fact]
    public void ApplyHaXStat_WhenHaXEnabled_WritesSpecialAttack()
    {
        var pkm = LoadLucario();

        StatsTab.ApplyHaXStat(pkm, true, 5678, (s, val) => s.Stat_SPA = val);

        pkm.Stat_SPA.Should().Be(5678);
    }

    [Fact]
    public void ApplyHaXStat_WhenHaXEnabled_WritesSpecialDefense()
    {
        var pkm = LoadLucario();

        StatsTab.ApplyHaXStat(pkm, true, 4321, (s, val) => s.Stat_SPD = val);

        pkm.Stat_SPD.Should().Be(4321);
    }

    [Fact]
    public void ApplyHaXStat_WhenHaXEnabled_WritesSpeed()
    {
        var pkm = LoadLucario();

        StatsTab.ApplyHaXStat(pkm, true, 9876, (s, val) => s.Stat_SPE = val);

        pkm.Stat_SPE.Should().Be(9876);
    }

    [Fact]
    public void ApplyHaXStat_MaxValueIsUshortMaxValue()
    {
        var pkm = LoadLucario();

        StatsTab.ApplyHaXStatHp(pkm, true, ushort.MaxValue);
        StatsTab.ApplyHaXStat(pkm, true, ushort.MaxValue, (s, val) => s.Stat_ATK = val);
        StatsTab.ApplyHaXStat(pkm, true, ushort.MaxValue, (s, val) => s.Stat_DEF = val);
        StatsTab.ApplyHaXStat(pkm, true, ushort.MaxValue, (s, val) => s.Stat_SPA = val);
        StatsTab.ApplyHaXStat(pkm, true, ushort.MaxValue, (s, val) => s.Stat_SPD = val);
        StatsTab.ApplyHaXStat(pkm, true, ushort.MaxValue, (s, val) => s.Stat_SPE = val);

        pkm.Stat_HPMax.Should().Be(ushort.MaxValue);
        pkm.Stat_ATK.Should().Be(ushort.MaxValue);
        pkm.Stat_DEF.Should().Be(ushort.MaxValue);
        pkm.Stat_SPA.Should().Be(ushort.MaxValue);
        pkm.Stat_SPD.Should().Be(ushort.MaxValue);
        pkm.Stat_SPE.Should().Be(ushort.MaxValue);
    }

    // ── nested test helpers ───────────────────────────────────────────────────

    private sealed class TestAppState : IAppState
    {
        public string CurrentLanguage { get; set; } = "en";
        public int CurrentLanguageId => 2;
        public SaveFile? SaveFile { get; set; }
        public BoxEdit? BoxEdit => null;
        public PKM? CopiedPokemon { get; set; }
        public int? SelectedBoxNumber { get; set; }
        public int? SelectedBoxSlotNumber { get; set; }
        public int? SelectedPartySlotNumber { get; set; }
        public bool ShowProgressIndicator { get; set; }
        public string? AppVersion => "Test";
        public bool SelectedSlotsAreValid => true;
        public bool IsHaXEnabled { get; set; }
    }
}
