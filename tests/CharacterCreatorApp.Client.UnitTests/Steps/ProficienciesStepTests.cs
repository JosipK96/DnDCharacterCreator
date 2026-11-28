using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses.Backgrounds;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Classes;
using CharacterCreatorApp.Contracts.Responses.Proficiencies;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The proficiencies step: the class's skill picks without the ones the background already gives,
/// the tool picks the race, background and class offer, what it sends, and a saved draft's picks.
/// </summary>
public class ProficienciesStepTests : WizardStepTestBase
{
    private const string ToolChoicesPath = "tool-choices";

    // A rogue picks two of four; a criminal background already gives Stealth
    private static readonly ClassApiResponse Rogue = Class("rogue", "Rogue") with
    {
        ProficiencyChoice = new("skills", 2,
        [
            Skill("acrobatics", "Acrobatics"), Skill("deception", "Deception"), Skill("stealth", "Stealth"), Skill("insight", "Insight"),
        ]),
    };

    private static readonly ProficiencyApiResponse Dice = new("dice-set", "Dice Set", "gaming_set");
    private static readonly ProficiencyApiResponse Cards = new("playing-cards", "Playing Card Set", "gaming_set");
    private static readonly ProficiencyApiResponse Lute = new("lute", "Lute", "musical_instrument");

