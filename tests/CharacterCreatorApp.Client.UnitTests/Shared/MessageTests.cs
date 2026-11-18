using Bunit;

using CharacterCreatorApp.Client.Shared;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Shared;

public class MessageTests : BunitContext
{
    [Theory]
    [InlineData(MessageKind.Error, "alert", "bg-red-tint", "border-red")]
    [InlineData(MessageKind.Success, "status", "bg-green-tint", "border-green")]
    [InlineData(MessageKind.Warning, "alert", "bg-amber-tint", "border-amber")]
    [InlineData(MessageKind.Info, "note", "bg-room-highlight", "border-ink")]
    public void Render_EachKind_HasItsRoleTintAndBorder(MessageKind kind, string role, string tint, string border)
    {
        // Act
        var cut = Render<Message>(parameters => parameters
            .Add(p => p.Kind, kind)
            .AddChildContent("<p>Something to say</p>"));

        // Assert
        var message = cut.Find($"[role={role}]");
        message.ClassList.Should().Contain([tint, border, "border-l-[5px]"]);
        message.GetAttribute("data-kind").Should().Be(kind.ToString().ToLowerInvariant());
        message.QuerySelector("svg[aria-hidden=true]").Should().NotBeNull();
        message.QuerySelector("p")!.TextContent.Should().Be("Something to say");
    }

    [Fact]
    public void Render_WithLayoutClassesAndAttributes_KeepsThem()
    {
        // Act
        var cut = Render<Message>(parameters => parameters
            .Add(p => p.Kind, MessageKind.Error)
            .Add(p => p.Class, "mb-6")
            .AddUnmatched("id", "save-error"));

        // Assert
        var message = cut.Find("[role=alert]");
        message.ClassList.Should().Contain("mb-6");
        message.Id.Should().Be("save-error");
    }
}
