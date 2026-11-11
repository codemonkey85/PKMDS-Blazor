using Bunit;
using MudBlazor;
using MudBlazor.Extensions;
using Pkmds.Rcl.Components.Dialogs;
using Pkmds.Rcl.Components.MainTabPages;
using Pkmds.Rcl.Components.MainTabPages.Trainer;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Pkmds.Tests;

public sealed class TrainerAuditTests
{
    [Theory]
    [InlineData(GameVersion.RD, false, false, false)]
    [InlineData(GameVersion.GD, false, false, false)]
    [InlineData(GameVersion.C, false, false, false)]
    [InlineData(GameVersion.R, false, false, false)]
    [InlineData(GameVersion.E, false, false, false)]
    [InlineData(GameVersion.FR, false, false, false)]
    [InlineData(GameVersion.COLO, false, false, false)]
    [InlineData(GameVersion.XD, false, false, false)]
    [InlineData(GameVersion.RSBOX, false, false, false)]
    [InlineData(GameVersion.BATREV, false, false, false)]
    [InlineData(GameVersion.D, true, true, false)]
    [InlineData(GameVersion.Pt, true, true, false)]
    [InlineData(GameVersion.HG, true, true, false)]
    [InlineData(GameVersion.B, true, true, false)]
    [InlineData(GameVersion.B2, true, true, false)]
    [InlineData(GameVersion.X, true, true, true)]
    [InlineData(GameVersion.AS, true, true, true)]
    [InlineData(GameVersion.ORASDEMO, true, false, true)]
    [InlineData(GameVersion.SN, true, true, true)]
    [InlineData(GameVersion.US, true, true, true)]
    [InlineData(GameVersion.GP, true, false, true)]
    [InlineData(GameVersion.SW, true, false, true)]
    [InlineData(GameVersion.BD, true, false, true)]
    [InlineData(GameVersion.PLA, true, false, true)]
    [InlineData(GameVersion.SL, true, false, true)]
    [InlineData(GameVersion.ZA, false, false, true)]
    public void TimestampControls_MatchActualFormatAndRenderingDoesNotWrite(
        GameVersion version, bool start, bool fame, bool saved)
    {
        var save = BlankSaveFile.Get(version, "TEST", LanguageID.English);
        var before = Snapshot(save);
        var edited = save.State.Edited;
        using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();

        var tab = context.Render<TrainerInfoTab>();
        tab.FindAll("#game-start").Count.Should().Be(start ? 1 : 0);
        tab.FindAll("#hall-of-fame").Count.Should().Be(fame ? 1 : 0);
        tab.FindAll("#last-saved").Count.Should().Be(saved ? 1 : 0);
        tab.FindComponents<MudCheckBox<bool>>()
            .Count(c => c.Instance.Label == "Mega Evolution Unlocked").Should().Be(save is SAV7 ? 1 : 0);
        tab.Render();

        Snapshot(save).Should().Equal(before);
        save.State.Edited.Should().Be(edited);
    }

