namespace Pkmds.Rcl;

public static class ShinyUtils
{
    public static bool GetIsShinySafe(this PKM pk)
    {
        if (pk.Format <= 2) // Gen I / II
        {
            // In Gen I / II, shininess is determined by the DV values
            return pk switch
            {
                PK1 pk1 => GetIsShinyGb(pk1.DV16),
                PK2 pk2 => GetIsShinyGb(pk2.DV16),
                _ => false
            };
        }

        // For Gen III and later, shininess is determined by the PID
        return pk.IsShiny;

        static bool GetIsShinyGb(ushort dv16) => (dv16 & 0x2FFF) == 0x2AAA;
    }

    public static bool SetIsShinySafe(this PKM pk, bool shiny)
    {
        if (pk.Format > 2) // Gen I / II
        {
            return pk.SetIsShiny(shiny);
        }

        if (shiny)
        {
            return pk.SetIsShiny(true);
        }

        if (!pk.IsShiny)
        {
            return false;
        }

        do
        {
            pk.SetRandomIVs();
        } while (pk.GetIsShinySafe());

        return true;
    }
}
