using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class BackgroundTests
{
    // The 2024 soldier: +2 and +1, or +1 to each, among Strength, Dexterity and Constitution
    private static readonly Background Soldier2024 = new()
    {
        Id = "soldier_2024",
        Name = "Soldier",
        Edition = RulesEdition.Edition2024,
        AbilityScoreOptions = ["STR", "DEX", "CON"],
        FeatId = "savage_attacker_2024"
    };

    private static readonly Background Soldier2014 = new()
    {
        Id = "soldier",
        Name = "Soldier",
        Edition = RulesEdition.Edition2014,
        FeatureId = "soldier_military_rank"
    };

    [Fact]
    public void LanguageChoiceCount_For2014Background_CountsItsAnyPlaceholders()
    {
        // Arrange - an Acolyte: two languages of the player's choice
        var acolyte = new Background { Id = "acolyte", Name = "Acolyte", Edition = RulesEdition.Edition2014, Languages = ["any", "ANY"] };

        // Assert
        acolyte.LanguageChoiceCount.Should().Be(2);
        acolyte.NamedLanguages.Should().BeEmpty();
    }

    [Fact]
    public void LanguageChoiceCount_For2024Background_IsTwoBesideCommon()
    {
        // Arrange
        var soldier = new Background { Id = "soldier_2024", Name = "Soldier", Edition = RulesEdition.Edition2024, Languages = ["Common", "any", "any"] };

        // Assert
        soldier.LanguageChoiceCount.Should().Be(2);
        soldier.NamedLanguages.Should().Equal("Common");
    }

    [Theory]
    [InlineData("STR:2 CON:1", true)]
    [InlineData("CON:1 STR:2", true)]
    [InlineData("STR:1 DEX:1 CON:1", true)]
    [InlineData("strength:2 con:1", true)]
    [InlineData("STR:2 CON:1 DEX:0", true)]
    [InlineData("STR:2", false)]
    [InlineData("STR:1 DEX:1", false)]
    [InlineData("STR:2 CON:2", false)]
    [InlineData("STR:3", false)]
    [InlineData("STR:2 WIS:1", false)]
    [InlineData("STR:2 str:1", false)]
    [InlineData("STR:2 luck:1", false)]
    [InlineData("", false)]
    public void AllowsAbilityIncreases_For2024Background_AcceptsTwoAndOneOrOneToEachOfItsAbilities(string increases, bool expected)
    {
        // Act
        var allowed = Soldier2024.AllowsAbilityIncreases(Increases(increases));

        // Assert
        allowed.Should().Be(expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("STR:1", false)]
    public void AllowsAbilityIncreases_For2014Background_AcceptsNone(string increases, bool expected)
    {
        // Act - a 2014 character's race raises its abilities, not its background
        var allowed = Soldier2014.AllowsAbilityIncreases(Increases(increases));

        // Assert
        allowed.Should().Be(expected);
    }

    [Theory]
    [InlineData("soldier_2024", null, "savage_attacker_2024")]
    [InlineData("soldier_2024", "mist_walker", "savage_attacker_2024")]
    [InlineData("soldier", null, null)]
    [InlineData("mist_wanderer_2024", "mist_walker", "mist_walker")]
    [InlineData("mist_wanderer_2024", null, null)]
    [InlineData("haunted_one_2024", "mist_walker", "mist_walker")]
    [InlineData("haunted_one_2024", null, "survivor")]
    public void FeatIdWith_APickOrNone_GivesThePickOnlyWhereTheBackgroundAllowsOne(string backgroundId, string? pickedFeatId, string? expected)
    {
        // Arrange
        var background = FeatBackgrounds()[backgroundId];

        // Act
        var featId = background.FeatIdWith(pickedFeatId);

        // Assert
        featId.Should().Be(expected);
    }

    [Theory]
    [InlineData("soldier_2024", null)]
    [InlineData("soldier", null)]
    [InlineData("mist_wanderer_2024", "Dark Gift")]
    [InlineData("haunted_one_2024", "Dark Gift")]
    public void PickableFeatCategory_ForEachKindOfBackground_IsTheCategoryAPickMustBeFrom(string backgroundId, string? expected)
    {
        // Act
        var category = FeatBackgrounds()[backgroundId].PickableFeatCategory;

        // Assert
        category.Should().Be(expected);
    }

    // A feat of its own, none, one the player must pick, and one the player may pick in its place
    private static Dictionary<string, Background> FeatBackgrounds() => new()
    {
        ["soldier_2024"] = Soldier2024,
        ["soldier"] = Soldier2014,
        ["mist_wanderer_2024"] = new() { Id = "mist_wanderer_2024", Name = "Mist Wanderer", Edition = RulesEdition.Edition2024, FeatChoiceCategory = "Dark Gift" },
        ["haunted_one_2024"] = new()
        {
            Id = "haunted_one_2024", Name = "Haunted One", Edition = RulesEdition.Edition2024, FeatId = "survivor", FeatAlternativeCategory = "Dark Gift",
        },
    };

    // "STR:2 CON:1" as {"STR": 2, "CON": 1}; keys that differ only in case stay apart
    private static Dictionary<string, int> Increases(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(':'))
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
}
