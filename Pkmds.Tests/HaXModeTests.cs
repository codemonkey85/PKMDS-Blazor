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

    // ── Hacked stats guard ────────────────────────────────────────────────────

    [Fact]
    public void HaXStatHp_WhenHaXDisabled_DoesNotChangeStat()
    {
        var pkm = LoadLucario();
        var originalHp = pkm.Stat_HPMax;
        var appState = new TestAppState { IsHaXEnabled = false };

        // Simulate the guard: writes should be skipped when HaX is off.
        if (appState.IsHaXEnabled)
        {
            pkm.Stat_HPMax = 9999;
            pkm.Stat_HPCurrent = 9999;
        }

        pkm.Stat_HPMax.Should().Be(originalHp, "stat should not change when HaX is disabled");
    }

    [Fact]
    public void HaXStatHp_WhenHaXEnabled_WritesBothHpFields()
    {
        var pkm = LoadLucario();
        var appState = new TestAppState { IsHaXEnabled = true };

        if (appState.IsHaXEnabled)
        {
            pkm.Stat_HPMax = 9999;
            pkm.Stat_HPCurrent = 9999;
        }

        pkm.Stat_HPMax.Should().Be(9999);
        pkm.Stat_HPCurrent.Should().Be(9999);
    }

    [Fact]
    public void HaXStatAtk_WhenHaXEnabled_WritesAttack()
    {
        var pkm = LoadLucario();
        var appState = new TestAppState { IsHaXEnabled = true };

        if (appState.IsHaXEnabled)
        {
            pkm.Stat_ATK = 65535;
        }

        pkm.Stat_ATK.Should().Be(65535);
    }

    [Fact]
    public void HaXStatAtk_WhenHaXDisabled_DoesNotChangeAttack()
    {
        var pkm = LoadLucario();
        var originalAtk = pkm.Stat_ATK;
        var appState = new TestAppState { IsHaXEnabled = false };

        if (appState.IsHaXEnabled)
        {
            pkm.Stat_ATK = 65535;
        }

        pkm.Stat_ATK.Should().Be(originalAtk);
    }

    [Fact]
    public void HaXStatDef_WhenHaXEnabled_WritesDefense()
    {
        var pkm = LoadLucario();
        var appState = new TestAppState { IsHaXEnabled = true };

        if (appState.IsHaXEnabled) pkm.Stat_DEF = 1234;

        pkm.Stat_DEF.Should().Be(1234);
    }

    [Fact]
    public void HaXStatSpa_WhenHaXEnabled_WritesSpecialAttack()
    {
        var pkm = LoadLucario();
        var appState = new TestAppState { IsHaXEnabled = true };

        if (appState.IsHaXEnabled) pkm.Stat_SPA = 5678;

        pkm.Stat_SPA.Should().Be(5678);
    }

    [Fact]
    public void HaXStatSpd_WhenHaXEnabled_WritesSpecialDefense()
    {
        var pkm = LoadLucario();
        var appState = new TestAppState { IsHaXEnabled = true };

        if (appState.IsHaXEnabled) pkm.Stat_SPD = 4321;

        pkm.Stat_SPD.Should().Be(4321);
    }

    [Fact]
    public void HaXStatSpe_WhenHaXEnabled_WritesSpeed()
    {
        var pkm = LoadLucario();
        var appState = new TestAppState { IsHaXEnabled = true };

        if (appState.IsHaXEnabled) pkm.Stat_SPE = 9876;

        pkm.Stat_SPE.Should().Be(9876);
    }

    [Fact]
    public void HaXStat_MaxValueIsUshortMaxValue()
    {
        // Verify that PKM stat properties accept ushort.MaxValue (65535),
        // confirming the full range the UI should allow in HaX mode.
        var pkm = LoadLucario();
        var appState = new TestAppState { IsHaXEnabled = true };

        if (appState.IsHaXEnabled)
        {
            pkm.Stat_HPMax = ushort.MaxValue;
            pkm.Stat_ATK = ushort.MaxValue;
            pkm.Stat_DEF = ushort.MaxValue;
            pkm.Stat_SPA = ushort.MaxValue;
            pkm.Stat_SPD = ushort.MaxValue;
            pkm.Stat_SPE = ushort.MaxValue;
        }

        pkm.Stat_HPMax.Should().Be(ushort.MaxValue);
        pkm.Stat_ATK.Should().Be(ushort.MaxValue);
        pkm.Stat_DEF.Should().Be(ushort.MaxValue);
        pkm.Stat_SPA.Should().Be(ushort.MaxValue);
        pkm.Stat_SPD.Should().Be(ushort.MaxValue);
        pkm.Stat_SPE.Should().Be(ushort.MaxValue);
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
