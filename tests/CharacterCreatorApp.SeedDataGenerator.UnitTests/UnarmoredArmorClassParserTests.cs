using CharacterCreatorApp.Domain.DomainModels.Classes;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>
/// The texts are the features' sentences from 5etools' class files, as the renderer writes them
/// with the markup taken out.
/// </summary>
public class UnarmoredArmorClassParserTests
{
    [Theory]
    [InlineData(
        "2014 barbarian",
        "While you are not wearing any armor, your Armor Class equals 10 + your Dexterity modifier + your Constitution modifier. You can use a shield and still gain this benefit.",
        10, "CON", true)]
    [InlineData(
        "2014 monk",
        "Beginning at 1st level, while you are wearing no armor and not wielding a shield, your AC equals 10 + your Dexterity modifier + your Wisdom modifier.",
        10, "WIS", false)]
    [InlineData(
        "2014 draconic sorcerer",
        "Additionally, parts of your skin are covered by a thin sheen of dragon-like scales. When you aren't wearing armor, your AC equals 13 + your Dexterity modifier.",
        13, null, true)]
    [InlineData(
        "2024 barbarian",
        "While you aren't wearing any armor, your base Armor Class equals 10 plus your Dexterity and Constitution modifiers. You can use a Shield and still gain this benefit.",
        10, "CON", true)]
    [InlineData(
        "2024 monk",
        "While you aren't wearing armor or wielding a Shield, your base Armor Class equals 10 plus your Dexterity and Wisdom modifiers.",
        10, "WIS", false)]
    [InlineData(
        "2024 draconic sorcerer",
        "Parts of you are also covered by dragon-like scales. While you aren't wearing armor, your base Armor Class equals 10 plus your Dexterity and Charisma modifiers.",
        10, "CHA", true)]
    public void Parse_AnUnarmoredArmorClassFeature_ReadsItsFormula(
        string feature, string text, int @base, string? abilityId, bool allowsShield)
    {
        // Act
        var armorClass = UnarmoredArmorClassParser.Parse(text);

        // Assert
        armorClass.Should().BeEquivalentTo(
            new UnarmoredArmorClass { Base = @base, AbilityId = abilityId, AllowsShield = allowsShield }, feature);
    }

    [Fact]
    public void Parse_AFeatureWhoseShieldRuleIsInItsParent_ReadsTheShieldFromThere()
    {
        // Arrange - the 2024 College of Dance's Dazzling Footwork, with its Unarmored Defense inlined
        const string text = "While you aren't wearing armor or wielding a Shield, you gain the following benefits.\n"
            + "Unarmored Defense. Your base Armor Class equals 10 plus your Dexterity and Charisma modifiers.";

        // Act
        var armorClass = UnarmoredArmorClassParser.Parse(text);

        // Assert
        armorClass.Should().BeEquivalentTo(new UnarmoredArmorClass { Base = 10, AbilityId = "CHA", AllowsShield = false });
    }

    [Theory]
    // A Circle of the Moon druid's Wild Shape AC has no Dexterity in it
    [InlineData("Until you leave the form, your AC equals 13 plus your Wisdom modifier if that total is higher than the Beast's AC.")]
    [InlineData("The vessel's AC equals your spell save DC.")]
    [InlineData("You gain proficiency with medium armor and shields.")]
    public void Parse_AFeatureWithoutAnUnarmoredArmorClass_ReturnsNull(string text)
    {
        // Act
        var armorClass = UnarmoredArmorClassParser.Parse(text);

        // Assert
        armorClass.Should().BeNull();
    }
}
