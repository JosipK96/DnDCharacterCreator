using Bunit;

using CharacterCreatorApp.Client.Shared;

using FluentAssertions;

using Microsoft.JSInterop;

namespace CharacterCreatorApp.Client.UnitTests.Shared;

public class UpdatePromptTests : BunitContext
{
    private const string Prompt = "[data-testid='update-prompt']";

    public UpdatePromptTests()
    {
        JSInterop.SetupVoid("appUpdate.listen", _ => true);
        JSInterop.SetupVoid("appUpdate.apply");
    }

    [Fact]
    public void Render_WhenNoUpdateIsSignalled_ShowsNothingAndListensForOne()
    {
        // Act
        var cut = Render<UpdatePrompt>();

        // Assert
        cut.FindAll(Prompt).Should().BeEmpty();
        JSInterop.Invocations["appUpdate.listen"].Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateReady_WhenSignalled_ShowsThePrompt()
    {
        // Arrange
        var cut = Render<UpdatePrompt>();

        // Act
        await Signal(cut);

        // Assert
        cut.Find(Prompt).TextContent.Should().Contain("A new version is ready");
    }

    [Fact]
    public async Task Reload_WhenAccepted_AsksTheAppToSwitchToTheNewVersion()
    {
        // Arrange
        var cut = Render<UpdatePrompt>();
        await Signal(cut);

        // Act
        cut.Find($"{Prompt} button").Click();

        // Assert
        JSInterop.Invocations["appUpdate.apply"].Should().ContainSingle();
    }

    // What app-update.js does through the reference it was handed
    private static Task Signal(IRenderedComponent<UpdatePrompt> cut) => cut.InvokeAsync(() => cut.Instance.UpdateReady());
}

