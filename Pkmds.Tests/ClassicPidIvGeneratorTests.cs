namespace Pkmds.Tests;

public class ClassicPidIvGeneratorTests
{
    [Theory]
    [InlineData(PIDType.Method_1)]
    [InlineData(PIDType.Method_2)]
    [InlineData(PIDType.Method_4)]
    public void GenerateForIVs_RecoversOriginalPairsAndPreservesEveryIV(PIDType method)
    {
        // Include both high-bit variants and several IV spreads to catch RNG
        // rewind errors and Method 4's skipped call.
        foreach (var seed in new uint[] { 0, 1, 0x12345678, 0x92345678, uint.MaxValue })
        {
            var original = ClassicPidIvGenerator.Generate(method, seed);
            var pairs = ClassicPidIvGenerator.GenerateForIVs(method, original.Ivs);

            pairs.Should().Contain(original);
            pairs.Should().OnlyContain(pair => pair.Ivs == original.Ivs);
            pairs.Should().OnlyHaveUniqueItems();
        }
    }

    [Fact]
    public void GenerateForIVs_RejectsUnsupportedMethods()
    {
        var generate = () => ClassicPidIvGenerator.GenerateForIVs(PIDType.None, 0);

        generate.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(PIDType.Method_1)]
    [InlineData(PIDType.Method_2)]
    [InlineData(PIDType.Method_4)]
    public void Generate_ProducesTheSelectedCorrelation(PIDType method)
    {
        var pair = ClassicPidIvGenerator.Generate(method, 0x12345678);
        var pokemon = new PK3 { Species = (ushort)Species.Pidgeot, Version = GameVersion.FR, PID = pair.Pid };
        pokemon.SetIVs(pair.Ivs);

        MethodFinder.Analyze(pokemon).Type.Should().Be(method);
    }

    [Fact]
    public void MatchesEncounterRng_AcceptsTheOriginalWildPairWithoutChangingThePokemon()
    {
        var pokemon = LoadWildPokemon();
        var original = pokemon.Data.ToArray();
        var pair = new ClassicPidIvGenerator.PidIvPair(pokemon.PID, pokemon.GetIVs());

        ClassicPidIvGenerator.MatchesEncounterRng(pokemon, pair).Should().BeTrue();
        pokemon.Data.ToArray().Should().Equal(original);
    }

    [Fact]
    public void MatchesEncounterRng_RejectsCorrelatedPairsWithAnUnmatchedEncounterFrame()
    {
        var pokemon = LoadWildPokemon();
        var original = pokemon.Data.ToArray();
        var pair = Enumerable.Range(0, 64)
            .Select(seed => ClassicPidIvGenerator.Generate(PIDType.Method_1, (uint)seed))
            .First(pair =>
            {
                var candidate = pokemon.Clone();
                candidate.PID = pair.Pid;
                candidate.SetIVs(pair.Ivs);
                var analysis = new LegalityAnalysis(candidate);
                return analysis.Info.PIDIVMatches && !analysis.Info.FrameMatches;
            });

        ClassicPidIvGenerator.MatchesEncounterRng(pokemon, pair).Should().BeFalse();
        pokemon.Data.ToArray().Should().Equal(original);
    }

    [Fact]
    public void MatchesEncounterRng_RejectsAnUnmatchedEncounter()
    {
        var pokemon = new PK3();
        var pair = ClassicPidIvGenerator.Generate(PIDType.Method_1, 0x12345678);

        ClassicPidIvGenerator.MatchesEncounterRng(pokemon, pair).Should().BeFalse();
    }

    [Fact]
    public void Generate_RejectsUnsupportedMethods()
    {
        var generate = () => ClassicPidIvGenerator.Generate(PIDType.None, 0);

        generate.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static PKM LoadWildPokemon()
    {
        // Public repository fixture; no private issue attachment is included in tests.
        var (save, _, _, _) = BunitTestHelpers.LoadSave("POKEMON FIRE_BPRE-0.sav");
        var pokemon = save.GetPartySlotAtIndex(2);
        pokemon.Species.Should().Be((ushort)Species.Pidgeot);
        return pokemon;
    }
}
