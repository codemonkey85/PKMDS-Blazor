namespace Pkmds.Core.Utilities;

/// <summary>Format-specific Trainer timestamps; reading never initializes or rewrites a save.</summary>
public static class TrainerTimestamps
{
    public static bool HasGameStart(SaveFile save) => save is
        SAV4 or SAV5 or SAV6 or SAV7 or SAV7b or SAV8SWSH or SAV8BS or SAV8LA or SAV9SV;

    public static bool HasHallOfFame(SaveFile save) => save is SAV4 or SAV5 or SAV6XY or SAV6AO or SAV7;

    public static bool HasLastSaved(SaveFile save) => save is
        SAV6 or SAV7 or SAV7b or SAV8SWSH or SAV8BS or SAV8LA or SAV9SV or SAV9ZA;

    public static bool IsDateOnlyStart(SaveFile save) => save is SAV8SWSH or SAV9SV;

    public static bool LastSavedHasSeconds(SaveFile save) => save switch
    {
        SAV8BS => true,
        SAV8LA la => la.LastSaved.HasSeconds,
        SAV9SV sv => sv.LastSaved.HasSeconds,
        SAV9ZA za => za.LastSaved.HasSeconds,
        _ => false,
    };

    public static DateTime? GetGameStart(SaveFile save) => ReadDate(() => save switch
    {
        SAV4 or SAV5 or SAV6 or SAV7 => From2000(save.SecondsToStart),
        SAV7b lgpe => lgpe.PlayerGeoLocation.AdventureBegin.Timestamp,
        SAV8SWSH swsh => GetCardStart(swsh.TrainerCard),
        SAV8BS bs => bs.System.LocalTimestampStart,
        // AdventureStart is an Epoch1970Value, unlike the Gen 4-7 counters.
        SAV8LA la => la.AdventureStart.Timestamp,
        SAV9SV sv => sv.EnrollmentDate.Timestamp,
        // ZA's EnrollmentDate block is explicitly unused in PKHeX.Core 26.10.18.
        _ => null,
    });

    public static DateTime? GetHallOfFame(SaveFile save) =>
        HasHallOfFame(save) && save.SecondsToFame != 0 ? ReadDate(() => From2000(save.SecondsToFame)) : null;

    public static DateTime? GetLastSaved(SaveFile save) => ReadDate(() => save switch
    {
        SAV6 s => s.Played.LastSavedDate,
        SAV7 s => s.Played.LastSavedDate,
        SAV7b s => s.Played.LastSavedDate,
        SAV8SWSH s => s.Played.LastSavedDate,
        SAV8BS s => s.System.LocalTimestampLatest,
        SAV8LA s => s.LastSaved.Timestamp,
        SAV9SV s => s.LastSaved.Timestamp,
        SAV9ZA s => s.LastSaved.Timestamp,
        _ => null,
    });

    public static bool SetGameStart(SaveFile save, DateTime? value)
    {
        if (value is not { } date || !HasGameStart(save) || !IsValid(date))
            return false;

        switch (save)
        {
            case SAV4 or SAV5 or SAV6 or SAV7:
                save.SecondsToStart = To2000(date);
                break;
            case SAV8SWSH swsh:
                swsh.TrainerCard.StartedYear = (ushort)date.Year;
                swsh.TrainerCard.StartedMonth = (byte)date.Month;
                swsh.TrainerCard.StartedDay = (byte)date.Day;
                break;
            case SAV7b lgpe:
                lgpe.PlayerGeoLocation.AdventureBegin.Timestamp = date;
                break;
            case SAV8BS bs:
                bs.System.LocalTimestampStart = ReviseLocal(date, bs.System.LocalTimestampStart);
                break;
            case SAV8LA la:
                la.AdventureStart.Timestamp = date;
                break;
            case SAV9SV sv:
                sv.EnrollmentDate.Timestamp = date;
                break;
        }

        save.State.Edited = true;
        return true;
    }

    public static bool SetHallOfFame(SaveFile save, DateTime? value)
    {
        if (value is not { } date || !HasHallOfFame(save) || !IsValid(date))
            return false;

        save.SecondsToFame = To2000(date);
        save.State.Edited = true;
        return true;
    }

    public static bool SetLastSaved(SaveFile save, DateTime? value)
    {
        if (value is not { } date || !HasLastSaved(save) || !IsValid(date))
            return false;

        switch (save)
        {
            case SAV6 s: s.Played.LastSavedDate = date; break;
            case SAV7 s: s.Played.LastSavedDate = date; break;
            case SAV7b s: s.Played.LastSavedDate = date; break;
            case SAV8SWSH s: s.Played.LastSavedDate = date; break;
            case SAV8BS s: s.System.LocalTimestampLatest = ReviseLocal(date, s.System.LocalTimestampLatest); break;
            case SAV8LA s: s.LastSaved.Timestamp = date; break;
            case SAV9SV s: s.LastSaved.Timestamp = date; break;
            case SAV9ZA s: s.LastSaved.Timestamp = date; break;
        }

        save.State.Edited = true;
        return true;
    }

    private static DateTime From2000(uint seconds) => new DateTime(2000, 1, 1).AddSeconds(seconds);

    private static uint To2000(DateTime date) => (uint)(date - new DateTime(2000, 1, 1)).TotalSeconds;

    // Match WinForms' date range; avoids epoch underflow and overflowing the Gen 4-7 uint counters.
    private static bool IsValid(DateTime date) => date.Year is >= 2000 and <= 2099;

    private static DateTime? GetCardStart(TrainerCard8 card) =>
        DateUtil.IsValidDate(card.StartedYear, card.StartedMonth, card.StartedDay)
            ? new DateTime(card.StartedYear, card.StartedMonth, card.StartedDay)
            : null;

    // WinForms preserves BDSP's fractional FILETIME ticks when changing the displayed seconds.
    private static DateTime ReviseLocal(DateTime date, DateTime original) =>
        new DateTime(date.Ticks - date.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Local)
            .AddTicks(original.Ticks % TimeSpan.TicksPerSecond);

    private static DateTime? ReadDate(Func<DateTime?> read)
    {
        try
        {
            var date = read();
            return date is { } value && IsValid(value) ? value : null;
        }
        catch (ArgumentOutOfRangeException) { return null; }
    }
}
