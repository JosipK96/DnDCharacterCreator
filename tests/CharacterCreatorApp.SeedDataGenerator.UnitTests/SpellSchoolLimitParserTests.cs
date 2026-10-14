using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>
/// The texts are the 2014 and 2024 Eldritch Knight's Spellcasting features from 5etools'
/// class-fighter.json, as the renderer writes them, shortened to the sentences that matter.
/// </summary>
public class SpellSchoolLimitParserTests
{
    [Fact]
    public void Parse_The2014EldritchKnight_ReadsBothSchoolsAndTheAnySchoolLevels()
    {
        // Arrange
        const string text = "You know three 1st-level wizard spells of your choice, two of which you must choose from the abjuration "
            + "and evocation spells on the wizard spell list. Each of these spells must be an abjuration or evocation spell of your "
            + "choice, and must be of a level for which you have spell slots. The spells you learn at 8th, 14th, and 20th level can "
            + "come from any school of magic. The new spell must be of a level for which you have spell slots, and it must be an "
            + "abjuration or evocation spell, unless you're replacing the spell you gained at 3rd, 8th, 14th, or 20th level from any "
            + "school of magic.";

        // Act
        var limit = SpellSchoolLimitParser.Parse(text);

        // Assert
        limit.Should().NotBeNull();
        limit!.Value.Schools.Should().Equal("Abjuration", "Evocation");
        limit.Value.AnySchoolLevels.Should().Equal(3, 8, 14, 20);
    }

    [Fact]
    public void Parse_The2014ArcaneTrickster_ReadsEnchantmentAndIllusion()
    {
        // Arrange
        const string text = "Each of these spells must be an enchantment or illusion spell of your choice. It must be an enchantment "
            + "or illusion spell, unless you're replacing the spell you gained at 3rd, 8th, 14th, or 20th level from any school of magic.";

        // Act
        var limit = SpellSchoolLimitParser.Parse(text);

        // Assert
        limit.Should().NotBeNull();
        limit!.Value.Schools.Should().Equal("Enchantment", "Illusion");
        limit.Value.AnySchoolLevels.Should().Equal(3, 8, 14, 20);
    }

    [Fact]
    public void Parse_The2024EldritchKnight_FindsNoLimit()
    {
        // Arrange - the 2024 rules dropped the schools
        const string text = "When you reach a Fighter level that grants more prepared spells, choose more Wizard spells until the "
            + "number of spells on your list matches the number in the Eldritch Knight Spellcasting table.";

        // Act
        var limit = SpellSchoolLimitParser.Parse(text);

        // Assert
        limit.Should().BeNull();
    }

    [Fact]
    public void Parse_ALimitWithoutAnySchoolLevels_Throws()
    {
        // Arrange - a reworded feature should stop the generator, not drop the free picks
        const string text = "Each of these spells must be an abjuration or evocation spell of your choice.";

        // Act
        var act = () => SpellSchoolLimitParser.Parse(text);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("A spellcasting feature limits its spells to abjuration and evocation but names no level for a spell from any school.");
    }
}
