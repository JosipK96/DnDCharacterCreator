using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Pages.CharacterCreation.Components;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

public class PointBuyComponentTests : BunitContext
{
    private readonly List<List<int>> _handedBack = [];

    [Fact]
    public void Render_Initially_Has27PointsAndEveryScoreAt8()
    {
        // Act
        var cut = RenderPointBuy();

        // Assert
        PointsLeft(cut).Should().Be("27");
        Scores(cut).Should().Equal(8, 8, 8, 8, 8, 8);
        cut.FindAll("button").Where(b => b.TextContent.Trim() == "−").Should().OnlyContain(b => b.HasAttribute("disabled"));
        UseScoresButton(cut).TextContent.Trim().Should().Be("Spend all 27 points to continue");
        UseScoresButton(cut).HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Increase_From13To14_CostsTwoPoints()
    {
        // Arrange
        var cut = RenderPointBuy();
        Raise(cut, "Strength", 5);
        PointsLeft(cut).Should().Be("22");

        // Act
        Raise(cut, "Strength", 1);

        // Assert
        PointsLeft(cut).Should().Be("20");
        Scores(cut).Should().Equal(14, 8, 8, 8, 8, 8);
    }

    [Fact]
    public void Increase_At15_IsDisabled()
    {
        // Act
        var cut = RenderPointBuy();
        Raise(cut, "Strength", 7);

        // Assert
        Scores(cut).Should().Equal(15, 8, 8, 8, 8, 8);
        PointsLeft(cut).Should().Be("18");
        PlusOf(cut, "Strength").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Increase_WithFewerPointsLeftThanTheNextScoreCosts_IsDisabled()
    {
        // Arrange: 9 + 9 + 7 + 1 spent, so 1 point is left
        var cut = RenderPointBuy();
        Raise(cut, "Strength", 7);
        Raise(cut, "Dexterity", 7);
        Raise(cut, "Constitution", 6);
        Raise(cut, "Intelligence", 1);

        // Assert: 14 to 15 costs 2, 8 to 9 costs 1
        PointsLeft(cut).Should().Be("1");
        PlusOf(cut, "Constitution").HasAttribute("disabled").Should().BeTrue();
        PlusOf(cut, "Wisdom").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void UseTheseScores_WithEveryPointSpent_HandsBackTheSixScores()
    {
        // Arrange
        var cut = RenderPointBuy();
        Raise(cut, "Strength", 7);
        Raise(cut, "Dexterity", 7);
        Raise(cut, "Constitution", 7);

        // Act
        UseScoresButton(cut).Click();

        // Assert
        PointsLeft(cut).Should().Be("0");
        _handedBack.Should().ContainSingle().Which.Should().Equal(15, 15, 15, 8, 8, 8);
    }

    private static string PointsLeft(IRenderedComponent<PointBuyComponent> cut) => cut.Find("span.text-2xl").TextContent.Trim();

    private static IEnumerable<int> Scores(IRenderedComponent<PointBuyComponent> cut) =>
        cut.FindAll("div.text-3xl").Select(d => int.Parse(d.TextContent.Trim(), System.Globalization.CultureInfo.InvariantCulture));

    private static IElement PlusOf(IRenderedComponent<PointBuyComponent> cut, string ability) =>
        cut.FindAll("h4").Single(h => h.TextContent.Trim() == ability).ParentElement!
            .QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "+");

    private static void Raise(IRenderedComponent<PointBuyComponent> cut, string ability, int times)
    {
        for (var i = 0; i < times; i++)
        {
            PlusOf(cut, ability).Click();
        }
    }

    private static IElement UseScoresButton(IRenderedComponent<PointBuyComponent> cut) => cut.FindAll("button")[^1];

    private IRenderedComponent<PointBuyComponent> RenderPointBuy() =>
        Render<PointBuyComponent>(parameters => parameters
            .Add(p => p.Scores, [])
            .Add(p => p.ScoresChanged, scores => _handedBack.Add([.. scores])));
}
