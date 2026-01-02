using FluentAssertions;
using PKHeX.Core;
using Pkmds.Rcl;
using Pkmds.Rcl.Services;

namespace Pkmds.Tests;

/// <summary>
/// Tests for AppService functionality
/// </summary>
public class AppServiceTests
{
    private const string TestFilesPath = "../../../TestFiles";

    private class TestAppState : IAppState
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
    }

    private class TestRefreshService : IRefreshService
    {
        public int RefreshCount { get; private set; }
        public int RefreshBoxStateCount { get; private set; }
        public int RefreshPartyStateCount { get; private set; }
        public int RefreshBoxAndPartyStateCount { get; private set; }

#pragma warning disable CS0067 // Event is never used - these are required by interface but not needed in test mock
        public event Action? OnAppStateChanged;
        public event Action? OnBoxStateChanged;
        public event Action? OnPartyStateChanged;
        public event Action? OnUpdateAvailable;
        public event Action<bool>? OnThemeChanged;
#pragma warning restore CS0067

        public void Refresh() => RefreshCount++;
        public void RefreshBoxState() => RefreshBoxStateCount++;
        public void RefreshPartyState() => RefreshPartyStateCount++;
        public void RefreshBoxAndPartyState() => RefreshBoxAndPartyStateCount++;
        public void RefreshTheme(bool isDarkMode) { }
        public void ShowUpdateMessage() { }
    }

    [Fact]
    public void GetCleanFileName_Gen5Pokemon_ReturnsCorrectFormat()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pokemon, ".pk5").Should().BeTrue();

        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService);

        // Act
        var fileName = appService.GetCleanFileName(pokemon!);

        // Assert
        fileName.Should().Be("Lucario_B06DDFAD.pk5");
    }

    [Fact]
    public void GetStatModifierString_NeutralNature_ReturnsNeutral()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService);

        // Act
        var result = appService.GetStatModifierString(Nature.Hardy);

        // Assert
        result.Should().Be("(neutral)");
    }

    [Fact]
    public void GetStatModifierString_BoldNature_ReturnsCorrectModifier()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService);

        // Act - Bold: +Def, -Atk
        var result = appService.GetStatModifierString(Nature.Bold);

        // Assert
        result.Should().Contain("Def");
        result.Should().Contain("Atk");
        result.Should().Contain("↑");
        result.Should().Contain("↓");
    }

    [Fact]
    public void ToggleDrawer_ChangesDrawerState()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService);
        var initialState = appService.IsDrawerOpen;

        // Act
        appService.ToggleDrawer();

        // Assert
        appService.IsDrawerOpen.Should().Be(!initialState);
        refreshService.RefreshCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ClearSelection_ResetsAllSelections()
    {
        // Arrange
        var appState = new TestAppState
        {
            SelectedBoxNumber = 1,
            SelectedBoxSlotNumber = 5,
            SelectedPartySlotNumber = 2
        };
        var refreshService = new TestRefreshService();
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pokemon, ".pk5").Should().BeTrue();
        
        var appService = new AppService(appState, refreshService)
        {
            EditFormPokemon = pokemon
        };

        // Act
        appService.ClearSelection();

        // Assert
        appState.SelectedBoxNumber.Should().BeNull();
        appState.SelectedBoxSlotNumber.Should().BeNull();
        appState.SelectedPartySlotNumber.Should().BeNull();
        appService.EditFormPokemon.Should().BeNull();
        refreshService.RefreshCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetSelectedPokemonSlot_PartySlot_ReturnsPartyType()
    {
        // Arrange
        var appState = new TestAppState
        {
            SelectedPartySlotNumber = 0
        };
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService);

        // Act
        var result = appService.GetSelectedPokemonSlot(out var partySlot, out var boxNumber, out var boxSlot);

        // Assert
        result.Should().Be(SelectedPokemonType.Party);
        partySlot.Should().Be(0);
        boxNumber.Should().Be(-1);
        boxSlot.Should().Be(-1);
    }

    [Fact]
    public void ExportPokemonAsShowdown_ValidPokemon_ReturnsShowdownFormat()
    {
        // Arrange
        var filePath = Path.Combine(TestFilesPath, "Lucario_B06DDFAD.pk5");
        var data = File.ReadAllBytes(filePath);
        FileUtil.TryGetPKM(data, out var pokemon, ".pk5").Should().BeTrue();
        
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService);

        // Act
        var showdown = appService.ExportPokemonAsShowdown(pokemon);

        // Assert
        showdown.Should().NotBeNullOrEmpty();
        showdown.Should().Contain("Lucario");
    }

    [Fact]
    public void ExportPokemonAsShowdown_NullPokemon_ReturnsEmpty()
    {
        // Arrange
        var appState = new TestAppState();
        var refreshService = new TestRefreshService();
        var appService = new AppService(appState, refreshService);

        // Act
        var showdown = appService.ExportPokemonAsShowdown(null);

        // Assert
        showdown.Should().BeEmpty();
    }
}
