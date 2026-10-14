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

        // Assert - the soldier's gaming set is a pick (ToolChoices), not a proficiency it gives
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

    [Theory]
    [InlineData("mist_wanderer_2024")]
    [InlineData("spirit_medium_2024")]
    public async Task Background2024_ThatLetsThePlayerPickTheFeat_OffersADarkGift(string backgroundId)
    {
        // Act
        var background = (await ReadBackgroundsAsync()).Single(b => b.Id == backgroundId);

        // Assert
        background.FeatId.Should().BeNull();
        background.FeatChoiceCategory.Should().Be("Dark Gift");
    }

    [Theory]
    [InlineData("acolyte_2024", "cleric-2024")]
    [InlineData("guide_2024", "druid-2024")]
    [InlineData("sage_2024", "wizard-2024")]
    [InlineData("soldier_2024", null)]
    public async Task Background2024_FixesTheListOfItsMagicInitiate(string backgroundId, string? classId)
    {
        // Act
        var background = (await ReadBackgroundsAsync()).Single(b => b.Id == backgroundId);

        // Assert
        background.FeatSpellListClassId.Should().Be(classId);
        background.FeatChoiceCategory.Should().BeNull();
    }

    [Theory]
    [InlineData("criminal")]
    [InlineData("noble")]
    [InlineData("soldier_2024")]
    [InlineData("carouser_2024")]
    public async Task Background_WithAGamingSet_LetsThePlayerPickOneOfTheFour(string backgroundId)
    {
        // Act
        var background = (await ReadBackgroundsAsync()).Single(b => b.Id == backgroundId);

        // Assert
        background.ToolChoices.Should().ContainSingle();
        background.ToolChoices[0].Count.Should().Be(1);
        background.ToolChoices[0].Options.Should().Equal("dice_set", "dragonchess", "playing_card_set", "three_dragon_ante");
    }

    [Fact]
    public async Task Vizier_PicksOneArtisansToolsAndOneInstrument()
    {
        // Act
        var vizier = (await ReadBackgroundsAsync()).Single(b => b.Id == "vizier");

        // Assert
        vizier.ToolChoices.Select(c => (c.Count, c.Options.Count)).Should().Equal((1, 17), (1, 10));
        vizier.ToolChoices[0].Options.Should().Contain(["smith_tools", "tinkers-tools"]);
        vizier.ToolChoices[1].Options.Should().Contain("lute");
    }

    [Fact]
    public async Task FarTraveler_PicksAnInstrumentOrAGamingSet()
    {
        // Act
        var farTraveler = (await ReadBackgroundsAsync()).Single(b => b.Id == "far_traveler");

        // Assert
        farTraveler.ToolChoices.Should().ContainSingle().Which.Options.Should().HaveCount(14).And.Contain(["lute", "dice_set"]);
    }

    private static Task<List<BackgroundSeedRow>> ReadBackgroundsAsync() =>
        SeedFile.ReadListAsync<BackgroundSeedRow>(Path.Combine(SeedService.DataPath, "Backgrounds.json"), TestContext.Current.CancellationToken);
}
