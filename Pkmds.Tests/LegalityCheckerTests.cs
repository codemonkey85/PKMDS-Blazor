namespace Pkmds.Tests;

/// <summary>
/// Service-level unit tests for <see cref="AppService.GetLegalityAnalysis" />.
/// Runs sequentially to avoid cross-test interference on <see cref="ParseSettings" /> global state.
/// </summary>
[Collection("Sequential")]
public class LegalityCheckerTests
{
    private const string TestFilesPath = "../../../TestFiles";

    // ── Helpers ───────────────────────────────────────────────────────────

    private static (AppService Service, SaveFile SaveFile) CreateService(string fileName)
    {
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, fileName));
        SaveUtil.TryGetSaveFile(data, out var saveFile, fileName).Should().BeTrue();
        var appState = new TestAppState { SaveFile = saveFile };
        return (new AppService(appState, new TestRefreshService(), new LegalizationService()), saveFile!);
    }

    private static PKM LoadPkm(string fileName)
    {
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, fileName));
        FileUtil.TryGetPKM(data, out var pkm, Path.GetExtension(fileName)).Should().BeTrue();
        return pkm!;
    }

    // ── Service tests ─────────────────────────────────────────────────────

    [Fact]
    public void GetLegalityAnalysis_KnownLegalPokemon_ReturnsAllValid()
    {
        var (service, saveFile) = CreateService("Black - Full Completion.sav");
        ParseSettings.InitFromSaveFileData(saveFile);

        // Find a non-empty party slot
        PKM? pkm = null;
        for (var i = 0; i < saveFile.PartyCount; i++)
        {
            var candidate = saveFile.GetPartySlotAtIndex(i);
            if (candidate.Species > 0)
            {
                pkm = candidate;
                break;
            }
        }

        pkm.Should().NotBeNull("Black full completion save must have at least one party Pokémon");

        var la = service.GetLegalityAnalysis(pkm!);

        la.Results.All(r => r.Valid).Should().BeTrue("all check results should be valid for a known-legal Pokémon");
        MoveResult.AllValid(la.Info.Moves).Should().BeTrue("all moves should be valid for a known-legal Pokémon");
        MoveResult.AllValid(la.Info.Relearn).Should().BeTrue("all relearn moves should be valid for a known-legal Pokémon");
    }

    [Fact]
    public void GetLegalityAnalysis_IllegalAbility_ReturnsInvalidAbilityResult()
    {
        // Lucario from a Gen 5 event — mutate AbilityNumber to 4 (Hidden Ability flag)
        // to trigger an ability legality failure.
        var (_, saveFile) = CreateService("Black - Full Completion.sav");
        ParseSettings.InitFromSaveFileData(saveFile);

        var pkm = LoadPkm("Lucario_B06DDFAD.pk5");
        pkm.AbilityNumber = 4; // Hidden Ability — illegal for this specific event Lucario
        pkm.RefreshChecksum();

        var service = new AppService(new TestAppState(), new TestRefreshService(), new LegalizationService());
        var la = service.GetLegalityAnalysis(pkm);

        la.Results.Any(r => !r.Valid && r.Identifier == CheckIdentifier.Ability)
            .Should().BeTrue("mutating AbilityNumber to 4 on an event Lucario should produce an Ability violation");
    }

    [Fact]
    public void GetLegalityAnalysis_HackedShiny_ReturnsInvalidResult()
    {
        // Load Black save and pick a box Pokémon, then force its PID to a shiny value
        // while keeping the original TID/SID, producing a shiny + PID/EC mismatch.
        var (service, saveFile) = CreateService("Black - Full Completion.sav");
        ParseSettings.InitFromSaveFileData(saveFile);

        PKM? pkm = null;
        for (var box = 0; box < saveFile.BoxCount; box++)
        {
            for (var slot = 0; slot < saveFile.BoxSlotCount; slot++)
            {
                var candidate = saveFile.GetBoxSlotAtIndex(box, slot);
                if (candidate.Species > 0 && !candidate.IsShiny)
                {
                    pkm = candidate;
                    goto foundCandidate;
                }
            }
        }

    foundCandidate:

        pkm.Should().NotBeNull("Black full completion save must have at least one non-shiny box Pokémon");

        // Force shiny by flipping the shiny bits in the PID (Gen 5 uses PID ^ TID ^ SID < 8 check)
        pkm!.PID ^= 0x10000000u; // dirty flip: makes shiny but breaks PID/gender/nature consistency
        pkm.RefreshChecksum();

        var la = service.GetLegalityAnalysis(pkm);

        // Either a Shiny or PID/EC violation should appear
        var hasViolation = !la.Valid || la.Results.Any(r => !r.Valid && r.Identifier is
            CheckIdentifier.Shiny or CheckIdentifier.PID or CheckIdentifier.EC);
        hasViolation.Should().BeTrue("forcing an illegal shiny PID should produce a legality violation");
    }

    [Fact]
    public void GetLegalityAnalysis_CheckResult_JudgementMatchesSeverity()
    {
        var (service, saveFile) = CreateService("Black - Full Completion.sav");
        ParseSettings.InitFromSaveFileData(saveFile);

        // Legal Pokémon: every result judgement should be Valid
        var pkm = saveFile.GetPartySlotAtIndex(0);
        pkm.Species.Should().BeGreaterThan(0);

        var la = service.GetLegalityAnalysis(pkm);
        la.Results.Should().AllSatisfy(r =>
            r.Judgement.Should().Be(Severity.Valid,
                "all check results for a known-legal Pokémon should carry Valid judgement"));

        // Introduce a violation and confirm at least one Invalid judgement appears
        pkm.AbilityNumber = 4;
        pkm.RefreshChecksum();

        var laInvalid = service.GetLegalityAnalysis(pkm);
        laInvalid.Results.Any(r => r.Judgement == Severity.Invalid)
            .Should().BeTrue("mutating the Pokémon should produce at least one Invalid judgement");
    }

    [Fact]
    public void GetLegalityAnalysis_NullSpecies_ReturnsValidOrDoesNotThrow()
    {
        var (service, saveFile) = CreateService("Black - Full Completion.sav");
        var blank = saveFile.BlankPKM;
        blank.Species.Should().Be(0, "BlankPKM must have Species 0");

        LegalityAnalysis? la = null;
        var act = () => { la = service.GetLegalityAnalysis(blank); };

        act.Should().NotThrow("GetLegalityAnalysis must not throw for a blank slot (Species 0)");
        la.Should().NotBeNull();
        la!.Valid.Should().BeTrue("a blank Species-0 slot is trivially valid in PKHeX");
    }

    [Fact]
    public void HumanizeCheckResult_InvalidResult_ReturnsNonEmptyString()
    {
        var (service, saveFile) = CreateService("Black - Full Completion.sav");
        ParseSettings.InitFromSaveFileData(saveFile);

        var pkm = LoadPkm("Lucario_B06DDFAD.pk5");
        pkm.AbilityNumber = 4;

        var la = service.GetLegalityAnalysis(pkm);
        var failingResult = la.Results.FirstOrDefault(r => !r.Valid);
        failingResult.Valid.Should().BeFalse("there should be at least one failing CheckResult");

        var ctx = LegalityLocalizationContext.Create(la);
        var message = ctx.Humanize(in failingResult);

        message.Should().NotBeNullOrEmpty("humanizing a failing CheckResult must return a descriptive string");
        // Should not leak raw enum names — a real localised message won't look like "LegalityCheckResultCode.Xyz"
        message.Should().NotContain("LegalityCheckResultCode", "humanize should return readable text, not raw code names");
    }

    [Fact]
    public void GetLegalityAnalysis_Gen1Pokemon_RecognisesPhysicalCartSave()
    {
        // Physical Gen 1 cart save — AllowGBCartEra should be true so GB-era events are legal.
        // This guards against regression on the ParseSettings.AllowGBCartEra flag.
        var data = File.ReadAllBytes(Path.Combine(TestFilesPath, "POKEMON RED-0.sav"));
        SaveUtil.TryGetSaveFile(data, out var saveFile, "POKEMON RED-0.sav").Should().BeTrue();
        ParseSettings.InitFromSaveFileData(saveFile!);

        var appState = new TestAppState { SaveFile = saveFile };
        var service = new AppService(appState, new TestRefreshService(), new LegalizationService());

        // Pick any non-empty party slot
        PKM? pkm = null;
        for (var i = 0; i < saveFile!.PartyCount; i++)
        {
            var candidate = saveFile.GetPartySlotAtIndex(i);
            if (candidate.Species > 0)
            {
                pkm = candidate;
                break;
            }
        }

        if (pkm is null)
        {
            // If party is empty use first non-empty box slot
            for (var box = 0; box < saveFile.BoxCount && pkm is null; box++)
            {
                for (var slot = 0; slot < saveFile.BoxSlotCount && pkm is null; slot++)
                {
                    var candidate = saveFile.GetBoxSlotAtIndex(box, slot);
                    if (candidate.Species > 0)
                    {
                        pkm = candidate;
                    }
                }
            }
        }

        // If the save is genuinely empty, skip the slot check but still verify AllowGBCartEra
        if (pkm is not null)
        {
            var la = service.GetLegalityAnalysis(pkm);
            // For a clean physical save the Pokémon should be legal
            la.Valid.Should().BeTrue("Pokémon in a clean physical Gen 1 save should be legal under GB-cart-era rules");
        }

        // AllowGBEraEvents (backed by AllowEraCartGB) must be true for a physical cartridge save —
        // PKHeX sets this via InitFromSaveFileData.
        ParseSettings.AllowGBEraEvents.Should().BeTrue(
            "InitFromSaveFileData must set AllowEraCartGB=true for physical Gen 1 carts so GB-era events remain legal");
    }

    // ── Inner test helpers (same pattern as AppServiceTests) ─────────────

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
        public string AppVersion => "Test";
        public DateTime? AppBuildDate => null;
        public int? PinnedBoxNumber { get; set; }
        public string? SaveFileName { get; set; }
        public ManicEmuSaveHelper.ManicEmuSaveContext? ManicEmuSaveContext { get; set; }
        public bool SelectedSlotsAreValid => true;
        public bool IsHaXEnabled { get; set; }
        public SpriteStyle SpriteStyle { get; set; }
        public bool ShowLegalIndicator { get; set; } = true;
        public bool ShowFishyIndicator { get; set; } = true;
        public bool ShowIllegalIndicator { get; set; } = true;
        public SaveFile? SaveFileB { get; set; }
        public string? SaveFileNameB { get; set; }
        public bool HasUnsavedChangesB { get; set; }
        public BoxEdit? BoxEditB => null;
        public int? SelectedBoxNumberB { get; set; }
        public int? SelectedBoxSlotNumberB { get; set; }
        public int? SelectedPartySlotNumberB { get; set; }
        public bool HapticsEnabled { get; set; }
    }

    private class TestRefreshService : IRefreshService
    {
        public void Refresh() { }
        public void RefreshBoxState() { }
        public void RefreshPartyState() { }
        public void RefreshBoxAndPartyState() { }
        public void RefreshTheme(bool isDarkMode) { }
        public void RefreshSystemTheme(bool systemIsDarkMode) { }
        public void ShowUpdateMessage() { }
        public void RequestJumpToPartyBox() { }
        public void RequestLoadSaveFile() { }

#pragma warning disable CS0067
        public event Action? OnAppStateChanged;
        public event Action? OnBoxStateChanged;
        public event Action? OnPartyStateChanged;
        public event Action? OnUpdateAvailable;
        public event Action<bool>? OnThemeChanged;
        public event Action<bool>? OnSystemThemeChanged;
        public event Action? OnRequestJumpToPartyBox;
        public event Action? OnRequestLoadSaveFile;
#pragma warning restore CS0067
    }
}

/// <summary>
/// Marks the "Sequential" collection so xUnit disables parallelism for all classes in it.
/// Used by <see cref="LegalityCheckerTests" /> to serialize access to <see cref="ParseSettings" /> global state.
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection
{
}
