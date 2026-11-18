using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>The class step drawn as rooms: the grid of classes and the picked one's key beside it.</summary>
public class ClassStepTests : WizardStepTestBase
{
    private static readonly ClassApiResponse Fighter = Class("fighter", "Fighter") with
    {
        ClassProficiencies =
        [
            new("light-armor", "Light Armor", "armor"),
            new("shields", "Shields", "armor"),
            new("martial-weapons", "Martial Weapons", "weapon"),
            new("dice-set", "Dice Set", "gaming_set"),
        ],
        ProficiencyChoice = new("skills", 2, [Skill("athletics", "Athletics"), Skill("intimidation", "Intimidation"), Skill("survival", "Survival")]),
    };

    private static readonly ClassApiResponse Wizard = Class("wizard", "Wizard") with { HitDie = "1d6", SavingThrowAbilityIds = ["INT", "WIS"] };

    [Fact]
    public void Render_WithTheFighterSaved_MarksItsRoomAndShowsItsKeyAsTheApiSendsIt()
    {
        // Arrange
        ArrangeLoad("fighter");

        // Act
        var cut = RenderStep(level: 1);

        // Assert
        var fighterRoom = Room(cut, "Fighter");
        fighterRoom.GetAttribute("aria-pressed").Should().Be("true");
        fighterRoom.QuerySelector(".you-are-here").Should().NotBeNull();
        Squash(fighterRoom.TextContent).Should().Contain("Pick 2 of 3 skills");
        Room(cut, "Wizard").GetAttribute("aria-pressed").Should().Be("false");
        Room(cut, "Wizard").QuerySelector(".you-are-here").Should().BeNull();

        var key = cut.Find("[data-testid=key-panel]");
        key.QuerySelector("h3")!.TextContent.Should().Be("Key: Fighter");
        key.QuerySelector("[data-testid=hit-die]")!.GetAttribute("data-die").Should().Be("d10");
        // The hit die's row holds its drawing, checked by data-die above
        Facts(key).Skip(1).Should().Equal(
            ("Saving throws", "Strength, Constitution"),
            ("Armor", "Light Armor, Shields"),
            ("Weapons", "Martial Weapons"),
            ("Tools", "Dice Set"),
            ("Skills", "Pick 2 of Athletics, Intimidation, Survival"),
            ("Subclass", "At level 3"));
    }

    [Fact]
    public void Click_AnotherClass_MovesTheMarkerAndTheKeyToIt()
    {
        // Arrange
        ArrangeLoad("fighter");
        var cut = RenderStep(level: 1);

        // Act
        Room(cut, "Wizard").Click();

        // Assert
        Room(cut, "Wizard").GetAttribute("aria-pressed").Should().Be("true");
        Room(cut, "Fighter").GetAttribute("aria-pressed").Should().Be("false");
        var key = cut.Find("[data-testid=key-panel]");
        key.QuerySelector("h3")!.TextContent.Should().Be("Key: Wizard");
        key.QuerySelector("[data-testid=hit-die]")!.GetAttribute("data-die").Should().Be("d6");
        Facts(key).Should().Contain([("Saving throws", "Intelligence, Wisdom"), ("Armor", "None")]);
    }

    [Fact]
    public void Render_WithNothingSaved_ShowsNoKeyAndKeepsNextDisabled()
    {
        // Arrange
        ArrangeLoad(classId: null);

        // Act
        var cut = RenderStep(level: 1);

        // Assert
        cut.FindAll("[data-testid=key-panel]").Should().BeEmpty();
        cut.FindAll("[data-testid=option-room]").Should().OnlyContain(r => r.GetAttribute("aria-pressed") == "false");
        Button(cut, "Next: Background").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Render_ForEachClass_DrawsItsHitDie()
    {
        // Arrange
        ArrangeLoad(classId: null);

        // Act
        var cut = RenderStep(level: 1);

        // Assert
        Room(cut, "Fighter").QuerySelector("[data-testid=hit-die]")!.GetAttribute("data-die").Should().Be("d10");
        Room(cut, "Wizard").QuerySelector("[data-testid=hit-die]")!.GetAttribute("data-die").Should().Be("d6");
    }

    private static string Squash(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static IElement Room(IRenderedComponent<ClassStep> cut, string name) =>
        cut.FindAll("[data-testid=option-room]").Single(r => r.QuerySelector("span.font-display")!.TextContent.Trim() == name);

    private static List<(string Label, string Value)> Facts(IElement key) =>
        key.QuerySelectorAll("dl > div")
            .Select(f => (f.QuerySelector("dt")!.TextContent.Trim(), Squash(f.QuerySelector("dd")!.TextContent)))
            .ToList();

    private void ArrangeLoad(string? classId)
    {
        Api.Get(DraftPath(), new CharacterDraftDetailsApiResponse { Id = DraftId, ClassId = classId });
        Api.Get("/api/classes/get-list?edition=2014", new List<ClassApiResponse> { Fighter, Wizard });
    }

    private IRenderedComponent<ClassStep> RenderStep(int level)
    {
        var cut = Render<ClassStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 3)
            .Add(p => p.CharacterLevel, level)
            .Add(p => p.Edition, "2014"));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }
}
