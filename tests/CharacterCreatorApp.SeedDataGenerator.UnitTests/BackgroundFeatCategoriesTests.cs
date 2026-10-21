using System.Text.Json.Nodes;

using FluentAssertions;

namespace CharacterCreatorApp.SeedDataGenerator.UnitTests;

/// <summary>
/// The "feats" entries are the shapes 5etools' backgrounds.json uses for the backgrounds named.
/// </summary>
public class BackgroundFeatCategoriesTests
{
    [Theory]
    [InlineData("haunted one", """[{"survivor|rhw": true}, {"anyFromCategory": {"category": ["DG"]}}]""", null, "Dark Gift")]
    [InlineData("mist wanderer", """[{"anyFromCategory": {"category": ["DG"]}}]""", "Dark Gift", null)]
    [InlineData("soldier", """[{"savage attacker|xphb": true}]""", null, null)]
    [InlineData("rewarded", """[{"lucky|phb": true}, {"magic initiate|phb": true}, {"skilled|phb": true}]""", null, null)]
    public void FeatCategories_ABackgroundsFeats_ReadsTheCategoryToPickFromAndTheAlternative(
        string background, string feats, string? choiceCategory, string? alternativeCategory)
    {
        // Arrange
        var entry = JsonNode.Parse($$"""{"name": "{{background}}", "feats": {{feats}}}""")!.AsObject();

        // Act
        var categories = BackgroundImporter.FeatCategories(entry);

        // Assert
        categories.Should().Be((choiceCategory, alternativeCategory));
    }

    [Fact]
    public void FeatCategories_ABackgroundWithNoFeats_HasNeither()
    {
        // Arrange - a 2014 acolyte has a feature and no feat
        var entry = JsonNode.Parse("""{"name": "acolyte"}""")!.AsObject();

        // Act
        var categories = BackgroundImporter.FeatCategories(entry);

        // Assert
        categories.Should().Be(((string?)null, (string?)null));
    }
}
