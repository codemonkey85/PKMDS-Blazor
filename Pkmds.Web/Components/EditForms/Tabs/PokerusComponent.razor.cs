namespace Pkmds.Web.Components.EditForms.Tabs;

public partial class PokerusComponent
{
    [Parameter, EditorRequired] public PKM? Pokemon { get; set; }

    private List<int> PokerusDays { get; set; } = [];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Pokemon is null)
        {
            return;
        }

        var max = Pokerus.GetMaxDuration(Pokemon.PokerusStrain);
        PokerusDays = Enumerable.Range(0, max + 1).ToList();
    }

    private void SetPokerusInfected(bool infected)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.IsPokerusInfected = infected;

        switch (infected)
        {
            case false when Pokemon.IsPokerusCured:
                Pokemon.IsPokerusCured = false;
                return;
            case true:
                {
                    if (Pokemon.PokerusStrain == 0)
                    {
                        Pokemon.PokerusStrain = 1;
                    }

                    if (Pokemon.PokerusDays == 0)
                    {
                        Pokemon.PokerusDays = 1;
                    }

                    break;
                }
            case false:
                Pokemon.PokerusStrain = Pokemon.PokerusDays = 0;
                break;
        }
    }

    private void SetPokerusCured(bool cured)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.IsPokerusCured = cured;

        if (cured)
        {
            if (Pokemon.PokerusStrain == 0)
            {
                Pokemon.PokerusStrain = 1;
            }

            Pokemon.PokerusDays = 0;
            Pokemon.IsPokerusInfected = true;
        }
        else if (!Pokemon.IsPokerusInfected)
        {
            Pokemon.PokerusStrain = 0;
        }
        else
        {
            Pokemon.PokerusDays = 1;
        }

        if (!cured && Pokemon.IsPokerusInfected && Pokemon.PokerusDays == 0)
        {
            Pokemon.PokerusDays = 1;
        }
    }

    private void SetPokerusStrain(int strain)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.PokerusStrain = strain;
        ChangePokerusDaysList(-1, strain, Pokemon.PokerusDays);
    }

    private void SetPokerusDays(int days)
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.PokerusDays = days;

        var strain = Pokemon.PokerusStrain;
        bool? cured = null;

        if (Pokerus.IsSusceptible(strain, days))
        {
            cured = Pokemon.IsPokerusInfected = false; // No Strain = Never Cured / Infected, triggers Strain update
        }
        else if (Pokerus.IsImmune(strain, days))
        {
            cured = true; // Any Strain = Cured
        }

        if (cured is not null)
        {
            Pokemon.IsPokerusCured = cured.Value;
        }
    }

    private void ChangePokerusDaysList(int oldStrain, int newStrain, int currentDuration)
    {
        if (oldStrain == newStrain)
        {
            return;
        }

        PokerusDays.Clear();
        var max = Pokerus.GetMaxDuration(newStrain);
        PokerusDays = Enumerable.Range(0, max + 1).ToList();

        // Set the days back if they're legal
        SetPokerusDays(Math.Min(max, currentDuration));
    }
}
