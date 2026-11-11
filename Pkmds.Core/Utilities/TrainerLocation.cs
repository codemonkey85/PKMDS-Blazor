namespace Pkmds.Core.Utilities;

public static class TrainerLocation
{
    public static void SetRotation(SAV9ZA save, double degrees)
    {
        if (!double.IsFinite(degrees))
            return;

        // PKHeX.Core 26.10.18's SetPlayerRotation(double) puts sin(angle) in RY,
        // while Rotation reads RZ. Use the backing components so edits round-trip (#1376).
        var angle = degrees * Math.PI / 360.0;
        save.Coordinates.SetPlayerRotation(0, 0, (float)Math.Sin(angle), (float)Math.Cos(angle));
        save.State.Edited = true;
    }
}
