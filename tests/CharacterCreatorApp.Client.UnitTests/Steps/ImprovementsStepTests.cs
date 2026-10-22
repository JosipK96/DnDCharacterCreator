using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

public class ImprovementsStepTests : WizardStepTestBase
{
    private static readonly string ImprovementsRoute = SaveRoute("ability-score-improvements");
    private static readonly string StylesRoute = SaveRoute("fighting-styles");

    private static readonly ClassApiResponse Fighter = Class("fighter-phb", "Fighter", 4, 6, 8, 12, 14, 16, 19);
    private static readonly FeatApiResponse Alert = Feat("alert-phb", "Alert");
    private static readonly FeatApiResponse Athlete = Feat("athlete-phb", "Athlete", increase: new(["STR", "DEX"], 1, 20));
    private static readonly FeatApiResponse Skilled = Feat("skilled-phb", "Skilled", choice: new(FeatChoiceApiResponse.Skills, 2, ["arcana", "history", "stealth"]));
    private static readonly FeatApiResponse Archery = Feat("archery-xphb", "Archery", category: "FS");
    private static readonly FeatApiResponse Defense = Feat("defense-xphb", "Defense", category: "FS");

    private static readonly List<ProficiencyApiResponse> Skills =
        [Skill("arcana", "Arcana"), Skill("history", "History"), Skill("stealth", "Stealth")];

    private int _movedOn;

