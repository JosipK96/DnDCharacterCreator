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

    private int _movedOn;

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

    [Fact]
    public void RaceStep_ForARaceWithSubraces_WaitsForOneAndSendsIt()
    {
        // Arrange
        var cut = RenderWith(Race("dwarf", "Dwarf", subraces: [Subrace("hill-dwarf", "Hill Dwarf", "dwarf"), Subrace("mountain-dwarf", "Mountain Dwarf", "dwarf")]));
        Button(cut, "Dwarf").Click();
        Button(cut, "Next: Class").HasAttribute("disabled").Should().BeTrue("no subrace is picked");

        // Act
        Button(cut, "Mountain Dwarf").Click();
        Button(cut, "Next: Class").Click();

        // Assert
        var sent = Api.BodyOf<SetRaceApiRequest>(SaveRoute("race"));
        (sent.RaceId, sent.SubraceId, sent.Size).Should().Be(("dwarf", "mountain-dwarf", (string?)null));
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void RaceStep_ForASpeciesSmallOrMedium_WaitsForASizeAndSendsIt()
    {
        // Arrange - a 2024 human
        var cut = RenderWith(Race("human", "Human") with { Size = "Small or Medium" });
        Button(cut, "Human").Click();
        Button(cut, "Next: Class").HasAttribute("disabled").Should().BeTrue("no size is picked");

        // Act
        cut.FindAll("[data-testid=option-room]").Single(r => r.QuerySelector("span.font-display")!.TextContent.Trim() == "Small").Click();
        Button(cut, "Next: Class").Click();

        // Assert
        Api.BodyOf<SetRaceApiRequest>(SaveRoute("race")).Size.Should().Be("Small");
    }

    [Fact]
    public void RaceStep_ForARaceWithSkillsToPick_SavesTheRaceAndOffersOnlyItsSkills()
    {
        // Arrange - a 2024 elf's Keen Senses: one of three
        var cut = RenderKeenSensesElf();

        // Act
        Button(cut, "Next: Choose Skills").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Should().Be("Step 2: Elf Skill Proficiencies"));
        cut.FindAll("[data-testid=choice-row]").Select(r => r.QuerySelector("span.font-semibold")!.TextContent)
            .Should().Equal("Insight", "Perception", "Survival");
        Api.BodyOf<SetRaceApiRequest>(SaveRoute("race")).RaceId.Should().Be("elf");
        _movedOn.Should().Be(0, "the race is saved, the skills not yet");
    }

    [Fact]
    public void RaceStep_AfterPickingTheRacesSkill_SendsItAndMovesOn()
    {
        // Arrange
        var cut = RenderKeenSensesElf();
        Button(cut, "Next: Choose Skills").Click();
        cut.WaitForAssertion(() => Row(cut, "Perception").Click());

        // Act
        Button(cut, "Next: Class").Click();

        // Assert
        Api.BodyOf<ChooseRaceSkillsApiRequest>(SaveRoute("race-skills")).ProficiencyIds.Should().Equal("perception");
        _movedOn.Should().Be(1);
    }

    private IRenderedComponent<RaceStep> RenderKeenSensesElf()
    {
        var cut = RenderWith(Race("elf", "Elf") with { SkillChoiceCount = 1, SkillChoiceOptions = ["insight", "perception", "survival"] });
        Api.Get("/api/proficiencies", new List<Contracts.Responses.Proficiencies.ProficiencyApiResponse>
        {
            Skill("athletics", "Athletics"), Skill("insight", "Insight"), Skill("perception", "Perception"), Skill("survival", "Survival"),
            new("dice-set", "Dice Set", "gaming_set"),
        });
        Api.Put(DraftPath("race-skills"));
        Button(cut, "Elf").Click();
        return cut;
    }

    [Fact]
    public void RaceStep_BackFromTheSkills_ReturnsToTheRaces()
    {
        // Arrange - a half-elf picks any two skills
        var cut = RenderWith(Race("half_elf", "Half-Elf") with { SkillChoiceCount = 2 });
        Api.Get("/api/proficiencies", new List<Contracts.Responses.Proficiencies.ProficiencyApiResponse> { Skill("athletics", "Athletics"), Skill("insight", "Insight") });
        Button(cut, "Half-Elf").Click();
        Button(cut, "Next: Choose Skills").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid=choice-row]").Should().HaveCount(2));

        // Act
        Button(cut, "Back").Click();

        // Assert
        cut.Find("h2").TextContent.Should().Be("Step 2: Choose Your Race");
    }

    [Fact]
    public void RaceStep_WhenTheApiRefusesTheRace_ShowsItsReasonAndStays()
    {
        // Arrange
        var cut = RenderWith(Race("dwarf", "Dwarf"));
        Api.RefusePut(DraftPath("race"), "That race is not in the draft's books");
        Button(cut, "Dwarf").Click();

        // Act
        Button(cut, "Next: Class").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should().Contain("That race is not in the draft's books"));
        _movedOn.Should().Be(0);
    }

    [Fact]
    public void RaceStep_WhenTheRacesFailToLoad_SaysSo()
    {
        // Arrange
        Api.NotFound("/api/races/get-list?edition=2014");

        // Act
        var cut = Render<RaceStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 2)
            .Add(p => p.Edition, "2014"));

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("Failed to load races");
    }

    private static AngleSharp.Dom.IElement Row(IRenderedComponent<RaceStep> cut, string name) =>
        cut.FindAll("[data-testid=choice-row]").Single(r => r.QuerySelector("span.font-semibold")!.TextContent == name);
    // The step with only this race on offer and nothing saved yet
    private IRenderedComponent<RaceStep> RenderWith(RaceApiResponse race)
    {
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId });
        Api.Get("/api/races/get-list?edition=2014", new List<RaceApiResponse> { race });
        Api.Put(DraftPath("race"));

        return Render<RaceStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 2)
            .Add(p => p.Edition, "2014")
            .Add(p => p.OnNext, () => _movedOn++));
    }
}
