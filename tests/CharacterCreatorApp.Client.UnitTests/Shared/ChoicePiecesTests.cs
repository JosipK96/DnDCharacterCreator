using Bunit;

using CharacterCreatorApp.Client.Shared;

using FluentAssertions;

using Microsoft.AspNetCore.Components;

namespace CharacterCreatorApp.Client.UnitTests.Shared;

/// <summary>The wizard's shared pieces: the count box, the option room, the row and the hit die.</summary>
public class ChoicePiecesTests : BunitContext
{
    [Theory]
    [InlineData(4, 6, "4 of 6 chosen: choose 2 more", "border-amber")]
    [InlineData(0, 1, "0 of 1 chosen: choose 1 more", "border-amber")]
    [InlineData(3, 3, "3 of 3 chosen", "border-green")]
    public void ChoiceCount_ForAPartOrAllOfTheChoices_SaysHowManyAndTakesItsColour(int chosen, int total, string text, string border)
    {
        // Act
        var cut = Render<ChoiceCount>(parameters => parameters.Add(p => p.Chosen, chosen).Add(p => p.Total, total));

        // Assert
        var count = cut.Find("[data-testid=choice-count]");
        string.Join(' ', count.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Should().Be(text);
        count.ClassList.Should().Contain(border);
        (count.QuerySelector("svg") is not null).Should().Be(chosen == total, "only a complete count shows the check");
    }

    [Fact]
    public void OptionRoom_WhenPicked_PressesTheButtonAndShowsYouAreHere()
    {
        // Act
        var cut = Render<OptionRoom>(parameters => parameters
            .Add(p => p.Name, "Fighter")
            .Add(p => p.Picked, true)
            .AddChildContent("<span>d10</span>"));

        // Assert
        var room = cut.Find("button");
        room.ClassList.Should().Contain("option-room");
        room.GetAttribute("aria-pressed").Should().Be("true");
        room.QuerySelector(".you-are-here").Should().NotBeNull();
        room.QuerySelector("span.font-display")!.TextContent.Should().Be("Fighter");
    }

    [Fact]
    public void OptionRoom_WhenClicked_CallsBack()
    {
        // Arrange
        var clicks = 0;
        var cut = Render<OptionRoom>(parameters => parameters
            .Add(p => p.Name, "Wizard")
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicks++)));

        // Act
        cut.Find("button").Click();

        // Assert
        clicks.Should().Be(1);
        cut.Find("button").GetAttribute("aria-pressed").Should().Be("false");
        cut.FindAll(".you-are-here").Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, false, "true", true)]
    [InlineData(false, true, "false", false)]
    public void ChoiceRow_PickedOrDisabled_SaysSoAndTicksOnlyAPick(bool picked, bool disabled, string pressed, bool ticked)
    {
        // Act
        var cut = Render<ChoiceRow>(parameters => parameters
            .Add(p => p.Name, "Fire Bolt")
            .Add(p => p.Picked, picked)
            .Add(p => p.Disabled, disabled));

        // Assert
        var row = cut.Find("button");
        row.GetAttribute("aria-pressed").Should().Be(pressed);
        row.HasAttribute("disabled").Should().Be(disabled);
        (row.QuerySelector(".choice-box svg") is not null).Should().Be(ticked);
    }

    [Theory]
    [InlineData("1d6", "rect")]
    [InlineData("1d8", "polygon")]
    [InlineData("d10", "polygon")]
    [InlineData("1d12", "polygon")]
    public void HitDie_ForEachDie_DrawsItsShapeWithItsNumber(string die, string shape)
    {
        // Act
        var cut = Render<HitDie>(parameters => parameters.Add(p => p.Die, die));

        // Assert
        var sides = die[(die.IndexOf('d', StringComparison.Ordinal) + 1)..];
        cut.Find("[data-testid=hit-die]").GetAttribute("data-die").Should().Be($"d{sides}");
        cut.Find($"svg {shape}").Should().NotBeNull();
        cut.Find("svg text").TextContent.Should().Be(sides);
    }

    [Theory]
    [InlineData("1d8", 4)]
    [InlineData("1d10", 4)]
    [InlineData("1d12", 5)]
    public void HitDie_ForAPolygonDie_HasItsNumberOfCorners(string die, int corners)
    {
        // Act - a diamond and a kite have four corners, a pentagon five
        var cut = Render<HitDie>(parameters => parameters.Add(p => p.Die, die));

        // Assert
        cut.Find("svg polygon").GetAttribute("points")!.Split(' ').Should().HaveCount(corners);
    }
}
