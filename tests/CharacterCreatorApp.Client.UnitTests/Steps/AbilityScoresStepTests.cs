using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;
using CharacterCreatorApp.Client.UnitTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

public class AbilityScoresStepTests : WizardStepTestBase
{
    private const string SaveRoute = "PUT /api/character-drafts/0b6f3a52-7d2e-4c1a-9a51-3f0c2d7e8b14/ability-scores";

    private int _movedOn;

    [Fact]
    public void Next_WithTheStandardArrayAssigned_SavesItAsTheStandardArray()
    {
        // Arrange
        Api.Put(DraftPath("ability-scores"));
        var cut = RenderStep();
        Button(cut, "Standard Array").Click();
        Button(cut, "Generate Standard Array").Click();
        AssignInOrder(cut);

        // Act
        Button(cut, "Next: Proficiencies").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoresApiRequest>(SaveRoute);
        sent.Method.Should().Be("standard-array");
        sent.Scores.Should().Equal(new Dictionary<string, int>
        {
            ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 12, ["WIS"] = 10, ["CHA"] = 8,
        });
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void Next_BeforeEveryAbilityHasAScore_IsDisabled()
    {
        // Arrange
        var cut = RenderStep();
        Button(cut, "Standard Array").Click();
        Button(cut, "Generate Standard Array").Click();

        // Act: five of the six
        for (var i = 0; i < 5; i++)
        {
            ScoreCards(cut)[i].Click();
            Button(cut, "Assign ").Click();
        }

        // Assert
        Button(cut, "Next: Proficiencies").HasAttribute("disabled").Should().BeTrue();
        Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public void Next_WithPointBuyScores_SavesThemAsPointBuy()
    {
        // Arrange
        Api.Put(DraftPath("ability-scores"));
        var cut = RenderStep();
        Button(cut, "Point Buy").Click();

        foreach (var ability in new[] { "Strength", "Dexterity", "Constitution" })
        {
            for (var i = 0; i < 7; i++)
            {
                cut.FindAll("h4").Single(h => h.TextContent.Trim() == ability).ParentElement!
                    .QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "+").Click();
            }
        }

        Button(cut, "Use These Scores").Click();
        Button(cut, "Use Point Buy Scores").Click();
        AssignInOrder(cut);

        // Act
        Button(cut, "Next: Proficiencies").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoresApiRequest>(SaveRoute);
        sent.Method.Should().Be("point-buy");
        sent.Scores.Should().Equal(new Dictionary<string, int>
        {
            ["STR"] = 15, ["DEX"] = 15, ["CON"] = 15, ["INT"] = 8, ["WIS"] = 8, ["CHA"] = 8,
        });
        _movedOn.Should().Be(1);
    }

    [Fact]
    public void RollDice_ForEachAbility_KeepsTheHighestThreeOfFourD6AndSavesThemAsRolled()
    {
        // Arrange: the 3D dice are JavaScript; every roll comes back 3, 6, 2, 5, so 2 is dropped
        JSInterop.Setup<bool>("eval", "typeof window.initFantasticDice === 'function'").SetResult(true);
        JSInterop.SetupVoid("initFantasticDice").SetVoidResult();
        JSInterop.Setup<int[]>("rollFantasticDice", "4d6").SetResult([3, 6, 2, 5]);
        Api.Put(DraftPath("ability-scores"));
        var cut = RenderStep();
        Button(cut, "Roll Dice").Click();

        // Act: the component waits 1.5 seconds between rolls, so six take about nine
        Button(cut, "Roll All Ability Scores").Click();
        cut.WaitForAssertion(() => ScoreCards(cut).Should().HaveCount(6), TimeSpan.FromSeconds(20));

        // The cards can show before the roll's last callbacks have finished, and a pick made in
        // between did not stick here; picking until it does waits them out
        cut.WaitForAssertion(
            () =>
            {
                ScoreCards(cut)[0].Click();
                Buttons(cut, "Assign ").Should().NotBeEmpty();
            },
            TimeSpan.FromSeconds(5));
        AssignInOrder(cut);
        Button(cut, "Next: Proficiencies").Click();

        // Assert
        var sent = Api.BodyOf<SetAbilityScoresApiRequest>(SaveRoute);
        sent.Method.Should().Be("rolled");
        sent.Scores.Should().Equal(new Dictionary<string, int>
        {
            ["STR"] = 14, ["DEX"] = 14, ["CON"] = 14, ["INT"] = 14, ["WIS"] = 14, ["CHA"] = 14,
        });
        cut.FindAll("div.line-through").Select(d => d.TextContent.Trim()).Should().Equal(Enumerable.Repeat("2", 6));
    }

    [Fact]
    public void Next_WhenTheServerRefusesTheScores_ShowsItsMessageAndStays()
    {
        // Arrange
        Api.RefusePut(DraftPath("ability-scores"), "Ability scores must be between 3 and 20");
        var cut = RenderStep();
        Button(cut, "Standard Array").Click();
        Button(cut, "Generate Standard Array").Click();
        AssignInOrder(cut);

        // Act
        Button(cut, "Next: Proficiencies").Click();

        // Assert
        cut.Find("div.bg-red-900 p").TextContent.Should().Be("Failed to save ability scores: Ability scores must be between 3 and 20");
        _movedOn.Should().Be(0);
    }

    // The score cards are the buttons holding a big number; the first unassigned ability takes each
    private static List<IElement> ScoreCards(IRenderedComponent<AbilityScoresStep> cut) =>
        cut.FindAll("button").Where(b => b.QuerySelector("div.text-4xl") is not null).ToList();

    private static void AssignInOrder(IRenderedComponent<AbilityScoresStep> cut)
    {
        for (var i = 0; i < 6; i++)
        {
            ScoreCards(cut)[i].Click();
            Button(cut, "Assign ").Click();
        }
    }

    private IRenderedComponent<AbilityScoresStep> RenderStep() =>
        Render<AbilityScoresStep>(parameters => parameters
            .Add(p => p.DraftId, DraftId)
            .Add(p => p.Step, 6)
            .Add(p => p.OnNext, () => _movedOn++));
}
