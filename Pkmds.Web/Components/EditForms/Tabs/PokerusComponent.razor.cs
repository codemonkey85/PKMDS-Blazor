namespace Pkmds.Web.Components.EditForms.Tabs;

public partial class PokerusComponent
{
    [Parameter, EditorRequired]
    public PKM? Pokemon { get; set; }

    private void SetPokerusInfected(bool infected)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.IsPokerusInfected = infected;
    }

    private void SetPokerusCured(bool cured)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.IsPokerusCured = cured;
    }

    private void SetPokerusStrain(int strain)
    {
        if (Pokemon is null)
        {
            return;
        }
        Pokemon.PokerusStrain = strain;
    }

    private void SetPokerusDays(int days)
    {
        if (Pokemon is null)
        {
            return;
        }
        Pokemon.PokerusDays = days;
    }
}

