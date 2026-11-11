namespace Pkmds.Core.Utilities;

/// <summary>
/// Trainer data supported by the released PKHeX 26.10.18 save implementations.
/// Storage formats inherit writable SaveFile placeholders, so generation alone is insufficient.
/// </summary>
public static class TrainerCapabilities
{
    public static bool HasIdentity(SaveFile save) => save is
        SAV1 or SAV2 or SAV3 or SAV3Colosseum or SAV3XD or SAV4 or SAV4BR or
        SAV5 or SAV6 or SAV7 or SAV7b or SAV8SWSH or SAV8BS or SAV8LA or SAV9SV or SAV9ZA;

    public static bool HasGender(SaveFile save) => HasIdentity(save) && save is not
        (SAV1 or SAV2 { Version: not GameVersion.C } or SAV4BR);

    public static bool HasPlaytime(SaveFile save) => HasIdentity(save) || save is SAV4Ranch;

    public static bool HasCoins(SaveFile save) => save is SAV1 or SAV2 or SAV3 or SAV4;

    public static int MaxPlayedHours(SaveFile save) => save is SAV1 ? byte.MaxValue : ushort.MaxValue;
}
