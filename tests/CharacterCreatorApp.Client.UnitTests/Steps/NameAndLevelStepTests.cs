using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

public class NameAndLevelStepTests : WizardStepTestBase
{
    private const string NameRoute = "PUT /api/character-drafts/0b6f3a52-7d2e-4c1a-9a51-3f0c2d7e8b14/name-and-level";
    private const string BooksRoute = "PUT /api/character-drafts/0b6f3a52-7d2e-4c1a-9a51-3f0c2d7e8b14/books";

    private static readonly List<BookApiResponse> Books2024 =
        [new("XPHB", "Player's Handbook (2024)", "2024", true), new("EFA", "Eberron: Forge of the Artificer", "2024", false)];

    private static readonly List<BookApiResponse> Books2014 =
        [new("PHB", "Player's Handbook", "2014", true), new("XGE", "Xanathar's Guide to Everything", "2014", false)];

    private readonly List<(int Level, string Edition, IReadOnlyList<string> Sources)> _movedOn = [];

    [Fact]
    public void Render_ForANewDraft_TicksThePlayersHandbookAndLocksIt()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2024", AllowedSources = ["XPHB"] });

        // Act
        var cut = RenderStep();

        // Assert
        var phb = BookBox(cut, "Player's Handbook (2024)");
        phb.HasAttribute("checked").Should().BeTrue();
        phb.HasAttribute("disabled").Should().BeTrue();
        BookBox(cut, "Eberron: Forge of the Artificer").HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Next_WithABookTicked_SavesItWithoutThePlayersHandbookAndPassesThePlayersHandbookFirst()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Edition = "2024", AllowedSources = ["XPHB"] });
        Api.Put(DraftPath("name-and-level"));
        Api.Put(DraftPath("books"));
        var cut = RenderStep();
        cut.Find("input[type=text]").Input("Tordek");
        cut.Find("select").Change("3");
        BookBox(cut, "Eberron: Forge of the Artificer").Change(true);

        // Act
        Button(cut, "Next: Race").Click();

        // Assert
        Api.Requests.TakeLast(2).Should().Equal(NameRoute, BooksRoute);
        var named = Api.BodyOf<SetNameAndLevelApiRequest>(NameRoute);
        (named.Name, named.Level, named.Edition).Should().Be(("Tordek", 3, "2024"));
        Api.BodyOf<SetAllowedBooksApiRequest>(BooksRoute).Books.Should().Equal("EFA");
        _movedOn.Should().ContainSingle();
        _movedOn[0].Level.Should().Be(3);
        _movedOn[0].Edition.Should().Be("2024");
        _movedOn[0].Sources.Should().Equal("XPHB", "EFA");
    }

    [Fact]
    public void ChooseEdition_TheOtherOne_LoadsItsBooksUntickedAndSavesNone()
    {
        // Arrange: a draft saved under 2024 with a book ticked
        ArrangeLoad(new CharacterDraftDetailsApiResponse { Id = DraftId, Name = "Tordek", Level = 1, Edition = "2024", AllowedSources = ["XPHB", "EFA"] });
        Api.Get("/api/books?edition=2014", Books2014);
        Api.Put(DraftPath("name-and-level"));
        Api.Put(DraftPath("books"));
        var cut = RenderStep();
        BookBox(cut, "Eberron: Forge of the Artificer").HasAttribute("checked").Should().BeTrue();

        // Act
        Button(cut, "2014 rules").Click();
        BookBox(cut, "Xanathar's Guide to Everything").HasAttribute("checked").Should().BeFalse();
        Button(cut, "Next: Race").Click();

        // Assert
        Api.BodyOf<SetNameAndLevelApiRequest>(NameRoute).Edition.Should().Be("2014");
        Api.BodyOf<SetAllowedBooksApiRequest>(BooksRoute).Books.Should().BeEmpty();
        _movedOn.Should().ContainSingle().Which.Sources.Should().Equal("PHB");
    }

    [Fact]
    public void ToggleBook_UntickingASavedBookOfADraftWithChoices_WarnsTheyAreCleared()
    {
        // Arrange
        ArrangeLoad(new CharacterDraftDetailsApiResponse
        {
            Id = DraftId, Name = "Tordek", Level = 1, Edition = "2024", AllowedSources = ["XPHB", "EFA"], RaceId = "dwarf-xphb",
        });
        var cut = RenderStep();
        cut.Markup.Should().NotContain("Choices made from a book you untick are cleared.");

        // Act
        BookBox(cut, "Eberron: Forge of the Artificer").Change(false);

        // Assert
        cut.Markup.Should().Contain("Choices made from a book you untick are cleared.");
    }

    private static IElement BookBox(IRenderedComponent<NameAndLevelStep> cut, string name) =>
        cut.FindAll("label").Single(l => l.QuerySelector("span")?.TextContent == name).QuerySelector("input[type=checkbox]")!;

    private void ArrangeLoad(CharacterDraftDetailsApiResponse draft)
    {
        Api.Get(DraftPath(), draft);
        Api.Get("/api/books?edition=2024", Books2024);
    }

    private IRenderedComponent<NameAndLevelStep> RenderStep()
    {
        var cut = Render<NameAndLevelStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 1)
            .Add(p => p.OnNext, choice => _movedOn.Add(choice)));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }
}
