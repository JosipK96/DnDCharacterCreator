using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses.Backgrounds;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Languages;
using CharacterCreatorApp.Contracts.Responses.Races;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The languages step: how many it asks for (the race's, the subrace's and the background's
/// together), the languages already known left out of the choices, and what it saves.
/// </summary>
public class LanguagesStepTests : WizardStepTestBase
{
    private static readonly List<LanguageApiResponse> Languages =
    [
        new("common", "Common", "Standard"),
        new("elvish", "Elvish", "Standard"),
        new("dwarvish", "Dwarvish", "Standard"),
        new("giant", "Giant", "Standard"),
        new("abyssal", "Abyssal", "Exotic"),
    ];

    // An elf knows Common and Elvish; a high elf picks one more language
    private static readonly RaceApiResponse Elf = Race("elf", "Elf") with
    {
        Languages = ["Common", "Elvish"],
        Subraces = [Subrace("high-elf", "High Elf", "elf") with { LanguageChoiceCount = 1 }],
    };

    // An acolyte picks two languages
    private static readonly BackgroundApiResponse Acolyte = Background("acolyte", "Acolyte") with { LanguageChoiceCount = 2 };

    [Fact]
    public void Render_AHighElfAcolyte_AsksForThreeAndLeavesOutTheLanguagesAlreadyKnown()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf", BackgroundId = "acolyte" });

        // Act
        var cut = RenderStep();

        // Assert
        cut.Markup.Should().Contain("Choose 3 additional language(s)");
        cut.Find("strong").TextContent.Should().Be("Common, Elvish");
        RowNames(cut).Should().Equal("Abyssal", "Dwarvish", "Giant");
    }

    [Fact]
    public void Render_WithNoLanguageToChoose_SaysSoAndLetsThePlayerOn()
    {
        // Arrange - an elf with no subrace and no background
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf" });

        // Act
        var cut = RenderStep();

        // Assert
        cut.Markup.Should().Contain("No Language Choices");
        cut.FindAll("[data-testid=choice-row]").Should().BeEmpty();
        Button(cut, "Next: Ability Scores").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Click_AsManyAsAllowed_DisablesTheRestAndSendsThePicks()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf", BackgroundId = "acolyte" });
        Api.Put(DraftPath("languages"));
        var nexts = 0;
        var cut = RenderStep(() => nexts++);
        Button(cut, "Next: Ability Scores").HasAttribute("disabled").Should().BeTrue();

        // Act
        Row(cut, "Giant").Click();
        Row(cut, "Abyssal").Click();
        Row(cut, "Dwarvish").Click();
        Button(cut, "Next: Ability Scores").Click();

        // Assert
        cut.WaitForAssertion(() => nexts.Should().Be(1));
        Api.BodyOf<ChooseLanguagesApiRequest>(SaveRoute("languages")).LanguageIds.Should().BeEquivalentTo(["giant", "abyssal", "dwarvish"]);
    }

    [Fact]
    public void Click_ARowWhenEveryPickIsMade_IsDisabledUntilOneIsTakenBack()
    {
        // Arrange - a high elf with no background picks one
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf" });
        var cut = RenderStep();

        // Act
        Row(cut, "Giant").Click();

        // Assert
        Row(cut, "Abyssal").HasAttribute("disabled").Should().BeTrue();
        Row(cut, "Giant").Click();
        Row(cut, "Abyssal").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Render_WithLanguagesSaved_ShowsThemPicked()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf", SelectedLanguageIds = ["giant"],
        });

        // Act
        var cut = RenderStep();

        // Assert
        Row(cut, "Giant").GetAttribute("aria-pressed").Should().Be("true");
        Row(cut, "Dwarvish").GetAttribute("aria-pressed").Should().Be("false");
        Button(cut, "Next: Ability Scores").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Next_WhenTheApiRefuses_ShowsItsMessageAndStays()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf", SelectedLanguageIds = ["giant"] });
        Api.RefusePut(DraftPath("languages"), "That language is already known");
        var nexts = 0;
        var cut = RenderStep(() => nexts++);

        // Act
        Button(cut, "Next: Ability Scores").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should().Contain("That language is already known"));
        nexts.Should().Be(0);
    }

    [Fact]
    public void Render_WhenTheDraftIsGone_SaysSo()
    {
        // Arrange
        Api.NotFound(DraftPath());

        // Act
        var cut = RenderStep();

        // Assert
        cut.Find("[role=alert]").TextContent.Should().Contain("Draft not found.");
    }

    private static List<string> RowNames(IRenderedComponent<LanguagesStep> cut) =>
        cut.FindAll("[data-testid=choice-row]").Select(r => r.QuerySelector("span.font-semibold")!.TextContent).ToList();

    private static AngleSharp.Dom.IElement Row(IRenderedComponent<LanguagesStep> cut, string name) =>
        cut.FindAll("[data-testid=choice-row]").Single(r => r.QuerySelector("span.font-semibold")!.TextContent == name);

    private void ArrangeLoad(CharacterDraftDetailsApiResponse draft)
    {
        Api.Get(DraftPath(), draft);
        Api.Get("/api/races/get-list?edition=2014", new List<RaceApiResponse> { Elf });
        Api.Get("/api/backgrounds/acolyte", Acolyte);
        Api.Get("/api/languages/get-list?edition=2014", Languages);
    }

    private IRenderedComponent<LanguagesStep> RenderStep(Action? onNext = null)
    {
        var cut = Render<LanguagesStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 5)
            .Add(p => p.OnNext, () => onNext?.Invoke()));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }
}
