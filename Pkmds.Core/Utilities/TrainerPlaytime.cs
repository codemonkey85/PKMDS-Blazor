using static System.Buffers.Binary.BinaryPrimitives;

namespace Pkmds.Core.Utilities;

public static class TrainerPlaytime
{
    public static void SetHours(SaveFile save, int hours)
    {
        if (!TrainerCapabilities.HasPlaytime(save) || hours < 0 || hours > TrainerCapabilities.MaxPlayedHours(save))
            return;

        // PKHeX.Core 26.10.18's XD/PBR/Ranch PlayedHours setters subtract TotalHours,
        // erasing minutes, seconds and fractions. Update the same backing value by a
        // whole-hour delta instead. Keep this workaround until Core fixes those setters (#1376).
        var delta = (hours - save.PlayedHours) * 3600.0;
        switch (save)
        {
            case SAV4Ranch ranch:
                ranch.TotalSeconds = (uint)(ranch.TotalSeconds + delta);
                break;
            case SAV4BR br:
                var pbrTime = br.Data[0x388..];
                WriteDoubleBigEndian(pbrTime, ReadDoubleBigEndian(pbrTime) + delta);
                break;
            case SAV3XD xd:
                // Config offset is a pair of big-endian ushorts (low word first).
                var config = 0xA8 + ReadUInt16BigEndian(xd.Data[0x40..]) +
                             (ReadUInt16BigEndian(xd.Data[0x42..]) << 16);
                var japanese = ReadUInt16BigEndian(xd.Data[0x2E..]) == 0x1E00;
                var xdTime = xd.Data[(config + (japanese ? 0x20 : 0x30))..];
                if (japanese)
                    WriteSingleBigEndian(xdTime, (float)(ReadSingleBigEndian(xdTime) + delta));
                else
                    WriteDoubleBigEndian(xdTime, ReadDoubleBigEndian(xdTime) + delta);
                break;
            default:
                save.PlayedHours = hours;
                break;
        }

        if (save is SAV1 gen1 && hours < byte.MaxValue)
            gen1.PlayedMaximum = false;
        save.State.Edited = true;
    }

    public static void Reset(SaveFile save)
    {
        SetHours(save, 0);
        save.PlayedMinutes = 0;
        save.PlayedSeconds = 0;
        if (save is SAV1 gen1)
        {
            gen1.PlayedMaximum = false;
            gen1.PlayedFrames = 0;
        }
        if (save is SAV9ZA za)
            za.Played.RawSeconds = 0;
    }
}
