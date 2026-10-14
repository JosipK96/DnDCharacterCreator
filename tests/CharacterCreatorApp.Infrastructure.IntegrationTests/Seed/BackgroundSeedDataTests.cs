using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure.Seed;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using FluentAssertions;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// The backgrounds the generator writes from 5etools, checked against the books. No database is
/// involved.
/// </summary>
public class BackgroundSeedDataTests
{
    [Theory]
    [InlineData("acolyte", new[] { "insight", "religion" }, new[] { "any", "any" }, "acolyte_shelter_of_the_faithful", null)]
    [InlineData("criminal", new[] { "deception", "stealth", "thieves-tools" }, new string[0], "criminal_criminal_contact", null)]
    [InlineData("giant_foundling", new[] { "intimidation", "survival" }, new[] { "giant", "any" }, "giant_foundling_strike_of_the_giants", "strike_of_the_giants")]
    public async Task Background2014_HasItsProficienciesLanguagesAndFeature(
        string backgroundId, string[] proficiencyIds, string[] languages, string featureId, string? featId)
    {
        // Act
        var background = (await ReadBackgroundsAsync()).Single(b => b.Id == backgroundId);

        // Assert - no ability increases: a 2014 character's race gives them
        background.Edition.Should().Be(RulesEdition.Edition2014);
        background.ProficiencyIds.Should().Equal(proficiencyIds);
        background.Languages.Should().Equal(languages);
        (background.FeatureId, background.FeatId).Should().Be((featureId, featId));
        background.AbilityScoreOptions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("acolyte_2024", new[] { "INT", "WIS", "CHA" }, "magic_initiate_2024", new[] { "insight", "religion", "calligrapher_supplies" })]
    [InlineData("farmer_2024", new[] { "STR", "CON", "WIS" }, "tough_2024", new[] { "animal_handling", "nature", "carpenter_tools" })]
    [InlineData("soldier_2024", new[] { "STR", "DEX", "CON" }, "savage_attacker_2024", new[] { "athletics", "intimidation" })]
    public async Task Background2024_RaisesThreeAbilitiesAndGivesAnOriginFeat(
        string backgroundId, string[] abilityScoreOptions, string featId, string[] proficiencyIds)
    {
        // Act
        var background = (await ReadBackgroundsAsync()).Single(b => b.Id == backgroundId);

        // Assert - the soldier's gaming set is a pick the app does not ask for yet
        background.Edition.Should().Be(RulesEdition.Edition2024);
        background.AbilityScoreOptions.Should().Equal(abilityScoreOptions);
        background.FeatId.Should().Be(featId);
        background.ProficiencyIds.Should().Equal(proficiencyIds);
        background.FeatureId.Should().BeNull();
    }

    [Fact]
    public async Task Backgrounds2024_EachGiveCommonAndTwoLanguagePicks()
    {
        // Act - the 2024 rules give every new character these; the background carries them
        var backgrounds2024 = (await ReadBackgroundsAsync()).Where(b => b.Edition == RulesEdition.Edition2024).ToList();

        // Assert
        backgrounds2024.Should().NotBeEmpty();
        backgrounds2024.Should().OnlyContain(b => b.Languages.SequenceEqual(new[] { "common", "any", "any" }));
    }

    private static Task<List<BackgroundSeedRow>> ReadBackgroundsAsync() =>
        SeedFile.ReadListAsync<BackgroundSeedRow>(Path.Combine(SeedService.DataPath, "Backgrounds.json"), TestContext.Current.CancellationToken);
}
