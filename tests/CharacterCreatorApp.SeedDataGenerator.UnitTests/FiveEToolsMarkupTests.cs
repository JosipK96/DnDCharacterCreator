using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>
/// Every example is taken from the 5etools data.
/// </summary>
public class FiveEToolsMarkupTests
{
    [Theory]
    [InlineData("roll a {@dice d4} and add it", "roll a d4 and add it")]
    [InlineData("take {@damage 1d8} radiant damage", "take 1d8 radiant damage")]
    [InlineData("{@dice 1d20+5|+5}", "+5")]
    [InlineData("{@d20 8}", "+8")]
    [InlineData("{@d20 0}", "+0")]
    [InlineData("{@d20 -1}", "-1")]
    [InlineData("a {@dc 15} Wisdom saving throw", "a DC 15 Wisdom saving throw")]
    [InlineData("a {@chance 50} chance", "a 50 percent chance")]
    [InlineData("{@chance 25|||Random reading!|Regular reading}", "25 percent")]
    public void ToPlainText_Rolls_ShowTheRoll(string markup, string expected)
    {
        // Act
        var text = FiveEToolsMarkup.ToPlainText(markup);

        // Assert
        text.Should().Be(expected);
    }

    [Theory]
    [InlineData("{@spell augury}", "augury")]
    [InlineData("{@spell Guidance|XPHB}", "Guidance")]
    [InlineData("{@condition prone}", "prone")]
    [InlineData("{@creature servo|psk|servos}", "servos")]
    [InlineData("{@status concentration||concentrating}", "concentrating")]
    [InlineData("{@table skills|phb|skill proficiency}", "skill proficiency")]
    [InlineData("{@item healer's kit|phb}", "healer's kit")]
    [InlineData("{@skill Deception|XPHB}", "Deception")]
    public void ToPlainText_Links_ShowTheDisplayTextOrTheName(string markup, string expected)
    {
        // Act
        var text = FiveEToolsMarkup.ToPlainText(markup);

        // Assert
        text.Should().Be(expected);
    }

    [Theory]
    [InlineData("{@book PHB p202|PHB|10|Bonus Action}", "PHB p202")]
    [InlineData("{@adventure contamination levels|DoDk|12}", "contamination levels")]
    [InlineData("{@filter 2nd-level spell|spells|level=2|school=E;N}", "2nd-level spell")]
    [InlineData("{@5etools feat|feats.html}", "feat")]
    [InlineData("{@quickref resting|PHB|2|0|long rest}", "long rest")]
    [InlineData("{@quickref difficult terrain||3}", "difficult terrain")]
    [InlineData("{@classFeature Divine Sense|Paladin|PHB|1}", "Divine Sense")]
    [InlineData("{@subclassFeature Channel Divinity: Mind of Two Moons|Cleric|PHB|Moon|TDCSR|6|TDCSR|Mind of Two Moons}", "Mind of Two Moons")]
    public void ToPlainText_TagsThatKeepTheirTextElsewhere_ShowThatText(string markup, string expected)
    {
        // Act
        var text = FiveEToolsMarkup.ToPlainText(markup);

        // Assert
        text.Should().Be(expected);
    }

    [Fact]
    public void ToPlainText_NestedTags_ResolvesTheInnerOneFirst()
    {
        // Act
        var text = FiveEToolsMarkup.ToPlainText("{@i 2nd-level bard {@spell heat metal|phb}}");

        // Assert
        text.Should().Be("2nd-level bard heat metal");
    }

    [Theory]
    [InlineData("")]
    [InlineData("You touch one willing creature.")]
    [InlineData("A {curly} brace is not markup")]
    public void ToPlainText_TextWithoutMarkup_IsUnchanged(string plain)
    {
        // Act
        var text = FiveEToolsMarkup.ToPlainText(plain);

        // Assert
        text.Should().Be(plain);
    }
}
