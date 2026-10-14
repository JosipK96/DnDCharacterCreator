using CharacterCreatorApp.Infrastructure.Seed;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using FluentAssertions;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// The feats the generator writes from 5etools, checked against the books. No database is
/// involved.
/// </summary>
public class FeatSeedDataTests
{
    [Theory]
    // A feat in both editions has its book's name in each, and the edition in its id
    [InlineData("alert_2014", "Alert", "2014", null)]
    [InlineData("alert_2024", "Alert", "2024", "Origin")]
    // The 2024 book spells it with a hyphen, which the id leaves out
    [InlineData("fey_touched_2014", "Fey Touched", "2014", null)]
    [InlineData("fey_touched_2024", "Fey-Touched", "2024", "General")]
    // The 2014 Fighting Styles are feats too, of the same category as the 2024 ones
    [InlineData("archery_2014", "Archery", "2014", "Fighting Style")]
    [InlineData("archery_2024", "Archery", "2024", "Fighting Style")]
    // Only in one edition: no suffix
    [InlineData("elven_accuracy", "Elven Accuracy", "2014", null)]
    [InlineData("interception", "Interception", "2024", "Fighting Style")]
    [InlineData("boon_of_fate", "Boon of Fate", "2024", "Epic Boon")]
    public async Task Feat_HasTheNameEditionAndCategoryOfItsBook(string featId, string name, string edition, string? category)
    {
        // Act
        var feat = (await ReadFeatsAsync()).Single(f => f.Id == featId);

        // Assert
        (feat.Name, feat.Edition, feat.Category).Should().Be((name, edition, category));
    }

    [Theory]
    [InlineData("fey_touched_2014", "misty_step")]
    [InlineData("fey_touched_2024", "misty_step_2024")]
    public async Task FeyTouched_GrantsMistyStepFromItsOwnEdition(string featId, string spellId)
    {
        // Act
        var feat = (await ReadFeatsAsync()).Single(f => f.Id == featId);

        // Assert - the second spell is a choice, which the app does not ask for
        feat.GrantedSpellIds.Should().Equal(spellId);
    }

    [Fact]
    public async Task Skilled2024_IsRepeatableAndOffersThreeSkillsOrTools()
    {
        // Act
        var skilled = (await ReadFeatsAsync()).Single(f => f.Id == "skilled_2024");

        // Assert
        skilled.IsRepeatable.Should().BeTrue();
        skilled.Choice!.Type.Should().Be("skills_tools");
        skilled.Choice.Count.Should().Be(3);
        skilled.Choice.Options.Should().Contain(["athletics", "thieves-tools"]);
    }

    [Fact]
    public async Task Resilient2014_OffersTheSavingThrowOfAnyAbility()
    {
        // Act
        var resilient = (await ReadFeatsAsync()).Single(f => f.Id == "resilient_2014");

        // Assert
        resilient.Choice!.Type.Should().Be("ability-scores");
        resilient.Choice.Count.Should().Be(1);
        resilient.Choice.Options.Should().Equal("STR", "DEX", "CON", "INT", "WIS", "CHA");
    }

    [Theory]
    // Fixed: nothing to pick
    [InlineData("actor_2014", new[] { "CHA" }, 1, 20)]
    [InlineData("boon_of_terror", new[] { "CHA" }, 1, 30)]
    // One of several
    [InlineData("athlete_2014", new[] { "STR", "DEX" }, 1, 20)]
    [InlineData("resilient_2024", new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" }, 1, 20)]
    // Epic Boons go up to 30
    [InlineData("boon_of_fate", new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" }, 1, 30)]
    // +2 to one or +1 to two
    [InlineData("ability_score_improvement", new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" }, 2, 20)]
    public async Task Feat_RaisesTheAbilitiesOfItsBook(string featId, string[] options, int points, int maximum)
    {
        // Act
        var feat = (await ReadFeatsAsync()).Single(f => f.Id == featId);

        // Assert
        feat.AbilityIncrease!.Options.Should().Equal(options);
        (feat.AbilityIncrease.Points, feat.AbilityIncrease.Maximum).Should().Be((points, maximum));
    }

    [Theory]
    // Only the Player's Handbook's six, each for the classes whose Fighting Style lists it
    [InlineData("archery_2014", new[] { "fighter", "ranger" })]
    [InlineData("defense_2014", new[] { "fighter", "paladin", "ranger" })]
    [InlineData("dueling_2014", new[] { "fighter", "bard", "paladin", "ranger" })]
    [InlineData("great_weapon_fighting_2014", new[] { "fighter", "paladin" })]
    [InlineData("protection_2014", new[] { "fighter", "paladin" })]
    [InlineData("two_weapon_fighting_2014", new[] { "fighter", "bard", "ranger" })]
    public async Task FightingStyle2014_IsForTheClassesThatOfferIt(string featId, string[] classIds)
    {
        // Act
        var feats = await ReadFeatsAsync();
        var style = feats.Single(f => f.Id == featId);

        // Assert
        feats.Where(f => f.Edition == "2014" && f.Category == "Fighting Style").Should().HaveCount(6);
        style.Prerequisites.ClassIds.Should().BeEquivalentTo(classIds);
        style.Prerequisites.Features.Should().Equal("Fighting Style");
    }

    [Fact]
    public async Task Alert_RaisesNoAbility()
    {
        // Act
        var alert = (await ReadFeatsAsync()).Single(f => f.Id == "alert_2014");

        // Assert
        alert.AbilityIncrease.Should().BeNull();
    }

    private static Task<List<FeatSeedRow>> ReadFeatsAsync() =>
        SeedFile.ReadListAsync<FeatSeedRow>(Path.Combine(SeedService.DataPath, "Feats.json"), TestContext.Current.CancellationToken);
}
