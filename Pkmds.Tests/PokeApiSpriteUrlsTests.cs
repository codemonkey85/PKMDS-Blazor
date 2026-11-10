namespace Pkmds.Tests;

public class PokeApiSpriteUrlsTests
{
    private const string VersionsBaseUrl =
        "https://sprites.pkmds.app/collections/a7a3d62bb80a654ab5e01b488659929af0077c31/pokemon/versions/";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegendArceus_UsesNormalArtworkInSelectedSet(bool isShiny)
    {
        var pokemon = new PA8 { Species = (ushort)Species.Arceus, Form = 18 };
        pokemon.SetIsShiny(isShiny);
        var shinyPath = isShiny ? "shiny/" : string.Empty;

        PokeApiSpriteUrls.GetPokeApiHomeSpriteUrl(pokemon.Species, pokemon.Form, isShiny: isShiny)
            .Should().Be(SpriteSource.HomeBaseUrl + shinyPath + "493.png");
        ImageHelper.GetPokemonGameSpriteUrl(pokemon, GameVersion.PLA)
            .Should().Be(VersionsBaseUrl + "generation-viii/legends-arceus/" + shinyPath + "493.webp");
        pokemon.Form.Should().Be(18);
    }

    [Theory]
    [InlineData((byte)9, "fire")]
    [InlineData((byte)17, "fairy")]
    public void TypedArceus_KeepsItsOwnArtwork(byte form, string suffix)
    {
        foreach (var isShiny in new[] { false, true })
        {
            var shinyPath = isShiny ? "shiny/" : string.Empty;
            PokeApiSpriteUrls.GetPokeApiHomeSpriteUrl((ushort)Species.Arceus, form, isShiny: isShiny)
                .Should().Be(SpriteSource.HomeBaseUrl + shinyPath + $"493-{suffix}.png");
            PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl((ushort)Species.Arceus, form,
                    isShiny: isShiny, version: GameVersion.PLA)
                .Should().Be(VersionsBaseUrl + "generation-viii/legends-arceus/" + shinyPath + $"493-{suffix}.webp");
        }
    }

    [Fact]
    public void UnsupportedArceusForm_StillReturnsNull()
    {
        PokeApiSpriteUrls.GetPokeApiHomeSpriteUrl((ushort)Species.Arceus, 19).Should().BeNull();
        PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl((ushort)Species.Arceus, 19, version: GameVersion.PLA)
            .Should().BeNull();
    }

    [Theory]
    [InlineData(GameVersion.OR, "generation-vi/omega-ruby-alpha-sapphire/25.png")]
    [InlineData(GameVersion.US, "generation-vii/ultra-sun-ultra-moon/25.gif")]
    [InlineData(GameVersion.GP, "generation-vii/lets-go-pikachu-lets-go-eevee/25.gif")]
    [InlineData(GameVersion.SW, "generation-viii/sword-shield/25.webp")]
    [InlineData(GameVersion.PLA, "generation-viii/legends-arceus/25.webp")]
    [InlineData(GameVersion.BD, "generation-viii/brilliant-diamond-shining-pearl/25.webp")]
    [InlineData(GameVersion.SL, "generation-ix/scarlet-violet/25.webp")]
    [InlineData(GameVersion.ZA, "generation-ix/legends-za/25.webp")]
    public void GetPokeApiVersionSpriteUrl_UsesCurrentSetPathAndFormat(
        GameVersion version, string expectedPath)
    {
        // Act
        var result = PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl(
            (ushort)Species.Pikachu, version: version);

        // Assert
        result.Should().Be(VersionsBaseUrl + expectedPath);
    }

    [Theory]
    [InlineData(GameVersion.USUM, "generation-vii/ultra-sun-ultra-moon/25.gif")]
    [InlineData(GameVersion.GG, "generation-vii/lets-go-pikachu-lets-go-eevee/25.gif")]
    [InlineData(GameVersion.SWSH, "generation-viii/sword-shield/25.webp")]
    [InlineData(GameVersion.BDSP, "generation-viii/brilliant-diamond-shining-pearl/25.webp")]
    [InlineData(GameVersion.SV, "generation-ix/scarlet-violet/25.webp")]
    public void GetPokeApiVersionSpriteUrl_SupportsGroupedVersions(
        GameVersion version, string expectedPath)
    {
        // Act
        var result = PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl(
            (ushort)Species.Pikachu, version: version);

        // Assert
        result.Should().Be(VersionsBaseUrl + expectedPath);
    }

    [Theory]
    [InlineData(GameVersion.Gen1, "generation-i/red-blue/transparent/25.png")]
    [InlineData(GameVersion.Gen2, "generation-ii/gold/transparent/25.png")]
    [InlineData(GameVersion.GSC, "generation-ii/gold/transparent/25.png")]
    public void GetPokeApiVersionSpriteUrl_UsesTransparentVariantForEarlyGenerationGroups(
        GameVersion version, string expectedPath)
    {
        // Act
        var result = PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl(
            (ushort)Species.Pikachu, version: version);

        // Assert
        result.Should().Be(VersionsBaseUrl + expectedPath);
    }

    [Theory]
    [InlineData(GameVersion.Gen4, "generation-iv/diamond-pearl/female/25.png")]
    [InlineData(GameVersion.Gen5, "generation-v/black-white/female/25.png")]
    [InlineData(GameVersion.Gen6, "generation-vi/x-y/female/25.png")]
    [InlineData(GameVersion.Gen7, "generation-vii/ultra-sun-ultra-moon/female/25.gif")]
    public void GetPokeApiVersionSpriteUrl_UsesFemaleVariantForLaterGenerationGroups(
        GameVersion version, string expectedPath)
    {
        // Act
        var result = PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl(
            (ushort)Species.Pikachu, gender: (byte)Gender.Female, version: version);

        // Assert
        result.Should().Be(VersionsBaseUrl + expectedPath);
    }

    [Theory]
    [InlineData(GameVersion.GP, "generation-vii/lets-go-pikachu-lets-go-eevee/shiny/female/25.gif")]
    [InlineData(GameVersion.BD, "generation-viii/brilliant-diamond-shining-pearl/shiny/female/25.webp")]
    [InlineData(GameVersion.SL, "generation-ix/scarlet-violet/shiny/female/25.webp")]
    [InlineData(GameVersion.ZA, "generation-ix/legends-za/shiny/female/25.webp")]
    public void GetPokeApiVersionSpriteUrl_UsesAvailableShinyFemaleVariant(
        GameVersion version, string expectedPath)
    {
        // Act
        var result = PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl(
            (ushort)Species.Pikachu, isShiny: true, gender: (byte)Gender.Female, version: version);

        // Assert
        result.Should().Be(VersionsBaseUrl + expectedPath);
    }

    [Fact]
    public void GetPokeApiVersionSpriteUrl_UnsupportedVersion_ReturnsNull()
    {
        // Act
        var result = PokeApiSpriteUrls.GetPokeApiVersionSpriteUrl(
            (ushort)Species.Pikachu, version: GameVersion.GO);

        // Assert
        result.Should().BeNull();
    }
}
