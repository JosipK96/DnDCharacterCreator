using Bunit;

using CharacterCreatorApp.Client.Shared;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Shared;

public class RoomTests : BunitContext
{
    [Fact]
    public void Render_WithATitle_FramesTheContentUnderTheTitle()
    {
        // Act
        var cut = Render<Room>(parameters => parameters
            .Add(p => p.Title, "Ability Scores")
            .Add(p => p.Class, "mt-6")
            .AddChildContent("<p>Roll or buy</p>"));

        // Assert
        var room = cut.Find("section");
        room.ClassList.Should().Contain(["room", "mt-6"]);
        room.QuerySelector("h2.room-title")!.TextContent.Should().Be("Ability Scores");
        room.QuerySelector("p")!.TextContent.Should().Be("Roll or buy");
    }

    [Fact]
    public void Render_WithoutATitle_HasNoHeading()
    {
        // Act
        var cut = Render<Room>(parameters => parameters.AddChildContent("<p>Roll or buy</p>"));

        // Assert
        cut.FindAll("h2").Should().BeEmpty();
    }
}