    [Fact]
    public void Render_ARogueCriminal_LeavesOutTheSkillTheBackgroundGivesAndSaysWhy()
    {
        // Arrange
        ArrangeLoad(Draft(), Rogue, []);

        // Act
        var cut = RenderStep();

        // Assert
        RowNames(cut).Should().Equal("Acrobatics", "Deception", "Insight");
        cut.Find("[role=note]").TextContent.Should().Contain("Stealth");
        Button(cut, "Next: Improvements").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Click_TwoSkills_DisablesTheRestAndSendsThem()
    {
        // Arrange
        ArrangeLoad(Draft(), Rogue, []);
        Api.Put(DraftPath("proficiencies"));
        var nexts = 0;
        var cut = RenderStep(() => nexts++);

        // Act
        Row(cut, "Insight").Click();
        Row(cut, "Acrobatics").Click();

        // Assert
        Row(cut, "Deception").HasAttribute("disabled").Should().BeTrue();
        Button(cut, "Next: Improvements").Click();
        cut.WaitForAssertion(() => nexts.Should().Be(1));
        Api.BodyOf<ChooseProficienciesApiRequest>(SaveRoute("proficiencies")).ProficiencyIds.Should().BeEquivalentTo(["insight", "acrobatics"]);
        Api.Requests.Should().NotContain(SaveRoute("tools"));
    }

    [Fact]
    public void Render_WithSkillsSaved_ShowsThemPicked()
    {
        // Arrange
        ArrangeLoad(Draft(skills: ["deception", "insight"]), Rogue, []);

        // Act
        var cut = RenderStep();

        // Assert
        cut.FindAll("[data-testid=choice-row][aria-pressed=true]").Select(r => r.QuerySelector("span.font-semibold")!.TextContent)
            .Should().Equal("Deception", "Insight");
        Button(cut, "Next: Improvements").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Click_ToolsForTwoChoices_KeepsOneToolToOneChoiceAndSendsThePicksInOrder()
    {
        // Arrange - a class with no skill to pick, and two tool choices offering the dice
        var fighter = Class("fighter", "Fighter");
        ArrangeLoad(Draft(classId: "fighter"), fighter,
        [
            new("Folk Hero", 1, [Dice, Cards]),
            new("Fighter", 1, [Dice, Lute]),
        ]);
        Api.Put(DraftPath("tools"));
        var nexts = 0;
        var cut = RenderStep(() => nexts++);

        // Act
        ToolRow(cut, 0, "Dice Set").Click();

        // Assert - the dice are taken, so the class's choice cannot take them too
        ToolRow(cut, 1, "Dice Set").HasAttribute("disabled").Should().BeTrue();
        Button(cut, "Next: Improvements").HasAttribute("disabled").Should().BeTrue();
        ToolRow(cut, 1, "Lute").Click();
        Button(cut, "Next: Improvements").Click();
        cut.WaitForAssertion(() => nexts.Should().Be(1));
        Api.BodyOf<ChooseToolsApiRequest>(SaveRoute("tools")).Picks.Should().BeEquivalentTo(
            new List<List<string>> { new() { "dice-set" }, new() { "lute" } },
            options => options.WithStrictOrdering());
        Api.Requests.Should().NotContain(SaveRoute("proficiencies"));
    }

    [Fact]
    public void Render_WithToolsSaved_ShowsThemPickedWhileTheChoicesStillFit()
    {
        // Arrange
        ArrangeLoad(Draft(classId: "fighter", toolPicks: [["playing-cards"]]), Class("fighter", "Fighter"), [new("Folk Hero", 1, [Dice, Cards])]);

        // Act
        var cut = RenderStep();

        // Assert
        ToolRow(cut, 0, "Playing Card Set").GetAttribute("aria-pressed").Should().Be("true");
        cut.Find("[data-testid=choice-section] h3").TextContent.Should().Contain("Folk Hero: choose 1 tool");
    }

    [Fact]
    public void Render_WithSavedToolsForOtherChoices_StartsThePicksAfresh()
    {
        // Arrange - two picks saved, but the draft now offers one choice
        ArrangeLoad(Draft(classId: "fighter", toolPicks: [["dice-set"], ["lute"]]), Class("fighter", "Fighter"), [new("Folk Hero", 1, [Dice, Cards])]);

        // Act
        var cut = RenderStep();

        // Assert
        cut.FindAll("[data-testid=choice-row][aria-pressed=true]").Should().BeEmpty();
    }

    [Fact]
    public void Render_AClassWithNothingToPick_SaysSoAndSavesNothingOnNext()
    {
        // Arrange
        ArrangeLoad(Draft(classId: "fighter"), Class("fighter", "Fighter"), []);
        var nexts = 0;
        var cut = RenderStep(() => nexts++);
        cut.Markup.Should().Contain("No Proficiency Choices");

        // Act
        Button(cut, "Next: Improvements").Click();

        // Assert
        cut.WaitForAssertion(() => nexts.Should().Be(1));
        Api.Requests.Should().NotContain(r => r.StartsWith("PUT ", StringComparison.Ordinal));
    }

    [Fact]
    public void Next_WhenTheApiRefusesTheSkills_ShowsItsMessageAndStays()
    {
        // Arrange
        ArrangeLoad(Draft(skills: ["deception", "insight"]), Rogue, []);
        Api.RefusePut(DraftPath("proficiencies"), "Insight is not a rogue skill");
        var nexts = 0;
        var cut = RenderStep(() => nexts++);

        // Act
        Button(cut, "Next: Improvements").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should().Contain("Insight is not a rogue skill"));
        nexts.Should().Be(0);
    }

    [Fact]
    public void Render_WithNoClassSaved_AsksToPickOne()
    {
        // Arrange
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014" });

        // Act
        var cut = RenderStep();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("No class selected");
    }

    [Fact]
    public void Render_WhenTheDraftIsGone_SaysSo()
    {
        // Arrange
        Api.NotFound(DraftPath());

        // Act
        var cut = RenderStep();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("Draft not found");
    }

    private static CharacterDraftDetailsApiResponse Draft(
        string classId = "rogue",
        List<string>? skills = null,
        List<List<string>>? toolPicks = null) => new()
        {
            Id = DraftId,
            Edition = "2014",
            ClassId = classId,
            BackgroundId = "criminal",
            SelectedClassProficiencyIds = skills ?? [],
            ToolPicks = toolPicks ?? [],
        };

    private static List<string> RowNames(IRenderedComponent<ProficienciesStep> cut) =>
        cut.FindAll("[data-testid=choice-row]").Select(r => r.QuerySelector("span.font-semibold")!.TextContent).ToList();

    private static IElement Row(IRenderedComponent<ProficienciesStep> cut, string name) =>
        cut.FindAll("[data-testid=choice-row]").Single(r => r.QuerySelector("span.font-semibold")!.TextContent == name);

    private static IElement ToolRow(IRenderedComponent<ProficienciesStep> cut, int choice, string name) =>
        cut.FindAll("[data-testid=choice-section]")[choice]
            .QuerySelectorAll("[data-testid=choice-row]")
            .Single(r => r.QuerySelector("span.font-semibold")!.TextContent == name);

    private void ArrangeLoad(CharacterDraftDetailsApiResponse draft, ClassApiResponse classResponse, List<ToolChoiceApiResponse> toolChoices)
    {
        Api.Get(DraftPath(), draft);
        Api.Get($"/api/classes/{classResponse.Id}", classResponse);
        Api.Get("/api/backgrounds/criminal", Background("criminal", "Criminal", null, new BackgroundProficiencyApiResponse("stealth", "Stealth", "skill")));
        Api.Get(DraftPath(ToolChoicesPath), toolChoices);
    }

    private IRenderedComponent<ProficienciesStep> RenderStep(Action? onNext = null)
    {
        var cut = Render<ProficienciesStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 7)
            .Add(p => p.OnNext, () => onNext?.Invoke()));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }
}
