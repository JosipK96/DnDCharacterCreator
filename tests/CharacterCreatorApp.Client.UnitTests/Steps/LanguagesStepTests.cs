using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Languages;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// The languages step: it asks for as many as the API counts (the race's, the subrace's and the
/// background's together), leaves out the languages the API says are known, and saves the picks.
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

    [Fact]
    public void Render_AHighElfAcolyte_AsksForThreeAndLeavesOutTheLanguagesAlreadyKnown()
    {
        // Arrange - a high elf picks one, an acolyte two, and an elf knows Common and Elvish
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf", BackgroundId = "acolyte" }, languages: 3);

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
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf" }, languages: 0);

        // Act
        var cut = RenderStep();

        // Assert
        cut.Markup.Should().Contain("No Language Choices");
        cut.FindAll("[data-testid=choice-row]").Should().BeEmpty();
        Button(cut, "Next: Ability Scores").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Next_WhileTheStepIsStillLoading_IsDisabled()
    {
        // Arrange - before the count arrives it reads 0, which would let no pick through
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf" }, languages: 1);
        var owed = Api.Hold(DraftPath("choices-owed"));

        // Act
        var cut = Render<LanguagesStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 5));

        // Assert
        cut.WaitForAssertion(() => Api.Requests.Should().Contain($"GET {DraftPath("choices-owed")}"));
        Button(cut, "Next: Ability Scores").HasAttribute("disabled").Should().BeTrue();
        owed.SetResult();
    }

    [Fact]
    public void Click_AsManyAsAllowed_DisablesTheRestAndSendsThePicks()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf", BackgroundId = "acolyte" }, languages: 3);
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
    public void Click_TheLastPick_DisablesTheRest()
    {
        // Arrange - a high elf with no background picks one
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf" }, languages: 1);
        var cut = RenderStep();

        // Act
        Row(cut, "Giant").Click();

        // Assert
        Row(cut, "Abyssal").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Click_APickAgain_TakesItBackAndFreesTheRest()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf" }, languages: 1);
        var cut = RenderStep();
        Row(cut, "Giant").Click();

        // Act
        Row(cut, "Giant").Click();

        // Assert
        Row(cut, "Giant").GetAttribute("aria-pressed").Should().Be("false");
        Row(cut, "Abyssal").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Render_WithLanguagesSaved_ShowsThemPicked()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf", SelectedLanguageIds = ["giant"],
        }, languages: 1);

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
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2014", RaceId = "elf", SubraceId = "high-elf", SelectedLanguageIds = ["giant"] }, languages: 1);
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

    // The API counts the picks; an elf knows Common and Elvish
    private void ArrangeLoad(CharacterDraftDetailsApiResponse draft, int languages)
    {
        Api.Get(DraftPath(), draft);
        Api.Get(DraftPath("choices-owed"), Owed(languages: languages, knownLanguageIds: ["common", "elvish"]));
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
