using Bunit;

using CharacterCreatorApp.Client.Shared;
using CharacterCreatorApp.Contracts.Responses.Characters;

using FluentAssertions;

namespace CharacterCreatorApp.Client.UnitTests.Shared;

/// <summary>The spell slots the sheet and the review step both show: one square per slot.</summary>
public class SpellSlotsPanelTests : BunitContext
{
    [Fact]
    public void Render_WithSpellSlots_DrawsALinePerLevelWithOneSquarePerSlot()
    {
        // Act
        var cut = Render<SpellSlotsPanel>(parameters => parameters.Add(p => p.Stats, Stats([new(1, 4), new(2, 3), new(3, 2)], null)));

        // Assert
        var levels = cut.FindAll("[data-testid='slot-level']");
        levels.Select(l => l.TextContent.Split("level")[0].Trim()).Should().Equal("1st", "2nd", "3rd");
        levels.Select(l => l.QuerySelectorAll("[data-testid='slot']").Length).Should().Equal(4, 3, 2);
        cut.FindAll("[data-testid='pact-magic']").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithPactMagicOnly_DrawsItsSlotsApartAndNoSpellSlots()
    {
        // Act
        var cut = Render<SpellSlotsPanel>(parameters => parameters.Add(p => p.Stats, Stats([], new(2, 2))));

        // Assert
        cut.FindAll("[data-testid='slot-level']").Should().BeEmpty();
        var pact = cut.Find("[data-testid='pact-magic']");
        pact.TextContent.Should().Contain("2nd level");
        pact.QuerySelectorAll("[data-testid='slot']").Should().HaveCount(2);
        cut.Markup.Should().Contain("Pact Magic, back on a short rest");
    }

    private static CharacterStatsApiResponse Stats(List<SpellSlotStatApiResponse> slots, PactMagicStatApiResponse? pactMagic) => new(
        2, 8, 12, 2, 30, "1d8", 11, [], [], [], slots, pactMagic, []);
}