    [Theory]
    [InlineData(GameVersion.SN)]
    [InlineData(GameVersion.MN)]
    [InlineData(GameVersion.US)]
    [InlineData(GameVersion.UM)]
    public async Task MegaEvolutionToggle_OnlyChangesUnlockBitAndPersists(GameVersion version)
    {
        var save = (SAV7)BlankSaveFile.Get(version, "TEST", LanguageID.English);
        save.MyStatus.Data[0x78] = 0xFE; // Includes Z-Move unlock and unrelated bits.
        var original = Snapshot(save);
        var appState = new TestAppState { SaveFile = save };
        var refresh = new TestRefreshService();
        using var context = BunitTestHelpers.CreateBunitContext(appState, refresh,
            new AppService(appState, refresh, new LegalizationService(appState)));
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        var toggle = tab.FindComponents<MudCheckBox<bool>>()
            .Single(c => c.Instance.Label == "Mega Evolution Unlocked");

        await tab.InvokeAsync(() => toggle.Instance.ValueChanged.InvokeAsync(true));
        save.MyStatus.Data[0x78].Should().Be(0xFF);
        save.MyStatus.ZMoveUnlocked.Should().BeTrue();
        Snapshot(save).Zip(original).Count(pair => pair.First != pair.Second).Should().Be(1);
        save.State.Edited.Should().BeTrue();
        refresh.RefreshCount.Should().BeGreaterThan(0);
        var bytes = save.Write().ToArray();
        SAV7 reloaded = save is SAV7SM ? new SAV7SM(bytes) : new SAV7USUM(bytes);
        reloaded.MyStatus.MegaUnlocked.Should().BeTrue();
        reloaded.MyStatus.ZMoveUnlocked.Should().BeTrue();

        await tab.InvokeAsync(() => toggle.Instance.ValueChanged.InvokeAsync(false));
        // Export updates checksums; compare to the original logical data after restoring those.
        save.MyStatus.Data[0x78].Should().Be(0xFE);
        var restored = save.Write().ToArray();
        reloaded = save is SAV7SM ? new SAV7SM(restored) : new SAV7USUM(restored);
        reloaded.MyStatus.MegaUnlocked.Should().BeFalse();
        reloaded.MyStatus.ZMoveUnlocked.Should().BeTrue();
    }

    [Theory]
    [InlineData(GameVersion.D)]
    [InlineData(GameVersion.B)]
    [InlineData(GameVersion.X)]
    [InlineData(GameVersion.US)]
    [InlineData(GameVersion.GP)]
    [InlineData(GameVersion.SW)]
    [InlineData(GameVersion.BD)]
    [InlineData(GameVersion.PLA)]
    [InlineData(GameVersion.SL)]
    public void GameStart_RoundTripsNativePrecisionThroughExport(GameVersion version)
    {
        var save = BlankSaveFile.Get(version, "TEST", LanguageID.English);
        var date = new DateTime(2024, 6, 7, 12, 34, 56);
        var money = save.Money;
        TrainerTimestamps.SetGameStart(save, date).Should().BeTrue();
        var expected = TrainerTimestamps.IsDateOnlyStart(save) ? date.Date : date;
        TrainerTimestamps.GetGameStart(save).Should().Be(expected);

        var exported = save.Write().ToArray();
        SaveFile reloaded = save switch
        {
            SAV4DP => new SAV4DP(exported),
            SAV5BW => new SAV5BW(exported),
            SAV6XY => new SAV6XY(exported),
            SAV7USUM => new SAV7USUM(exported),
            SAV7b => new SAV7b(exported),
            SAV8SWSH => new SAV8SWSH(exported),
            SAV8BS => new SAV8BS(exported),
            SAV8LA => new SAV8LA(exported),
            SAV9SV => new SAV9SV(exported),
            _ => throw new InvalidOperationException(),
        };
        TrainerTimestamps.GetGameStart(reloaded).Should().Be(expected);
        reloaded.Money.Should().Be(money);
    }

    [Fact]
    public void LegendsArceus_Uses1970EpochAndPreservesLastSaved()
    {
        var save = new SAV8LA();
        var start = new DateTime(2024, 1, 2, 3, 4, 5);
        save.AdventureStart.Timestamp = start;
        save.LastSaved.Timestamp = start.AddHours(1);
        var saved = save.LastSaved.Timestamp;
        TrainerTimestamps.GetGameStart(save).Should().Be(start);
        TrainerTimestamps.SetGameStart(save, start.AddDays(1)).Should().BeTrue();
        save.AdventureStart.Seconds.Should().Be((ulong)(start.AddDays(1) - new DateTime(1970, 1, 1)).TotalSeconds);
        save.LastSaved.Timestamp.Should().Be(saved);
    }

