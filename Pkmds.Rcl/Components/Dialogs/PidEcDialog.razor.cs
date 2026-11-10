namespace Pkmds.Rcl.Components.Dialogs;

public partial class PidEcDialog
{
    // Yield to the browser event loop every this many iterations to keep the UI responsive.
    private const int YieldInterval = 10_000;

    private bool avoidShiny = true;

    private bool forceShiny;

    [Parameter]
    [EditorRequired]
    public PKM? Pokemon { get; set; }

    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    private PIDType SelectedMethod { get; set; } = PIDType.Method_1;

    private Shiny SelectedShinyType { get; set; } = Shiny.AlwaysStar;

    private bool KeepNature { get; set; } = true;

    private bool KeepGender { get; set; } = true;

    private bool AvoidShiny
    {
        get => avoidShiny;
        set
        {
            avoidShiny = value;
            if (value)
            {
                forceShiny = false;
            }
        }
    }

    private bool ForceShiny
    {
        get => forceShiny;
        set
        {
            forceShiny = value;
            if (value)
            {
                avoidShiny = false;
            }
        }
    }

    private int SaveGeneration => AppState.SaveFile?.Generation ?? 0;

    private bool IsGen345 => SaveGeneration is 3 or 4 or 5;

    private bool IsGenerating { get; set; }

    private async Task GeneratePid()
    {
        if (Pokemon is null)
        {
            return;
        }

        var desiredNature = (byte)Pokemon.Nature;
        var genderRatio = Pokemon.PersonalInfo.Gender;
        var desiredGender = Pokemon.Gender;
        var isDualGender = Pokemon.PersonalInfo.IsDualGender;

        // For Gen 3–5, the ability slot is encoded in the PID (bit 0 for Gen 3/4, bit 16
        // for Gen 5). Preserve it so randomizing the PID does not silently change the
        // Pokémon's ability. See EntityPID.GetRandomPID in PKHeX.Core for reference.
        var abilityBitMask = SaveGeneration switch
        {
            3 or 4 => 0x0000_0001u,
            5 => 0x0001_0000u,
            _ => 0u
        };
        var desiredAbilityBit = Pokemon.PID & abilityBitMask;

        const int maxAttempts = 1_000_000;
        var attempts = 0;
        IsGenerating = true;
        try
        {
            uint pid;
            do
            {
                if (++attempts > maxAttempts)
                {
                    Snackbar.Add(
                        "Could not find a PID satisfying all constraints after 1,000,000 attempts. " +
                        "Try relaxing the constraints.",
                        Severity.Warning);
                    return;
                }

                if (attempts % YieldInterval == 0)
                {
                    await Task.Yield();
                }

                pid = NextRandomUInt32();
                Pokemon.PID = pid; // needed to evaluate IsShiny
            } while (
                abilityBitMask != 0 && (pid & abilityBitMask) != desiredAbilityBit ||
                IsGen345 && KeepNature && pid % 25 != desiredNature ||
                IsGen345 && KeepGender && isDualGender && EntityGender.GetFromPIDAndRatio(pid, genderRatio) != desiredGender ||
                AvoidShiny && Pokemon.IsShiny ||
                ForceShiny && !Pokemon.IsShiny
            );
        }
        finally
        {
            IsGenerating = false;
        }

        AppService.LoadPokemonStats(Pokemon);
        Haptics.Confirm();
        RefreshService.Refresh();
    }

    private void MakeShinyPid()
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetIsShiny(true);
        Haptics.Confirm();
        RefreshService.Refresh();
    }

    private async Task GenerateWithMethod()
    {
        if (Pokemon is null)
        {
            return;
        }

        var desiredNature = (byte)Pokemon.Nature;
        var genderRatio = Pokemon.PersonalInfo.Gender;
        var desiredGender = Pokemon.Gender;
        var isDualGender = Pokemon.PersonalInfo.IsDualGender;

        // A Method 1/2/4 pair alone does not guarantee a valid wild encounter frame.
        // Check candidates on copies so failed searches leave the Pokémon untouched (#1363).
        const int maxAttempts = 1_000_000;
        const int maxEncounterChecks = 1_000;
        var encounterChecks = 0;
        var desiredAbilityBit = Pokemon.PID & 1;
        IsGenerating = true;
        try
        {
            for (var attempts = 1; attempts <= maxAttempts; attempts++)
            {
                if (attempts % YieldInterval == 0)
                {
                    await Task.Yield();
                }

                var pair = ClassicPidIvGenerator.Generate(SelectedMethod, NextRandomUInt32());
                var isShiny = ShinyUtil.GetIsShiny3(Pokemon.ID32, pair.Pid);
                if ((pair.Pid & 1) != desiredAbilityBit ||
                    KeepNature && pair.Pid % 25 != desiredNature ||
                    KeepGender && isDualGender && EntityGender.GetFromPIDAndRatio(pair.Pid, genderRatio) != desiredGender ||
                    AvoidShiny && isShiny || ForceShiny && !isShiny)
                {
                    continue;
                }

                // Full encounter analysis is more expensive than the PID filters above.
                if (encounterChecks >= maxEncounterChecks)
                {
                    break;
                }

                if (++encounterChecks % 32 == 0)
                {
                    await Task.Yield();
                }

                if (!ClassicPidIvGenerator.MatchesEncounterRng(Pokemon, pair))
                {
                    continue;
                }

                Pokemon.PID = pair.Pid;
                Pokemon.SetIVs(pair.Ivs);
                AppService.LoadPokemonStats(Pokemon);
                Haptics.Confirm();
                RefreshService.Refresh();
                return;
            }

            Snackbar.Add(
                "Could not find PID/IVs matching this encounter and the selected constraints. " +
                "Try relaxing the constraints. The Pokémon was not changed.", Severity.Warning);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private void RandomizeEc()
    {
        if (Pokemon is null)
        {
            return;
        }

        // Note: EC % 6 determines the Pokémon's characteristic (e.g. "Often dozes off").
        // If preserving the characteristic ever becomes a requirement, loop until the new
        // EC satisfies newEc % 6 == oldEc % 6 before calling SetRandomEC.
        Pokemon.SetRandomEC();
        AppService.LoadPokemonStats(Pokemon);
        Haptics.Confirm();
        RefreshService.Refresh();
    }

    private void ApplyShinyType()
    {
        if (Pokemon is null)
        {
            return;
        }

        Pokemon.SetShiny(SelectedShinyType);
        Haptics.Confirm();
        RefreshService.Refresh();
    }

    private static uint NextRandomUInt32() =>
        (uint)Random.Shared.NextInt64(uint.MaxValue + 1L);

    private void Close() => MudDialog?.Close();
}
