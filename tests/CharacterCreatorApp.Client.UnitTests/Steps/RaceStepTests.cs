using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Races;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Races;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The race step's ability increases, spread as the API says: a half-elf's two +1s, a Monsters of
/// the Multiverse race's +2 and +1 or +1 to three, Custom Lineage's +2 to one.
/// </summary>
public class RaceStepTests : WizardStepTestBase
{
    private static readonly List<string> AllSix = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    [Fact]
    public void RaceStep_ForAMonstersOfTheMultiverseRace_SendsPlusTwoAndPlusOne()
    {
        // Arrange
        var cut = RenderWith(Race("aasimar_mpmm", "Aasimar") with { AbilityChoiceCount = 3, AbilityChoiceOptions = AllSix, AbilityChoiceMaxPerAbility = 2 });
        Button(cut, "Aasimar").Click();

        // Act - Strength twice is +2, Wisdom once +1
        Button(cut, "+1 Strength").Click();
        Button(cut, "+1 Strength").Click();
        Button(cut, "+1 Wisdom").Click();
        Button(cut, "Next: Class").Click();

        // Assert
        Api.BodyOf<SetRaceApiRequest>(SaveRoute("race")).AbilityIncreases.Should()
            .BeEquivalentTo(new Dictionary<string, int> { ["STR"] = 2, ["WIS"] = 1 });
    }

    [Fact]
    public void RaceStep_ForAMonstersOfTheMultiverseRace_SendsPlusOneToThree()
    {
        // Arrange
        var cut = RenderWith(Race("aasimar_mpmm", "Aasimar") with { AbilityChoiceCount = 3, AbilityChoiceOptions = AllSix, AbilityChoiceMaxPerAbility = 2 });
        Button(cut, "Aasimar").Click();

        // Act
        Button(cut, "+1 Strength").Click();
        Button(cut, "+1 Dexterity").Click();
        Button(cut, "+1 Charisma").Click();
        Button(cut, "Next: Class").Click();

        // Assert
        Api.BodyOf<SetRaceApiRequest>(SaveRoute("race")).AbilityIncreases.Should()
            .BeEquivalentTo(new Dictionary<string, int> { ["STR"] = 1, ["DEX"] = 1, ["CHA"] = 1 });
    }

    [Fact]
    public void RaceStep_PickingAnAbilityPastTheCap_UnpicksIt()
    {
        // Arrange
        var cut = RenderWith(Race("aasimar_mpmm", "Aasimar") with { AbilityChoiceCount = 3, AbilityChoiceOptions = AllSix, AbilityChoiceMaxPerAbility = 2 });
        Button(cut, "Aasimar").Click();
        Button(cut, "+1 Strength").Click();
        Button(cut, "+1 Strength").Click();
        Button(cut, "+1 Wisdom").Click();

        // Act - a third pick of Strength is past the cap
        Button(cut, "+2 Strength").Click();

        // Assert
        Button(cut, "+1 Strength").GetAttribute("aria-pressed").Should().Be("false");
        Button(cut, "+1 Wisdom").GetAttribute("aria-pressed").Should().Be("true");
        Button(cut, "Next: Class").HasAttribute("disabled").Should().BeTrue("only 1 of the 3 points is spent");
    }

    [Fact]
    public void RaceStep_WithNoPointsLeft_DisablesTheUnpickedAbilities()
    {
        // Arrange
        var cut = RenderWith(Race("aasimar_mpmm", "Aasimar") with { AbilityChoiceCount = 3, AbilityChoiceOptions = AllSix, AbilityChoiceMaxPerAbility = 2 });
        Button(cut, "Aasimar").Click();

        // Act
        Button(cut, "+1 Strength").Click();
        Button(cut, "+1 Strength").Click();
        Button(cut, "+1 Wisdom").Click();

        // Assert
        Button(cut, "+1 Dexterity").HasAttribute("disabled").Should().BeTrue();
        cut.Markup.Should().Contain("Spread 3 points, at most +2 to one ability");
    }