    [Fact]
    public void Render_ForALevel6Fighter_OffersOneImprovementPerAsiLevelReached()
    {
        // Arrange
        ArrangeLoad(Draft(), [Alert]);

        // Act
        var cut = RenderStep(level: 6);

        // Assert
        cut.FindAll("h3.font-bold").Select(h => h.TextContent.Trim()).Should().Equal("Improvement #1", "Improvement #2");
        Button(cut, "Next: Spells").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Increase_WithTwoPointsSpent_DisablesEveryIncrease()
    {
        // Arrange
        ArrangeLoad(Draft(), [Alert]);
        var cut = RenderStep(level: 4);

        // Act
        PlusOf(cut, "Strength").Click();
        PlusOf(cut, "Dexterity").Click();

        // Assert
        cut.Markup.Should().Contain("0 point(s) remaining");
        cut.FindAll("button").Where(b => b.TextContent.Trim() == "+").Should().OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Increase_TwiceInOneAbility_StopsAtTwo()
    {
        // Arrange
        ArrangeLoad(Draft(), [Alert]);
        var cut = RenderStep(level: 4);

        // Act
        PlusOf(cut, "Strength").Click();
        PlusOf(cut, "Strength").Click();

        // Assert
        PlusOf(cut, "Strength").HasAttribute("disabled").Should().BeTrue();
        Button(cut, "Next: Spells").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Next_WithTwoPointsInAnAbility_SavesTheImprovementAndMovesOn()
    {
        // Arrange
        ArrangeLoad(Draft(), [Alert]);
        Api.Put(DraftPath("ability-score-improvements"));
        var cut = RenderStep(level: 4);
        PlusOf(cut, "Strength").Click();
        PlusOf(cut, "Strength").Click();

        // Act
        Button(cut, "Next: Spells").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoreImprovementsApiRequest>(ImprovementsRoute);
        sent.Improvements.Should().ContainSingle().Which.Should().Equal(new Dictionary<string, int> { ["strength"] = 2 });
        sent.FeatIds.Should().BeEmpty();
        sent.FeatAbilityIncreases.Should().BeEmpty();
        sent.FeatProficiencyPicks.Should().BeEmpty();
        Api.Requests.Should().NotContain(StylesRoute);
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Next_WithTheSlotTakingAFeat_SendsTheFeatInsteadOfAnImprovement()
    {
        // Arrange
        ArrangeLoad(Draft(), [Alert, Athlete]);
        Api.Put(DraftPath("ability-score-improvements"));
        var cut = RenderStep(level: 4);
        ModeButton(cut, "Feat").Click();
        Button(cut, "Alert").Click();

        // Act
        Button(cut, "Next: Spells").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoreImprovementsApiRequest>(ImprovementsRoute);
        sent.Improvements.Should().BeEmpty();
        sent.FeatIds.Should().Equal("alert-phb");
        sent.FeatAbilityIncreases.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Fact]
    public void Next_WithAthlete_WaitsForTheAbilityItRaisesAndSendsIt()
    {
        // Arrange
        ArrangeLoad(Draft(), [Alert, Athlete]);
        Api.Put(DraftPath("ability-score-improvements"));
        var cut = RenderStep(level: 4);
        ModeButton(cut, "Feat").Click();
        Button(cut, "Athlete").Click();
        Button(cut, "Next: Spells").HasAttribute("disabled").Should().BeTrue();

        // Act
        cut.Find("select").Change("DEX");
        Button(cut, "Next: Spells").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoreImprovementsApiRequest>(ImprovementsRoute);
        sent.FeatIds.Should().Equal("athlete-phb");
        sent.FeatAbilityIncreases.Should().ContainSingle().Which.Should().Equal(new Dictionary<string, int> { ["DEX"] = 1 });
    }

    [Fact]
    public void Next_WithSkilled_WaitsForEverySkillAndSendsThePicks()
    {
        // Arrange
        ArrangeLoad(Draft(), [Alert, Skilled]);
        Api.Put(DraftPath("ability-score-improvements"));
        var cut = RenderStep(level: 4);
        ModeButton(cut, "Feat").Click();
        Button(cut, "Skilled").Click();

        // Act
        cut.FindAll("select")[0].Change("stealth");
        Button(cut, "Next: Spells").HasAttribute("disabled").Should().BeTrue();
        cut.FindAll("select")[1].Change("arcana");
        Button(cut, "Next: Spells").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoreImprovementsApiRequest>(ImprovementsRoute);
        sent.FeatIds.Should().Equal("skilled-phb");
        sent.FeatProficiencyPicks.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FeatProficiencyPickApiModel { FeatId = "skilled-phb", ProficiencyIds = ["stealth", "arcana"] });
    }

    [Fact]
    public void Render_WithASkillPickedInOneSelect_LeavesItOutOfTheOthers()
    {
        // Arrange
        ArrangeLoad(Draft(), [Skilled]);
        var cut = RenderStep(level: 4);
        ModeButton(cut, "Feat").Click();
        Button(cut, "Skilled").Click();

        // Act
        cut.FindAll("select")[0].Change("history");

        // Assert
        OptionValues(cut.FindAll("select")[1]).Should().Equal("arcana", "stealth");
    }

    [Fact]
    public void Next_ForAVariantHuman_SendsTheRaceFeatWithNoImprovementToChoose()
    {
        // Arrange
        var human = Race("human-phb", "Human", subraces: Subrace("human-variant-phb", "Variant Human", "human-phb", featChoiceCount: 1));
        ArrangeLoad(Draft(level: 1, raceId: "human-phb", subraceId: "human-variant-phb"), [Alert], races: [human]);
        Api.Put(DraftPath("ability-score-improvements"));
        var cut = RenderStep(level: 1);
        cut.FindAll("h3.font-bold").Select(h => h.TextContent.Trim()).Should().Equal("Variant Human Feat");
        Buttons(cut, "Ability Scores").Should().BeEmpty();

        // Act
        Button(cut, "Alert").Click();
        Button(cut, "Next: Spells").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoreImprovementsApiRequest>(ImprovementsRoute);
        sent.Improvements.Should().BeEmpty();
        sent.FeatIds.Should().Equal("alert-phb");
    }

    [Fact]
    public void Next_ForAFighterOwedAStyle_WaitsForItThenSavesTheImprovementsBeforeTheStyle()
    {
        // Arrange
        ArrangeLoad(Draft(level: 1), [Alert], stylesOwed: 1, styles: [Archery, Defense]);
        Api.Put(DraftPath("ability-score-improvements"));
        Api.Put(DraftPath("fighting-styles"));
        var cut = RenderStep(level: 1);
        cut.FindAll("h3").Select(h => h.TextContent.Trim()).Should().Equal("Fighting Style");
        Button(cut, "Next: Spells").HasAttribute("disabled").Should().BeTrue();

        // Act
        cut.Find("select").Change("defense-xphb");
        Button(cut, "Next: Spells").Click();

        // Assert
        Api.Requests.TakeLast(2).Should().Equal(ImprovementsRoute, StylesRoute);
        Api.BodyOf<ChooseFightingStylesApiRequest>(StylesRoute).FeatIds.Should().Equal("defense-xphb");
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Render_WithAStyleTakenAsTheImprovementsFeat_LeavesItOutOfTheStyles()
    {
        // Arrange: a 2024 fighter may take another Fighting Style as a feat
        ArrangeLoad(Draft(level: 4), [Archery, Defense], stylesOwed: 1, styles: [Archery, Defense]);
        var cut = RenderStep(level: 4);

        // Act
        ModeButton(cut, "Feat").Click();
        Button(cut, "Archery").Click();

        // Assert
        OptionValues(cut.Find("select")).Should().Equal("defense-xphb");
    }

    [Fact]
    public void Render_ForABackgroundFeatAskingForSkills_OffersOnlySkillsTheCharacterLacksAndSendsThemFirst()
    {
        // Arrange: a background that gives History and Skilled
        var background = Background("scholar-xphb", "Scholar", new("skilled-phb", "Skilled", true), new BackgroundProficiencyApiResponse("history", "History", "skill"));
        ArrangeLoad(Draft(level: 4, backgroundId: "scholar-xphb"), [Alert, Skilled], background: background);
        Api.Put(DraftPath("ability-score-improvements"));
        var cut = RenderStep(level: 4);
        cut.Markup.Should().Contain("Background Feat: Skilled");
        OptionValues(cut.FindAll("select")[0]).Should().Equal("arcana", "stealth");

        // Act
        cut.FindAll("select")[0].Change("arcana");
        cut.FindAll("select")[1].Change("stealth");
        PlusOf(cut, "Constitution").Click();
        PlusOf(cut, "Constitution").Click();
        Button(cut, "Next: Spells").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoreImprovementsApiRequest>(ImprovementsRoute);
        sent.Improvements.Should().ContainSingle().Which.Should().Equal(new Dictionary<string, int> { ["constitution"] = 2 });
        sent.FeatProficiencyPicks.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FeatProficiencyPickApiModel { FeatId = "skilled-phb", ProficiencyIds = ["arcana", "stealth"] });
    }

    [Fact]
    public void Next_WhenTheServerRefusesTheImprovements_ShowsItsMessageAndSavesNoStyle()
    {
        // Arrange
        ArrangeLoad(Draft(level: 4), [Alert], stylesOwed: 1, styles: [Archery, Defense]);
        Api.RefusePut(DraftPath("ability-score-improvements"), "You don't meet the feat's prerequisites");
        var cut = RenderStep(level: 4);
        cut.Find("select").Change("archery-xphb");
        PlusOf(cut, "Wisdom").Click();
        PlusOf(cut, "Wisdom").Click();

        // Act
        Button(cut, "Next: Spells").Click();

        // Assert
        cut.Find("div.bg-red-900 p").TextContent.Should().Be("Failed to save improvements: You don't meet the feat's prerequisites");
        Api.Requests.Should().NotContain(StylesRoute);
        _movedOn.Should().Be(0);
    }

    private static CharacterDraftDetailsApiResponse Draft(int level = 4, string? raceId = null, string? subraceId = null, string? backgroundId = null) => new()
    {
        Id = DraftId,
        Level = level,
        Edition = "2014",
        AllowedSources = ["PHB"],
        ClassId = Fighter.Id,
        RaceId = raceId,
        SubraceId = subraceId,
        BackgroundId = backgroundId,
    };

    private static IElement PlusOf(IRenderedComponent<ImprovementsStep> cut, string ability) =>
        cut.FindAll("p").First(p => p.TextContent.Trim() == ability).ParentElement!
            .QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "+");

    private static IElement ModeButton(IRenderedComponent<ImprovementsStep> cut, string mode) =>
        cut.FindAll("button").Single(b => b.TextContent.Trim() == mode);

    private static IEnumerable<string> OptionValues(IElement select) =>
        select.QuerySelectorAll("option").Select(o => o.GetAttribute("value")!).Where(v => v.Length > 0);

    private void ArrangeLoad(
        CharacterDraftDetailsApiResponse draft,
        List<FeatApiResponse> feats,
        int stylesOwed = 0,
        List<FeatApiResponse>? styles = null,
        List<RaceApiResponse>? races = null,
        BackgroundApiResponse? background = null)
    {
        Api.Get(DraftPath(), draft);
        Api.Get($"/api/classes/{Fighter.Id}", Fighter);
        Api.Get("/api/feats?edition=2014&sources=PHB", feats);
        Api.Get("/api/proficiencies", Skills);
        Api.Get(DraftPath("fighting-styles"), new FightingStylesApiResponse(stylesOwed, styles ?? []));

        if (races is not null)
        {
            Api.Get("/api/races/get-list?edition=2014", races);
        }

        if (background is not null)
        {
            Api.Get($"/api/backgrounds/{background.Id}", background);
        }
    }

    private IRenderedComponent<ImprovementsStep> RenderStep(int level)
    {
        var cut = Render<ImprovementsStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 8)
            .Add(p => p.CharacterLevel, level)
            .Add(p => p.OnNext, () => _movedOn++));

        cut.WaitForAssertion(() => cut.FindAll("div.animate-spin").Should().BeEmpty());
        return cut;
    }
}