    [Fact]
    public void Bdsp_TimestampEditsPreserveFractionalTicks()
    {
        var save = new SAV8BS();
        save.System.LocalTimestampStart = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local).AddTicks(1234567);
        var date = new DateTime(2025, 6, 7, 8, 9, 10);
        TrainerTimestamps.SetGameStart(save, date).Should().BeTrue();
        save.System.LocalTimestampStart.Should().Be(DateTime.SpecifyKind(date, DateTimeKind.Local).AddTicks(1234567));
    }

    [Fact]
    public void InvalidAndUnsupportedTimestamps_DoNotWriteOrHideSupportedControls()
    {
        var za = new SAV9ZA();
        var before = Snapshot(za);
        TrainerTimestamps.GetLastSaved(za).Should().BeNull(); // Blank packed date.
        TrainerTimestamps.HasLastSaved(za).Should().BeTrue();
        TrainerTimestamps.SetGameStart(za, DateTime.Today).Should().BeFalse();
        TrainerTimestamps.SetHallOfFame(za, DateTime.Today).Should().BeFalse();
        TrainerTimestamps.SetLastSaved(za, new DateTime(1999, 1, 1)).Should().BeFalse();
        Snapshot(za).Should().Equal(before);
        var swsh = new SAV8SWSH();
        swsh.TrainerCard.StartedYear = 2024;
        swsh.TrainerCard.StartedMonth = 0;
        TrainerTimestamps.GetGameStart(swsh).Should().BeNull();
        TrainerTimestamps.HasGameStart(swsh).Should().BeTrue();
    }

    [Theory]
    [InlineData(GameVersion.X)]
    [InlineData(GameVersion.US)]
    [InlineData(GameVersion.GP)]
    [InlineData(GameVersion.SW)]
    [InlineData(GameVersion.BD)]
    [InlineData(GameVersion.PLA)]
    [InlineData(GameVersion.SL)]
    [InlineData(GameVersion.ZA)]
    public void LastSaved_RoundTripsWithSupportedPrecision(GameVersion version)
    {
        var save = BlankSaveFile.Get(version, "TEST", LanguageID.English);
        var date = new DateTime(2024, 6, 7, 12, 34, 56);
        TrainerTimestamps.SetLastSaved(save, date).Should().BeTrue();
        var expected = TrainerTimestamps.LastSavedHasSeconds(save) ? date : date.AddSeconds(-56);
        TrainerTimestamps.GetLastSaved(save).Should().Be(expected);
        TrainerTimestamps.GetLastSaved(save.Clone()).Should().Be(expected);
    }

    [Theory]
    [InlineData(GameVersion.XD)]
    [InlineData(GameVersion.BATREV)]
    public void ChangingHours_PreservesMinutesSecondsAndFractions(GameVersion version)
    {
        var save = BlankSaveFile.Get(version, "TEST", LanguageID.English);
        save.PlayedMinutes = 23;
        save.PlayedSeconds = 45;
        var offset = save is SAV4BR ? 0x388 : 0xD8;
        WriteDoubleBigEndian(save.Data[offset..], 3600 + 23 * 60 + 45.5);
        TrainerPlaytime.SetHours(save, 2);
        save.PlayedHours.Should().Be(2);
        save.PlayedMinutes.Should().Be(23);
        save.PlayedSeconds.Should().Be(45);
        ReadDoubleBigEndian(save.Data[offset..]).Should().Be(7200 + 23 * 60 + 45.5);
    }

    [Fact]
    public void Gen1_ResetClearsMaximumAndFrames()
    {
        var save = new SAV1 { PlayedHours = 255, PlayedFrames = 12 };
        TrainerPlaytime.Reset(save);
        save.PlayedMaximum.Should().BeFalse();
        save.PlayedFrames.Should().Be(0);
        save.PlayedHours.Should().Be(0);
    }

    [Fact]
    public void ZaRotation_UsesSameAxisAsGetter()
    {
        var save = new SAV9ZA();
        save.Coordinates.X = 42;
        TrainerLocation.SetRotation(save, 90);
        save.Coordinates.Rotation.Should().BeApproximately(90, 0.001);
        save.Coordinates.RY.Should().Be(0);
        save.Coordinates.X.Should().Be(42);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StorageFormats_DoNotExposePlaceholderTrainerFields(bool ranch)
    {
        SaveFile save = ranch ? new SAV4Ranch(new byte[SaveUtil.SIZE_G4RANCH]) : new SAV1Stadium();
        TrainerCapabilities.HasIdentity(save).Should().BeFalse();
        TrainerCapabilities.HasCoins(save).Should().BeFalse();
        TrainerCapabilities.HasPlaytime(save).Should().Be(ranch);
        using var context = CreateContext(save);
        var tab = context.Render<TrainerInfoTab>();
        tab.FindAll("#game-start, #hall-of-fame, #last-saved").Should().BeEmpty();
        tab.FindComponents<MudTextField<string>>().Should().BeEmpty();
        tab.FindComponents<MudNumericField<uint>>().Should().BeEmpty();
    }

    [Fact]
    public void Gen2_StereoIsDisplayedUsingStoredValueTwo()
    {
        var save = new SAV2 { Sound = 2 };
        using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        var sound = tab.FindComponents<MudSelect<int>>().Single(c => c.Instance.Label == "Sound Type");
        sound.Instance.GetState(c => c.Value).Should().Be(2);
        sound.FindComponents<MudSelectItem<int>>().Select(c => c.Instance.Value).Should().Contain(2);
    }

    [Fact]
    public void RaidDialog_DisposalRestoresOnlyRaidBlocks()
    {
        var save = new SAV9SV();
        var seed = save.RaidPaldea.CurrentSeed;
        using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var dialog = context.Render<Raid9Dialog>(p => p.Add(c => c.SaveFile, save));
        save.RaidPaldea.CurrentSeed = 12345;
        save.Money = 999;
        dialog.Instance.Dispose(); // Also runs for X/Escape dismissal.
        save.RaidPaldea.CurrentSeed.Should().Be(seed);
        save.Money.Should().Be(999);
    }

    [Fact]
    public void DonutDialog_DisposalRestoresOnlyDonutBlock()
    {
        var save = new SAV9ZA();
        var before = save.Donuts.Data.ToArray();
        using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var dialog = context.Render<Donut9Dialog>(p => p.Add(c => c.SaveFile, save));
        save.Donuts.GetDonut(0).Berry1 = 123;
        save.Money = 999;
        dialog.Instance.Dispose();
        save.Donuts.Data.ToArray().Should().Equal(before);
        save.Money.Should().Be(999);
    }

    [Fact]
    public async Task PokeMilesEdit_MirrorsCurrentAndTotalRecords()
    {
        var save = new SAV6XY();
        using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        var miles = tab.FindComponents<MudNumericField<uint>>().Single(c => c.Instance.Label == "Poké Miles");
        await tab.InvokeAsync(() => miles.Instance.ValueChanged.InvokeAsync(1234));
        save.GetRecord(63).Should().Be(1234);
        save.GetRecord(64).Should().Be(1234);
    }

    [Theory]
    [InlineData(GameVersion.StadiumJ)]
    [InlineData(GameVersion.Stadium)]
    [InlineData(GameVersion.Stadium2)]
    [InlineData(GameVersion.RSBOX)]
    public void SideStorageFormats_HaveNoPlaceholderEditors(GameVersion version)
    {
        var save = BlankSaveFile.Get(version, "TEST", LanguageID.English);
        TrainerCapabilities.HasIdentity(save).Should().BeFalse();
        TrainerCapabilities.HasGender(save).Should().BeFalse();
        TrainerCapabilities.HasCoins(save).Should().BeFalse();
        TrainerCapabilities.HasPlaytime(save).Should().BeFalse();
    }

    [Fact]
    public void BankFormats_HaveNoPlaceholderEditors()
    {
        SaveFile[] saves =
        [
            new Bank3(new byte[SaveUtil.SIZE_G4BANK]),
            new Bank4(new byte[SaveUtil.SIZE_G4BANK]),
            Bank7.GetBank7(new byte[SaveUtil.SIZE_G7BANK_1]),
            Bank7.GetBank7(new byte[SaveUtil.SIZE_G7BANK_2]),
        ];
        foreach (var save in saves)
        {
            TrainerCapabilities.HasIdentity(save).Should().BeFalse();
            TrainerCapabilities.HasPlaytime(save).Should().BeFalse();
            TrainerTimestamps.HasGameStart(save).Should().BeFalse();
            TrainerTimestamps.HasHallOfFame(save).Should().BeFalse();
        }
    }

    [Fact]
    public async Task NameSync_PreservesOtherTrainersAndDoesNotIncrementCaptureRecords()
    {
        var save = (SAV7SM)BlankSaveFile.Get(GameVersion.SN, "TEST", LanguageID.English);
        var matching = new PK7
        {
            Species = 1,
            ID32 = save.ID32,
            OriginalTrainerName = save.OT,
            OriginalTrainerGender = save.Gender,
        };
        var other = new PK7
        {
            Species = 2,
            ID32 = save.ID32 + 1,
            OriginalTrainerName = save.OT,
            OriginalTrainerGender = save.Gender,
        };
        save.SetBoxSlotAtIndex(matching, 0, 0, EntityImportSettings.None);
        save.SetBoxSlotAtIndex(other, 0, 1, EntityImportSettings.None);
        var otherBytes = save.GetBoxSlotAtIndex(0, 1).Data.ToArray();
        var captures = save.GetRecord(7);
        using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        var name = tab.FindComponents<MudTextField<string>>().Single(c => c.Instance.Label == "Name");
        await tab.InvokeAsync(() => name.Instance.ValueChanged.InvokeAsync("NEW"));
        save.GetBoxSlotAtIndex(0, 0).OriginalTrainerName.Should().Be("NEW");
        save.GetBoxSlotAtIndex(0, 1).Data.ToArray().Should().Equal(otherBytes);
        save.GetRecord(7).Should().Be(captures);
    }

    [Fact]
    public void Bdsp_RenderPreservesSubsecondTimestampData()
    {
        var save = new SAV8BS();
        save.System.LocalTimestampStart = new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Local).AddTicks(12345);
        var before = Snapshot(save);
        using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        tab.Render();
        Snapshot(save).Should().Equal(before);
    }

    [Fact]
    public async Task BattleRevolution_TrainerEditsTargetOnlyActiveProfile()
    {
        var (save, state, refresh, service) = BunitTestHelpers.LoadSave("PbrSaveData");
        var br = (SAV4BR)save;
        br.CurrentSlot = 0;
        var originalName = br.CurrentOT;
        var originalId = br.TID16;
        br.CurrentSlot = 1;
        using var context = BunitTestHelpers.CreateBunitContext(state, refresh, service);
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        var name = tab.FindComponents<MudTextField<string>>().Single(c => c.Instance.Label == "Name");
        await tab.InvokeAsync(() => name.Instance.ValueChanged.InvokeAsync("TEST"));
        var id = tab.FindComponents<MudNumericField<ushort>>().Single(c => c.Instance.Label == "ID");
        await tab.InvokeAsync(() => id.Instance.ValueChanged.InvokeAsync(42));

        var reloaded = new SAV4BR(br.Write().ToArray());
        reloaded.CurrentSlot = 1;
        reloaded.CurrentOT.Should().Be("TEST");
        reloaded.TID16.Should().Be(42);
        reloaded.CurrentSlot = 0;
        reloaded.CurrentOT.Should().Be(originalName);
        reloaded.TID16.Should().Be(originalId);
    }

    private static BunitContext CreateContext(SaveFile save)
    {
        var state = new TestAppState { SaveFile = save };
        var refresh = new TestRefreshService();
        return BunitTestHelpers.CreateBunitContext(state, refresh,
            new AppService(state, refresh, new LegalizationService(state)));
    }

    private static byte[] Snapshot(SaveFile save) => save is ISCBlockArray blocks
        ? blocks.AllBlocks.SelectMany(b => new[] { (byte)b.Type }.Concat(b.Raw.ToArray())).ToArray()
        : save.Data.ToArray();
}
