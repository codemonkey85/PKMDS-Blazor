namespace Pkmds.Rcl.Components.MainTabPages;

public partial class RtcTab
{
    // RTC3.Day is stored as a UInt16; clamp UI input to that range.
    private const int MaxDays = ushort.MaxValue;
    private const int MaxHours = 23;

    // Emerald's berry program triggers a 366-day reset; the WinForms editor advances
    // ClockElapsed past two full years to bypass it after a dead-battery period.
    private const int BerryFixMinDays = (2 * 366) + 2;

    [Parameter]
    [EditorRequired]
    public SAV3? SaveFile { get; set; }

    private ISaveBlock3SmallHoenn? saveBlock;

    private int initialDay;
    private int initialHour;
    private int initialMinute;
    private int initialSecond;

    private int elapsedDay;
    private int elapsedHour;
    private int elapsedMinute;
    private int elapsedSecond;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        saveBlock = SaveFile?.SmallBlock as ISaveBlock3SmallHoenn;
        LoadClocks();
    }

    private void LoadClocks()
    {
        if (saveBlock is null)
        {
            return;
        }

        var initial = saveBlock.ClockInitial;
        initialDay = initial.Day;
        initialHour = Math.Min(MaxHours, initial.Hour);
        initialMinute = Math.Min(Constants.MaxMinutes, initial.Minute);
        initialSecond = Math.Min(Constants.MaxSeconds, initial.Second);

        var elapsed = saveBlock.ClockElapsed;
        elapsedDay = elapsed.Day;
        elapsedHour = Math.Min(MaxHours, elapsed.Hour);
        elapsedMinute = Math.Min(Constants.MaxMinutes, elapsed.Minute);
        elapsedSecond = Math.Min(Constants.MaxSeconds, elapsed.Second);
    }

    private void Apply()
    {
        if (saveBlock is null)
        {
            return;
        }

        // RTC3 getters return a copy backed by a fresh byte[]; mutate the copy
        // and assign it back so the setter writes the bytes into the save span.
        var initial = saveBlock.ClockInitial;
        initial.Day = initialDay;
        initial.Hour = initialHour;
        initial.Minute = initialMinute;
        initial.Second = initialSecond;
        saveBlock.ClockInitial = initial;

        var elapsed = saveBlock.ClockElapsed;
        elapsed.Day = elapsedDay;
        elapsed.Hour = elapsedHour;
        elapsed.Minute = elapsedMinute;
        elapsed.Second = elapsedSecond;
        saveBlock.ClockElapsed = elapsed;

        Snackbar.Add("RTC values applied to save.", Severity.Success);
        RefreshService.Refresh();
    }

    private void Revert() => LoadClocks();

    private void ResetClocks()
    {
        initialDay = initialHour = initialMinute = initialSecond = 0;
        elapsedDay = elapsedHour = elapsedMinute = elapsedSecond = 0;
    }

    private void BerryFix() => elapsedDay = Math.Max(BerryFixMinDays, elapsedDay);
}
