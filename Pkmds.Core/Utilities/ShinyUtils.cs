namespace Pkmds.Core.Utilities;

public static class ShinyUtils
{
    extension(PKM pk)
    {
        public bool GetIsShinySafe()
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

        public void SetIsShinySafe(bool shiny)
        {
            if (pk.Format > 2) // Gen III+
            {
                pk.SetIsShiny(shiny);
                return;
            }

            if (shiny)
            {
                pk.SetIsShiny(true);
                return;
            }

            if (!pk.IsShiny)
            {
                return;
            }

            do
            {
                pk.SetRandomIVs();
            } while (pk.GetIsShinySafe());
        }
    }
}