    [Fact]
    public void RaceStep_ForCustomLineage_RaisesOnePickByTwo()
    {
        // Arrange
        var cut = RenderWith(Race("custom_lineage_tce", "Custom Lineage") with
        {
            AbilityChoiceCount = 2,
            AbilityChoiceOptions = AllSix,
            AbilityChoiceMaxPerAbility = 2,
            AbilityChoiceMinPerAbility = 2,
        });
        Button(cut, "Custom Lineage").Click();

        // Act
        Button(cut, "+2 Constitution").Click();
        Button(cut, "Next: Class").Click();

        // Assert
        Button(cut, "+2 Strength").HasAttribute("disabled").Should().BeTrue("the one +2 is spent");
        Api.BodyOf<SetRaceApiRequest>(SaveRoute("race")).AbilityIncreases.Should()
            .BeEquivalentTo(new Dictionary<string, int> { ["CON"] = 2 });
    }

    [Fact]
    public void RaceStep_ForTheHalfElf_SendsTwoPlusOnes()
    {
        // Arrange
        var cut = RenderWith(Race("half_elf", "Half-Elf") with { AbilityChoiceCount = 2, AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS"] });
        Button(cut, "Half-Elf").Click();

        // Act - a second pick of Dexterity unpicks it, as a +1 each choice always has
        Button(cut, "+1 Dexterity").Click();
        Button(cut, "+1 Dexterity").Click();
        Button(cut, "+1 Dexterity").Click();
        Button(cut, "+1 Wisdom").Click();
        Button(cut, "Next: Class").Click();

        // Assert
        Api.BodyOf<SetRaceApiRequest>(SaveRoute("race")).AbilityIncreases.Should()
            .BeEquivalentTo(new Dictionary<string, int> { ["DEX"] = 1, ["WIS"] = 1 });
    }

    [Fact]
    public void RaceStep_WithPlusTwoSaved_ShowsItAndSavesItUnchanged()
    {
        // Arrange
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse
        {
            Id = DraftId,
            RaceId = "aasimar_mpmm",
            RaceAbilityIncreases = new Dictionary<string, int> { ["CHA"] = 2, ["WIS"] = 1 },
        });
        Api.Get("/api/races/get-list?edition=2014", new List<RaceApiResponse>
        {
            Race("aasimar_mpmm", "Aasimar") with { AbilityChoiceCount = 3, AbilityChoiceOptions = AllSix, AbilityChoiceMaxPerAbility = 2 },
        });
        Api.Put(DraftPath("race"));

        // Act
        var cut = Render<RaceStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 2)
            .Add(p => p.Edition, "2014"));
        Button(cut, "Next: Class").Click();

        // Assert
        Button(cut, "+2 Charisma").GetAttribute("aria-pressed").Should().Be("true");
        Api.BodyOf<SetRaceApiRequest>(SaveRoute("race")).AbilityIncreases.Should()
            .BeEquivalentTo(new Dictionary<string, int> { ["CHA"] = 2, ["WIS"] = 1 });
    }

    [Fact]
    public void RaceStep_WithARaceReprinted_SaysEachOnesBookAndNoneForTheOthers()
    {
        // Arrange - VGM's aasimar and MPMM's, both ticked
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId });
        Api.Get("/api/races/get-list?edition=2014", new List<RaceApiResponse>
        {
            Race("aasimar_mpmm", "Aasimar") with { Source = "MPMM" },
            Race("aasimar_vgm", "Aasimar") with { Source = "VGM" },
            Race("dwarf", "Dwarf"),
        });

        // Act
        var cut = Render<RaceStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 2)
            .Add(p => p.Edition, "2014"));

        // Assert
        cut.FindAll("[data-testid=race-book]").Select(e => e.TextContent).Should().Equal("MPMM", "VGM");
    }

    // The step with only this race on offer and nothing saved yet
    private IRenderedComponent<RaceStep> RenderWith(RaceApiResponse race)
    {
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId });
        Api.Get("/api/races/get-list?edition=2014", new List<RaceApiResponse> { race });
        Api.Put(DraftPath("race"));

        return Render<RaceStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 2)
            .Add(p => p.Edition, "2014"));
    }
}
