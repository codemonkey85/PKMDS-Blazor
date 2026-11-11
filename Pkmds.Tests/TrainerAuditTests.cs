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
    public async Task TimestampControls_MatchActualFormatAndRenderingDoesNotWrite(
        GameVersion version, bool start, bool fame, bool saved)
    {
        var save = CreateSave(version);
        var before = Snapshot(save);
        var edited = save.State.Edited;
        await using var context = CreateContext(save);
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
        await using var context = BunitTestHelpers.CreateBunitContext(appState, refresh,
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
    [InlineData("Manaphy Pearl.sav")]
    [InlineData("Black - Full Completion.sav")]
    [InlineData("x.sav")]
    [InlineData("ultra sun.sav")]
    [InlineData("Lets-Go-Pikachu-All-Pokemon.bin")]
    [InlineData("Test-Save-Shield.sav")]
    [InlineData("bdsp.bin")]
    [InlineData("legends-arceus")]
    [InlineData("Test-Save-Scarlet.sav")]
    public void GameStart_RoundTripsNativePrecisionThroughExport(string fileName)
    {
        // Blank Core saves may lack the backing buffers or typed blocks required for export.
        var (save, _, _, _) = BunitTestHelpers.LoadSave(fileName);
        var date = new DateTime(2024, 6, 7, 12, 34, 56);
        var money = save.Money;
        var fractionalTicks = save is SAV8BS bdsp
            ? bdsp.System.LocalTimestampStart.Ticks % TimeSpan.TicksPerSecond : 0;
        TrainerTimestamps.SetGameStart(save, date).Should().BeTrue();
        var expected = TrainerTimestamps.IsDateOnlyStart(save) ? date.Date : date.AddTicks(fractionalTicks);
        TrainerTimestamps.GetGameStart(save).Should().Be(expected);

        var exported = save.Write().ToArray();
        SaveUtil.TryGetSaveFile(exported, out var reloaded, fileName).Should().BeTrue();
        reloaded.Should().BeOfType(save.GetType());
        TrainerTimestamps.GetGameStart(reloaded!).Should().Be(expected);
        reloaded!.Money.Should().Be(money);
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
        var save = CreateSave(version);
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
        var save = CreateSave(version);
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
    public async Task StorageFormats_DoNotExposePlaceholderTrainerFields(bool ranch)
    {
        SaveFile save = ranch ? new SAV4Ranch(new byte[SaveUtil.SIZE_G4RANCH]) : new SAV1Stadium();
        TrainerCapabilities.HasIdentity(save).Should().BeFalse();
        TrainerCapabilities.HasCoins(save).Should().BeFalse();
        TrainerCapabilities.HasPlaytime(save).Should().Be(ranch);
        await using var context = CreateContext(save);
        var tab = context.Render<TrainerInfoTab>();
        tab.FindAll("#game-start, #hall-of-fame, #last-saved").Should().BeEmpty();
        tab.FindComponents<MudTextField<string>>().Should().BeEmpty();
        tab.FindComponents<MudNumericField<uint>>().Should().BeEmpty();
    }

    [Fact]
    public async Task Gen2_StereoIsDisplayedUsingStoredValueTwo()
    {
        var save = new SAV2 { Sound = 2 };
        await using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        var sound = tab.FindComponents<MudSelect<int>>().Single(c => c.Instance.Label == "Sound Type");
        sound.Instance.GetState(c => c.Value).Should().Be(2);
        sound.FindComponents<MudSelectItem<int>>().Select(c => c.Instance.Value).Should().Contain(2);
    }

    [Fact]
    public async Task RaidDialog_DisposalRestoresOnlyRaidBlocks()
    {
        var save = new SAV9SV();
        var seed = save.RaidPaldea.CurrentSeed;
        await using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var dialog = context.Render<Raid9Dialog>(p => p.Add(c => c.SaveFile, save));
        save.RaidPaldea.CurrentSeed = 12345;
        save.ID32 = 999;
        dialog.Instance.Dispose(); // Also runs for X/Escape dismissal.
        save.RaidPaldea.CurrentSeed.Should().Be(seed);
        save.ID32.Should().Be(999);
    }

    [Fact]
    public async Task DonutDialog_DisposalRestoresOnlyDonutBlock()
    {
        var save = new SAV9ZA();
        var before = save.Donuts.Data.ToArray();
        await using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var dialog = context.Render<Donut9Dialog>(p => p.Add(c => c.SaveFile, save));
        save.Donuts.GetDonut(0).Berry1 = 123;
        save.ID32 = 999;
        dialog.Instance.Dispose();
        save.Donuts.Data.ToArray().Should().Equal(before);
        save.ID32.Should().Be(999);
    }

    [Fact]
    public async Task PokeMilesEdit_MirrorsCurrentAndTotalRecords()
    {
        var save = new SAV6XY();
        await using var context = CreateContext(save);
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
        var save = CreateSave(version);
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
            CreateBank(BankRevision.Gen6),
            CreateBank(BankRevision.Gen7),
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
        await using var context = CreateContext(save);
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        var name = tab.FindComponents<MudTextField<string>>().Single(c => c.Instance.Label == "Name");
        await tab.InvokeAsync(() => name.Instance.ValueChanged.InvokeAsync("NEW"));
        save.GetBoxSlotAtIndex(0, 0).OriginalTrainerName.Should().Be("NEW");
        save.GetBoxSlotAtIndex(0, 1).Data.ToArray().Should().Equal(otherBytes);
        save.GetRecord(7).Should().Be(captures);
    }

    [Fact]
    public async Task Bdsp_RenderPreservesSubsecondTimestampData()
    {
        var save = new SAV8BS();
        save.System.LocalTimestampStart = new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Local).AddTicks(12345);
        var before = Snapshot(save);
        await using var context = CreateContext(save);
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
        await using var context = BunitTestHelpers.CreateBunitContext(state, refresh, service);
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

    [Theory]
    [InlineData(GameVersion.D, "Language", 3)]
    [InlineData(GameVersion.D, "Country", 1)]
    [InlineData(GameVersion.D, "Region", 1)]
    [InlineData(GameVersion.B, "Country", 1)]
    [InlineData(GameVersion.B, "Region", 1)]
    [InlineData(GameVersion.X, "Country", 1)]
    [InlineData(GameVersion.X, "Region", 1)]
    [InlineData(GameVersion.X, "3DS Region", 0)]
    [InlineData(GameVersion.SN, "Language", 3)]
    [InlineData(GameVersion.SN, "Country", 1)]
    [InlineData(GameVersion.SN, "Region", 1)]
    [InlineData(GameVersion.SN, "3DS Region", 0)]
    [InlineData(GameVersion.RD, "Battle Style", 0)]
    [InlineData(GameVersion.RD, "Text Speed", 3)]
    [InlineData(GameVersion.GD, "Battle Style", 0)]
    [InlineData(GameVersion.GD, "Text Speed", 3)]
    [InlineData(GameVersion.GD, "Sound Type", 2)]
    public async Task SelectCallback_MarksSaveEditedAndRefreshesWithoutDomChange(
        GameVersion version, string label, int value)
    {
        var save = CreateSave(version);
        var state = new TestAppState { SaveFile = save };
        var refresh = new TestRefreshService();
        await using var context = BunitTestHelpers.CreateBunitContext(state, refresh,
            new AppService(state, refresh, new LegalizationService(state)));
        context.Render<MudPopoverProvider>();
        var tab = context.Render<TrainerInfoTab>();
        save.State.Edited = false;
        var before = Snapshot(save);
        var refreshes = refresh.RefreshCount;

        // Invoke only the component callback: MudSelect emits no native DOM change event.
        if (label == "Battle Style")
        {
            var select = tab.FindComponents<MudSelect<bool>>().Single(c => c.Instance.Label == label);
            await tab.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(value != 0));
        }
        else if (save is IRegionOrigin && label != "Language")
        {
            var select = tab.FindComponents<MudSelect<byte>>().Single(c => c.Instance.Label == label);
            await tab.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync((byte)value));
        }
        else
        {
            var select = tab.FindComponents<MudSelect<int>>().Single(c => c.Instance.Label == label);
            await tab.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(value));
        }

        Snapshot(save).Should().NotEqual(before);
        save.State.Edited.Should().BeTrue();
        refresh.RefreshCount.Should().BeGreaterThan(refreshes);
    }

    [Fact]
    public async Task ThrowStyleCallback_MarksSaveEditedAndRefreshesWithoutDomChange()
    {
        var (loaded, _, _, _) = BunitTestHelpers.LoadSave("violet_main");
        var save = (SAV9SV)loaded;
        save.SaveRevision.Should().BeGreaterThanOrEqualTo(2);
        save.ThrowStyle = ThrowStyle9.OriginalStyle;
        var state = new TestAppState { SaveFile = save };
        var refresh = new TestRefreshService();
        await using var context = BunitTestHelpers.CreateBunitContext(state, refresh,
            new AppService(state, refresh, new LegalizationService(state)));
        context.Render<MudPopoverProvider>();
        var section = context.Render<TrainerInfoSav9SvSection>(p => p.Add(c => c.SaveFile, save));
        save.State.Edited = false;
        var refreshes = refresh.RefreshCount;
        var select = section.FindComponents<MudSelect<ThrowStyle9>>()
            .Single(c => c.Instance.Label == "Throw Style");

        await section.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(ThrowStyle9.NinjaStyle));

        save.ThrowStyle.Should().Be(ThrowStyle9.NinjaStyle);
        save.State.Edited.Should().BeTrue();
        refresh.RefreshCount.Should().BeGreaterThan(refreshes);
    }

    private static SaveFile CreateSave(GameVersion version) => version switch
    {
        // Core's modern blank blocks have no scalar types; use real fixtures when rendering.
        GameVersion.PLA => BunitTestHelpers.LoadSave("legends-arceus").SaveFile,
        GameVersion.SL => BunitTestHelpers.LoadSave("Test-Save-Scarlet.sav").SaveFile,
        GameVersion.ZA => BunitTestHelpers.LoadSave("legends-za").SaveFile,
        // Core 26.10.18 does not map these GameVersion values to a SaveFileType.
        // Its blank Colosseum constructor also throws while setting the region.
        GameVersion.COLO => BunitTestHelpers.LoadSave("01-GC6J-pokemon_colosseum.gci").SaveFile,
        GameVersion.XD => new SAV3XD(),
        GameVersion.RSBOX => new SAV3RSBox(),
        GameVersion.ORASDEMO => new SAV6AODemo(),
        // The blank StadiumJ constructor fails inside ClearBoxes; capability checks need only a buffer.
        GameVersion.StadiumJ => new SAV1StadiumJ(new byte[SaveUtil.SIZE_G1STAD]),
        GameVersion.Stadium => new SAV1Stadium(),
        GameVersion.Stadium2 => new SAV2Stadium(),
        _ => BlankSaveFile.Get(version, "TEST", LanguageID.English),
    };

    private static Bank7 CreateBank(BankRevision revision)
    {
        var size = revision == BankRevision.Gen6 ? SaveUtil.SIZE_G7BANK_1 : SaveUtil.SIZE_G7BANK_2;
        var data = new byte[size];
        WriteUInt16LittleEndian(data.AsSpan(0x15C), (ushort)revision);
        WriteUInt16LittleEndian(data.AsSpan(0x15E), Bank7.FixedBoxCount);
        Bank7.IsBank(data).Should().BeTrue();
        return Bank7.GetBank7(data);
    }

    private static BunitContext CreateContext(SaveFile save)
    {
        var state = new TestAppState { SaveFile = save };
        var refresh = new TestRefreshService();
        return BunitTestHelpers.CreateBunitContext(state, refresh,
            new AppService(state, refresh, new LegalizationService(state)));
    }

    private static byte[] Snapshot(SaveFile save) => save switch
    {
        ISCBlockArray blocks => blocks.AllBlocks.SelectMany(b => new[] { (byte)b.Type }.Concat(b.Raw.ToArray())).ToArray(),
        // Blank Gen 4 saves keep trainer data in a separate general buffer.
        SAV4 gen4 => [.. gen4.Data, .. gen4.General],
        _ => save.Data.ToArray(),
    };
}
