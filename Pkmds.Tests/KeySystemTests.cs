using MudBlazor.Extensions;

namespace Pkmds.Tests;

public sealed class KeySystemTests
{
    [Theory]
    [InlineData(GameVersion.B, false)]
    [InlineData(GameVersion.W, false)]
    [InlineData(GameVersion.B2, true)]
    [InlineData(GameVersion.W2, true)]
    public async Task TrainerTab_OnlyShowsKeySystemForSequels_AndRenderingDoesNotEdit(GameVersion version, bool visible)
    {
        var save = BlankSaveFile.Get(version, "TEST", LanguageID.English);
        var before = save.Data.ToArray();
        var edited = save.State.Edited;
        await using var context = CreateContext(save, new TestRefreshService());
        context.Render<MudPopoverProvider>();

        var tab = context.Render<TrainerInfoTab>();

        tab.FindComponents<TrainerInfoSav5B2W2Section>().Count.Should().Be(visible ? 1 : 0);
        if (visible)
        {
            var section = tab.FindComponent<TrainerInfoSav5B2W2Section>();
            section.FindComponents<MudCheckBox<bool>>().Count.Should().Be(10);
            section.Markup.Should().NotContain("Battle Subway BP").And.NotContain("Money");
        }
        save.Data.ToArray().Should().Equal(before);
        save.State.Edited.Should().Be(edited);
    }

    [Theory]
    [InlineData(GameVersion.B2, 0u, false)]
    [InlineData(GameVersion.W2, 0u, true)]
    [InlineData(GameVersion.B2, 0x12345678u, true)]
    [InlineData(GameVersion.W2, 0x12345678u, false)]
    public async Task UnlockChallengeMode_PreservesOtherKeys_AndDifficultyRoundTrips(
        GameVersion version, uint crypto, bool alreadyObtained)
    {
        var save = (SAV5B2W2)BlankSaveFile.Get(version, "TEST", LanguageID.English);
        save.Keys.Crypto = crypto;
        save.Keys.SetIsKeyObtained(KeyType5.Challenge, alreadyObtained);
        save.Keys.SetIsKeyObtained(KeyType5.Iron, true);
        save.Keys.SetIsKeyUnlocked(KeyType5.Iceberg, true);
        save.Keys.ActiveCity = City5.WhiteForest;
        save.Keys.ActiveChamber = Chamber5.Iron;
        var before = save.Data.ToArray();
        var refresh = new TestRefreshService();
        await using var context = CreateContext(save, refresh);
        context.Render<MudPopoverProvider>();
        var section = context.Render<TrainerInfoSav5B2W2Section>(p => p.Add(c => c.SaveFile, save));
        save.State.Edited = false;

        await SetFlag(section, "Challenge Key obtained", true);
        await SetFlag(section, "Challenge Mode unlocked", true);

        save.Keys.GetIsKeyObtained(KeyType5.Challenge).Should().BeTrue();
        save.Keys.GetIsKeyUnlocked(KeyType5.Challenge).Should().BeTrue();
        save.Keys.ActiveDifficulty.Should().Be(Difficulty5.Normal);
        save.State.Edited.Should().BeTrue();
        refresh.RefreshCount.Should().BeGreaterThan(0);
        // Unlocking writes just the obtained/unlocked Challenge Key uints.
        var expected = (SAV5B2W2)save.Clone();
        expected.Keys.SetIsKeyObtained(KeyType5.Challenge, alreadyObtained);
        expected.Keys.SetIsKeyUnlocked(KeyType5.Challenge, false);
        expected.Data.ToArray().Should().Equal(before);

        var select = section.FindComponent<MudSelect<Difficulty5>>();
        await section.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(Difficulty5.Challenge));

        var reloaded = new SAV5B2W2(save.Write().ToArray());
        reloaded.ChecksumsValid.Should().BeTrue();
        reloaded.Keys.ActiveDifficulty.Should().Be(Difficulty5.Challenge);
        reloaded.Keys.GetIsKeyObtained(KeyType5.Challenge).Should().BeTrue();
        reloaded.Keys.GetIsKeyUnlocked(KeyType5.Challenge).Should().BeTrue();
        reloaded.Keys.Crypto.Should().Be(crypto);
        reloaded.Keys.GetIsKeyObtained(KeyType5.Iron).Should().BeTrue();
        reloaded.Keys.GetIsKeyUnlocked(KeyType5.Iceberg).Should().BeTrue();
        reloaded.Keys.ActiveCity.Should().Be(City5.WhiteForest);
        reloaded.Keys.ActiveChamber.Should().Be(Chamber5.Iron);

