using System.Text.Json.Nodes;

using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>
/// SourceBooks keeps what it noted for the whole run, so each test uses a book no other test
/// does.
/// </summary>
public class SourceBooksTests
{
    [Fact]
    public void Of_AnEntryFromABookWithATitle_ReturnsTheCodeAndListsTheBook()
    {
        // Act
        var code = SourceBooks.Of(Entry("""{"name": "Fireball", "source": "PHB"}"""), RulesEdition.Edition2014);

        // Assert
        code.Should().Be("PHB");
        SourceBooks.Used().Single(b => b.Code == "PHB").Should().BeEquivalentTo(
            new { Name = "Player's Handbook (2014)", Edition = RulesEdition.Edition2014, IsPlayersHandbook = true });
    }

    [Fact]
    public void Of_AnEntryFromABookWithNoTitle_Throws()
    {
        // Act
        var act = () => SourceBooks.Of(Entry("""{"name": "Mystery", "source": "ZZZ"}"""), RulesEdition.Edition2014);

        // Assert - Books.json would otherwise name the book by its code
        act.Should().Throw<InvalidDataException>().WithMessage("*ZZZ*");
        SourceBooks.Used().Should().NotContain(b => b.Code == "ZZZ");
    }

    [Fact]
    public void Of_ABookAlreadyNotedInTheOtherEdition_Throws()
    {
        // Arrange
        SourceBooks.Of(Entry("""{"name": "Satyr", "source": "MOT"}"""), RulesEdition.Edition2014);

        // Act
        var act = () => SourceBooks.Of(Entry("""{"name": "Leonin", "source": "MOT"}"""), RulesEdition.Edition2024);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("MOT has rows in 2014 and 2024*");
    }

    private static JsonObject Entry(string json) => JsonNode.Parse(json)!.AsObject();
}
