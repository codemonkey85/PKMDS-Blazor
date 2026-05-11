namespace Pkmds.Rcl.Components.MainTabPages;

public partial class RecordsTab
{
    // Trainer-card star thresholds (Pokémon Jump / Dodrio Berry Picking) require 200 points;
    // PKHeX caps the underlying field at 99,990. See ISaveBlock3SmallExpansion.
    private const uint JoyfulScoreMax = 99_990;
    private const ushort JoyfulCounterMax = 9_999;
    private const uint BerryPowderMax = 99_999;

    [Parameter]
    [EditorRequired]
    public SAV3? SaveFile { get; set; }

    private int CurrentRecordIndex { get; set; }

    private uint? CurrentRecordValue { get; set; }

    private IList<ComboItem> RecordComboItems { get; set; } = [];

    private uint HallOfFameHours { get; set; }

    private byte HallOfFameMinutes { get; set; }

    private byte HallOfFameSeconds { get; set; }

    private ISaveBlock3SmallExpansion? JoyfulBlock { get; set; }

    private bool HallOfFameIndexSelected => (SaveFile, CurrentRecordIndex) switch
    {
        (SAV3RS, (int)RecID3RuSa.FIRST_HOF_PLAY_TIME) => true,
        (SAV3E, (int)RecID3Emerald.FIRST_HOF_PLAY_TIME) => true,
        (SAV3FRLG, (int)RecID3FRLG.FIRST_HOF_PLAY_TIME) => true,
        _ => false
    };

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        LoadRecords();
    }

    private void LoadRecords()
    {
        if (SaveFile is null)
        {
            JoyfulBlock = null;
            return;
        }

        RecordComboItems = Record3.GetItems(SaveFile);
        JoyfulBlock = SaveFile.SmallBlock as ISaveBlock3SmallExpansion;
        GetRecord();
    }

    private uint JoyfulJumpScore
    {
        get => JoyfulBlock?.JoyfulJumpScore ?? 0U;
        set
        {
            if (JoyfulBlock is not null)
            {
                JoyfulBlock.JoyfulJumpScore = Math.Min(JoyfulScoreMax, value);
            }
        }
    }

    private ushort JoyfulJumpInRow
    {
        get => JoyfulBlock?.JoyfulJumpInRow ?? (ushort)0;
        set
        {
            if (JoyfulBlock is not null)
            {
                JoyfulBlock.JoyfulJumpInRow = Math.Min(JoyfulCounterMax, value);
            }
        }
    }

    private ushort JoyfulJump5InRow
    {
        get => JoyfulBlock?.JoyfulJump5InRow ?? (ushort)0;
        set
        {
            if (JoyfulBlock is not null)
            {
                JoyfulBlock.JoyfulJump5InRow = Math.Min(JoyfulCounterMax, value);
            }
        }
    }

    private ushort JoyfulJumpGamesMaxPlayers
    {
        get => JoyfulBlock?.JoyfulJumpGamesMaxPlayers ?? (ushort)0;
        set
        {
            if (JoyfulBlock is not null)
            {
                JoyfulBlock.JoyfulJumpGamesMaxPlayers = Math.Min(JoyfulCounterMax, value);
            }
        }
    }

    private uint JoyfulBerriesScore
    {
        get => JoyfulBlock?.JoyfulBerriesScore ?? 0U;
        set
        {
            if (JoyfulBlock is not null)
            {
                JoyfulBlock.JoyfulBerriesScore = Math.Min(JoyfulScoreMax, value);
            }
        }
    }

    private ushort JoyfulBerriesInRow
    {
        get => JoyfulBlock?.JoyfulBerriesInRow ?? (ushort)0;
        set
        {
            if (JoyfulBlock is not null)
            {
                JoyfulBlock.JoyfulBerriesInRow = Math.Min(JoyfulCounterMax, value);
            }
        }
    }

    private ushort JoyfulBerries5InRow
    {
        get => JoyfulBlock?.JoyfulBerries5InRow ?? (ushort)0;
        set
        {
            if (JoyfulBlock is not null)
            {
                JoyfulBlock.JoyfulBerries5InRow = Math.Min(JoyfulCounterMax, value);
            }
        }
    }

    private uint BerryPowder
    {
        get => JoyfulBlock?.BerryPowder ?? 0U;
        set
        {
            if (JoyfulBlock is not null)
            {
                JoyfulBlock.BerryPowder = Math.Min(BerryPowderMax, value);
            }
        }
    }

    private void GetRecord()
    {
        if (SaveFile is null)
        {
            return;
        }

        CurrentRecordValue = SaveFile.GetRecord(CurrentRecordIndex);

        if (HallOfFameIndexSelected)
        {
            SetFameTime(CurrentRecordValue ?? 0U);
        }
    }

    private void SetCurrentRecordValue(uint? newValue)
    {
        if (SaveFile is null)
        {
            return;
        }

        CurrentRecordValue = newValue;
        SaveFile.SetRecord(CurrentRecordIndex, newValue ?? 0U);

        if (HallOfFameIndexSelected)
        {
            SetFameTime(newValue ?? 0U);
        }
    }

    private void ChangeFame()
    {
        if (!HallOfFameIndexSelected || SaveFile is null)
        {
            return;
        }

        SaveFile.SetRecord(1, (uint)(CurrentRecordValue = GetFameTime()));
    }

    private uint GetFameTime()
    {
        if (!HallOfFameIndexSelected || SaveFile is null)
        {
            return 0U;
        }

        var hrs = Math.Min(9999U, HallOfFameHours);
        var min = Math.Min((byte)59, HallOfFameMinutes);
        var sec = Math.Min((byte)59, HallOfFameSeconds);

        return hrs << 16 | (uint)min << 8 | sec;
    }

    private void SetFameTime(uint time)
    {
        if (!HallOfFameIndexSelected || SaveFile is null)
        {
            return;
        }

        HallOfFameHours = Math.Min(9999U, time >> 16);
        HallOfFameMinutes = Math.Min((byte)59, (byte)(time >> 8));
        HallOfFameSeconds = Math.Min((byte)59, (byte)time);
    }
}
