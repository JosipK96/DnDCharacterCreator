using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

public class SlugTests
{
    [Theory]
    [InlineData("Wizard", "wizard")]
    [InlineData("Path of the Totem Warrior", "path-of-the-totem-warrior")]
    [InlineData("Channel Divinity: Preserve Life", "channel-divinity-preserve-life")]
    [InlineData("Tinker's Tools", "tinkers-tools")]
    [InlineData("  Two  Spaces  ", "two-spaces")]
    public void Of_LowercasesAndJoinsWordsWithOneSeparator(string text, string expected)
    {
        // Act
        var slug = Slug.Of(text);

        // Assert
        slug.Should().Be(expected);
    }

    [Fact]
    public void Of_WithAnotherSeparator_UsesIt()
    {
        // Act
        var slug = Slug.Of("Sleight of Hand", '_');

        // Assert
        slug.Should().Be("sleight_of_hand");
    }

    [Theory]
    [InlineData("Purple Dragon Knight (Banneret)", "Purple Dragon Knight")]
    [InlineData("Knowledge (PSA)", "Knowledge")]
    [InlineData("Champion", "Champion")]
    public void WithoutParenthetical_DropsATrailingParenthesis(string text, string expected)
    {
        // Act
        var name = Slug.WithoutParenthetical(text);

        // Assert
        name.Should().Be(expected);
    }
}
