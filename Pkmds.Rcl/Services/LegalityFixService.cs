namespace Pkmds.Rcl.Services;

public sealed class LegalityFixService(IAppService appService) : ILegalityFixService
{
    public FixOutcome SuggestMoves(PKM pokemon)
    {
        pokemon.SetMoveset();

        // Update Technical Records (Gen 8+ SwSh / SV / ZA) to reflect the new moves,
        // mirroring PKMEditor's SetSuggestedMoves behaviour.
        if (pokemon is not ITechRecord tr)
        {
            return new FixOutcome(Changed: true, Severity.Success,
                "Moves updated with a legal move set. Click Save to apply changes.");
        }

        tr.ClearRecordFlags();
        var freshLa = appService.GetLegalityAnalysis(pokemon);
        tr.SetRecordFlags(pokemon, TechnicalRecordApplicatorOption.LegalCurrent, freshLa);

        return new FixOutcome(Changed: true, Severity.Success, "Moves updated with a legal move set. Click Save to apply changes.");
    }

    public FixOutcome SuggestRelearnMoves(PKM pokemon, LegalityAnalysis analysis)
    {
        pokemon.SetRelearnMoves(analysis);
        return new FixOutcome(Changed: true, Severity.Success, "Relearn moves updated. Click Save to apply changes.");
    }

    public FixOutcome SuggestBall(PKM pokemon, LegalityAnalysis analysis)
    {
        BallApplicator.ApplyBallLegalByColor(pokemon, analysis, PersonalColorUtil.GetColor(pokemon));
        return new FixOutcome(Changed: true, Severity.Success, "Ball updated to a legal option. Click Save to apply changes.");
    }

    public FixOutcome SuggestMetLocation(PKM pokemon)
    {
        var encounter = EncounterSuggestion.GetSuggestedMetInfo(pokemon);
        if (encounter is null)
        {
            return new FixOutcome(Changed: false, Severity.Warning, "No met location suggestion is available for this Pokémon.");
        }

        pokemon.MetLocation = encounter.Location;

        // If the suggested encounter is for a pre-evolution (e.g. Trophy Garden Pichu → Pikachu),
        // the Pokémon must have leveled up at least once from the encounter level to evolve.
        // Raise CurrentLevel to encounter.LevelMin + 1 before calling GetSuggestedMetLevel so
        // the brute-force loop considers a range that can include a valid MetLevel.
        if (encounter.Encounter is { } enc && enc.Species != pokemon.Species)
        {
            var minRequired = (byte)(encounter.LevelMin + 1);
            if (pokemon.CurrentLevel < minRequired)
            {
                pokemon.CurrentLevel = minRequired;
                appService.LoadPokemonStats(pokemon);
            }
        }

        var metLevel = encounter.GetSuggestedMetLevel(pokemon);
        pokemon.MetLevel = metLevel;

        // A Pokémon's current level must be at least its met level.
        // For freshly-created Pokémon the level defaults to 1, so raise it now.
        if (pokemon.CurrentLevel >= metLevel)
        {
            return new FixOutcome(Changed: true, Severity.Success,
                "Met location and level updated. Click Save to apply changes.");
        }

        pokemon.CurrentLevel = metLevel;
        appService.LoadPokemonStats(pokemon);

        return new FixOutcome(Changed: true, Severity.Success, "Met location and level updated. Click Save to apply changes.");
    }

    public FixOutcome RemoveInvalidRibbons(PKM pokemon, LegalityAnalysis analysis)
    {
        var args = new RibbonVerifierArguments(pokemon, analysis.EncounterMatch, analysis.Info.EvoChainsAllGens);
        RibbonApplicator.FixInvalidRibbons(in args);
        return new FixOutcome(Changed: true, Severity.Success, "Invalid ribbons removed. Click Save to apply changes.");
    }

    public FixOutcome AddValidRibbons(LegalityAnalysis analysis)
    {
        RibbonApplicator.SetAllValidRibbons(analysis);
        return new FixOutcome(Changed: true, Severity.Success, "All obtainable ribbons added. Click Save to apply changes.");
    }

    public FixOutcome FixTrashBytes(PKM pokemon, LegalityAnalysis analysis)
    {
        var enc = analysis.EncounterMatch;
        var changed = false;

        if (enc is PCD pcd)
        {
            var gift = pcd.Gift.PK;
            gift.OriginalTrainerTrash.CopyTo(pokemon.OriginalTrainerTrash);
            if (pcd.Species == pokemon.Species) // not evolved — nickname trash still relevant
            {
                gift.NicknameTrash.CopyTo(pokemon.NicknameTrash);
            }

            changed = true;
        }
        else if (pokemon.Format >= 8 || pokemon.Context == EntityContext.Gen7b)
        {
            changed |= EnsureTrashTerminator(pokemon.NicknameTrash);
            changed |= EnsureTrashTerminator(pokemon.OriginalTrainerTrash);
            changed |= EnsureTrashTerminator(pokemon.HandlingTrainerTrash);

            // Clear trash for fields explicitly flagged as needing to be empty (e.g. Gen 8 eggs).
            // Checked per-field to avoid zeroing HT on traded eggs, which require non-empty trash.
            if (HasInvalidResult(analysis, CheckIdentifier.Nickname, LegalityCheckResultCode.TrashBytesShouldBeEmpty))
            {
                changed |= ClearTrashAfterTerminator(pokemon, pokemon.NicknameTrash);
            }

            if (HasInvalidResult(analysis, CheckIdentifier.Trainer, LegalityCheckResultCode.TrashBytesShouldBeEmpty))
            {
                changed |= ClearTrashAfterTerminator(pokemon, pokemon.OriginalTrainerTrash);
            }

            if (HasInvalidResult(analysis, CheckIdentifier.Handler, LegalityCheckResultCode.TrashBytesShouldBeEmpty))
            {
                changed |= ClearTrashAfterTerminator(pokemon, pokemon.HandlingTrainerTrash);
            }
        }

        if (!changed)
        {
            return new FixOutcome(Changed: false, Severity.Warning, "No auto-fixable trash byte issues found.");
        }

        pokemon.RefreshChecksum();
        return new FixOutcome(Changed: true, Severity.Success, "Trash bytes fixed. Click Save to apply changes.");
    }

    public bool CanAutoFixTrashBytes(PKM pokemon, LegalityAnalysis analysis) =>
        analysis.EncounterMatch is PCD ||
        pokemon.Format >= 8 ||
        pokemon.Context == EntityContext.Gen7b;

    private static bool EnsureTrashTerminator(Span<byte> trash)
    {
        if (trash.Length < 2)
        {
            return false;
        }

        if (trash[^1] == 0 && trash[^2] == 0)
        {
            return false;
        }

        trash[^1] = 0;
        trash[^2] = 0;
        return true;
    }

    private static bool ClearTrashAfterTerminator(PKM pk, Span<byte> trash)
    {
        var termCharIdx = pk.GetStringTerminatorIndex(trash);
        if (termCharIdx < 0)
        {
            return false;
        }

        var byteOffset = (termCharIdx + 1) * pk.GetBytesPerChar();
        if (byteOffset >= trash.Length)
        {
            return false;
        }

        var trashRegion = trash[byteOffset..];
        if (!trashRegion.ContainsAnyExcept<byte>(0))
        {
            return false;
        }

        trashRegion.Clear();
        return true;
    }

    private static bool HasInvalidResult(LegalityAnalysis la, CheckIdentifier identifier, LegalityCheckResultCode code) =>
        la.Results.Any(r => !r.Valid && r.Identifier == identifier && r.Result == code);
}