        await section.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(Difficulty5.Normal));
        new SAV5B2W2(save.Write().ToArray()).Keys.ActiveDifficulty.Should().Be(Difficulty5.Normal);
        save.Keys.GetIsKeyUnlocked(KeyType5.Challenge).Should().BeTrue();
    }

    [Theory]
    [InlineData(GameVersion.B2)]
    [InlineData(GameVersion.W2)]
    public async Task ReceivedChallengeKey_CanBeSelectedWithoutChangingObtainedStatus(GameVersion version)
    {
        var save = (SAV5B2W2)BlankSaveFile.Get(version, "TEST", LanguageID.English);
        // WinForms deliberately exposes these flags independently. An unlocked
        // mode does not imply that this save has obtained its own shareable key.
        save.Keys.SetIsKeyUnlocked(KeyType5.Challenge, true);
        var before = save.Data.ToArray();
        await using var context = CreateContext(save, new TestRefreshService());
        context.Render<MudPopoverProvider>();
        save.State.Edited = false;
        var section = context.Render<TrainerInfoSav5B2W2Section>(p => p.Add(c => c.SaveFile, save));

        section.FindComponents<MudCheckBox<bool>>()
            .Single(c => c.Instance.AriaLabel == "Challenge Key obtained")
            .Instance.GetState(x => x.Value).Should().BeFalse();
        save.Data.ToArray().Should().Equal(before);
        save.State.Edited.Should().BeFalse();

        var select = section.FindComponent<MudSelect<Difficulty5>>();
        await section.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(Difficulty5.Challenge));

        save.Keys.ActiveDifficulty.Should().Be(Difficulty5.Challenge);
        save.Keys.GetIsKeyObtained(KeyType5.Challenge).Should().BeFalse();
        save.Keys.GetIsKeyUnlocked(KeyType5.Challenge).Should().BeTrue();
    }

    [Fact]
    public async Task ExistingEasyMode_RemainsAvailable_AndRenderingPreservesItsSelection()
    {
        var save = (SAV5B2W2)BlankSaveFile.Get(GameVersion.W2, "TEST", LanguageID.English);
        save.Keys.SetIsKeyUnlocked(KeyType5.Easy, true);
        save.Keys.ActiveDifficulty = Difficulty5.Easy;
        var before = save.Data.ToArray();
        await using var context = CreateContext(save, new TestRefreshService());
        context.Render<MudPopoverProvider>();
        var section = context.Render<TrainerInfoSav5B2W2Section>(p => p.Add(c => c.SaveFile, save));

        section.FindComponent<MudSelect<Difficulty5>>().Instance.GetState(x => x.Value).Should().Be(Difficulty5.Easy);
        save.Data.ToArray().Should().Equal(before);
        section.Find("button").Click();
        save.Keys.ActiveDifficulty.Should().Be(Difficulty5.Easy);

        var select = section.FindComponent<MudSelect<Difficulty5>>();
        await section.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(Difficulty5.Normal));
        await section.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(Difficulty5.Easy));
        new SAV5B2W2(save.Write().ToArray()).Keys.ActiveDifficulty.Should().Be(Difficulty5.Easy);
    }

    [Theory]
    [InlineData(Difficulty5.Easy)]
    [InlineData(Difficulty5.Challenge)]
    [InlineData((Difficulty5)99)]
    public async Task Difficulty_RejectsLockedOrInvalidSelections(Difficulty5 difficulty)
    {
        var save = (SAV5B2W2)BlankSaveFile.Get(GameVersion.B2, "TEST", LanguageID.English);
        await using var context = CreateContext(save, new TestRefreshService());
        context.Render<MudPopoverProvider>();
        var section = context.Render<TrainerInfoSav5B2W2Section>(p => p.Add(c => c.SaveFile, save));
        var before = save.Data.ToArray();
        save.State.Edited = false;

        var select = section.FindComponent<MudSelect<Difficulty5>>();
        await section.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(difficulty));

        save.Data.ToArray().Should().Equal(before);
        save.State.Edited.Should().BeFalse();
    }

    private static BunitContext CreateContext(SaveFile save, TestRefreshService refresh)
    {
        var state = new TestAppState { SaveFile = save };
        return BunitTestHelpers.CreateBunitContext(state, refresh,
            new AppService(state, refresh, new LegalizationService(state)));
    }

    [Theory]
    [InlineData(KeyType5.Easy, "Easy Key obtained", "Easy Mode unlocked")]
    [InlineData(KeyType5.Challenge, "Challenge Key obtained", "Challenge Mode unlocked")]
    [InlineData(KeyType5.City, "City Key obtained", "Black City / White Forest unlocked")]
    [InlineData(KeyType5.Iron, "Iron Key obtained", "Iron Chamber unlocked")]
    [InlineData(KeyType5.Iceberg, "Iceberg Key obtained", "Iceberg Chamber unlocked")]
    public async Task EveryKey_HasIndependentReversibleFlags(KeyType5 key, string obtainedLabel, string unlockedLabel)
    {
        foreach (var version in new[] { GameVersion.B2, GameVersion.W2 })
        {
            var save = (SAV5B2W2)BlankSaveFile.Get(version, "TEST", LanguageID.English);
            save.Keys.Crypto = 0x12345678;
            save.Keys.ActiveDifficulty = Difficulty5.Challenge;
            save.Keys.ActiveCity = City5.WhiteForest;
            save.Keys.ActiveChamber = Chamber5.Iron;
            var before = save.Data.ToArray();
            var refresh = new TestRefreshService();
            await using var context = CreateContext(save, refresh);
            context.Render<MudPopoverProvider>();
            var section = context.Render<TrainerInfoSav5B2W2Section>(p => p.Add(c => c.SaveFile, save));
            save.State.Edited = false;

            await SetFlag(section, obtainedLabel, true);
            save.Keys.GetIsKeyObtained(key).Should().BeTrue();
            save.Keys.GetIsKeyUnlocked(key).Should().BeFalse();
            await SetFlag(section, unlockedLabel, true);
            await SetFlag(section, obtainedLabel, false);
            save.Keys.GetIsKeyObtained(key).Should().BeFalse();
            save.Keys.GetIsKeyUnlocked(key).Should().BeTrue();
            save.State.Edited.Should().BeTrue();
            refresh.RefreshCount.Should().BeGreaterThan(0);

            // Undo just the expected edit on a copy to detect unrelated writes.
            var expected = (SAV5B2W2)save.Clone();
            expected.Keys.SetIsKeyUnlocked(key, false);
            expected.Data.ToArray().Should().Equal(before);
            var reloaded = new SAV5B2W2(save.Write().ToArray());
            reloaded.ChecksumsValid.Should().BeTrue();
            reloaded.Keys.GetIsKeyObtained(key).Should().BeFalse();
            reloaded.Keys.GetIsKeyUnlocked(key).Should().BeTrue();
            await SetFlag(section, unlockedLabel, false);
            save.Keys.ActiveDifficulty.Should().Be(Difficulty5.Challenge);
            save.Keys.ActiveCity.Should().Be(City5.WhiteForest);
            save.Keys.ActiveChamber.Should().Be(Chamber5.Iron);
            save.Keys.Crypto.Should().Be(0x12345678);
            save.Keys.GetIsKeyUnlocked(key).Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(GameVersion.B2)]
    [InlineData(GameVersion.W2)]
    public async Task UnlockAll_MatchesWinFormsCheckAll_WithoutChangingActiveSelections(GameVersion version)
    {
        var save = (SAV5B2W2)BlankSaveFile.Get(version, "TEST", LanguageID.English);
        save.Keys.Crypto = 0x12345678;
        save.Keys.ActiveDifficulty = Difficulty5.Easy;
        save.Keys.ActiveCity = City5.WhiteForest;
        save.Keys.ActiveChamber = Chamber5.Iceberg;
        save.Keys.SetIsKeyObtained(KeyType5.Iron, true);
        var before = (SAV5B2W2)save.Clone();
        await using var context = CreateContext(save, new TestRefreshService());
        context.Render<MudPopoverProvider>();
        var section = context.Render<TrainerInfoSav5B2W2Section>(p => p.Add(c => c.SaveFile, save));

        section.Find("button").Click();

        section.Find("button").HasAttribute("disabled").Should().BeTrue();
        var expected = (SAV5B2W2)save.Clone();
        foreach (var key in Enum.GetValues<KeyType5>())
        {
            save.Keys.GetIsKeyObtained(key).Should().BeTrue();
            save.Keys.GetIsKeyUnlocked(key).Should().BeTrue();
            expected.Keys.SetIsKeyObtained(key, before.Keys.GetIsKeyObtained(key));
            expected.Keys.SetIsKeyUnlocked(key, before.Keys.GetIsKeyUnlocked(key));
        }

        expected.Data.ToArray().Should().Equal(before.Data.ToArray());
        var reloaded = new SAV5B2W2(save.Write().ToArray());
        reloaded.ChecksumsValid.Should().BeTrue();
        foreach (var key in Enum.GetValues<KeyType5>())
        {
            reloaded.Keys.GetIsKeyObtained(key).Should().BeTrue();
            reloaded.Keys.GetIsKeyUnlocked(key).Should().BeTrue();
        }
        reloaded.Keys.ActiveDifficulty.Should().Be(Difficulty5.Easy);
        reloaded.Keys.ActiveCity.Should().Be(City5.WhiteForest);
        reloaded.Keys.ActiveChamber.Should().Be(Chamber5.Iceberg);
        reloaded.Keys.Crypto.Should().Be(0x12345678);
    }

    private static Task SetFlag(IRenderedComponent<TrainerInfoSav5B2W2Section> section, string label, bool value)
    {
        var toggle = section.FindComponents<MudCheckBox<bool>>().Single(c => c.Instance.AriaLabel == label);
        return section.InvokeAsync(() => toggle.Instance.ValueChanged.InvokeAsync(value));
    }
}
