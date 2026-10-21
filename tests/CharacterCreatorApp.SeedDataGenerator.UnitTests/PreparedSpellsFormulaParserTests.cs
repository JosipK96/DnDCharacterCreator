using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>The formulas are the 2014 classes' "preparedSpells" in 5etools' class files.</summary>
public class PreparedSpellsFormulaParserTests
{
    [Theory]
    [InlineData("<$level$> + <$wis_mod$>", "wis", 1)]
    [InlineData("<$level$> + <$int_mod$>", "int", 1)]
    [InlineData("<$level$> / 2 + <$cha_mod$>", "cha", 2)]
    [InlineData("<$level$> / 2 + <$int_mod$>", "int", 2)]
    public void ParseLevelDivisor_AClassFormula_ReadsWhatTheLevelIsDividedBy(string formula, string ability, int divisor)
    {
        // Act
        var result = PreparedSpellsFormulaParser.ParseLevelDivisor(formula, ability);

        // Assert
        result.Should().Be(divisor);
    }

    [Fact]
    public void ParseLevelDivisor_AFormulaOfAnotherAbility_Throws()
    {
        // Act
        var parse = () => PreparedSpellsFormulaParser.ParseLevelDivisor("<$level$> + <$wis_mod$>", "cha");

        // Assert
        parse.Should().Throw<InvalidOperationException>().WithMessage("*another ability*");
    }

    [Fact]
    public void ParseLevelDivisor_AFormulaOfAnotherShape_Throws()
    {
        // Act - a shape the app would count wrongly, so the generator stops rather than guessing
        var parse = () => PreparedSpellsFormulaParser.ParseLevelDivisor("<$level$> * 2 + <$wis_mod$>", "wis");

        // Assert
        parse.Should().Throw<InvalidOperationException>().WithMessage("*not a level*");
    }
}
