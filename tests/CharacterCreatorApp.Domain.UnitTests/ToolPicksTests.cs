using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace CharacterCreatorApp.Domain.UnitTests;

public class ToolPicksTests
{
    private static readonly ToolChoice BardInstruments = new() { Count = 3, Options = ["drum", "flute", "lute", "lyre"] };

    [Theory]
    [InlineData("lute lyre drum", true)]
    [InlineData("lute lyre", false)]
    [InlineData("lute lyre drum flute", false)]
    [InlineData("lute lute lyre", false)]
    [InlineData("lute lyre dice_set", false)]
    public void Allows_ThreeDifferentInstrumentsForABard_OnlyThose(string picks, bool allowed)
    {
        // Act
        var result = BardInstruments.Allows(picks.Split(' '));

        // Assert
        result.Should().Be(allowed);
    }

    [Theory]
    [InlineData("race")]
    [InlineData("background")]
    [InlineData("class")]
    [InlineData("edition")]
    public void ChangingWhatOffersTheTools_ClearsThePicks(string change)
    {
        // Arrange - a dwarf criminal bard's picks, in race, background, class order
        var draft = CharacterDraft.CreateEmpty(Guid.Empty, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetRace("dwarf");
        draft.SetBackground("criminal");
        draft.SetClass("bard");
        draft.ChooseTools([["smith_tools"], ["dice_set"], ["drum", "flute", "lute"]]);

        // Act
        switch (change)
        {
            case "race":
                draft.SetRace("elf");
                break;
            case "background":
                draft.SetBackground("noble");
                break;
            case "class":
                draft.SetClass("monk");
                break;
            default:
                draft.ChooseEdition(RulesEdition.Edition2024);
                break;
        }

        // Assert
        draft.ToolPicks.Should().BeEmpty();
    }

    [Fact]
    public void SettingTheSameRaceBackgroundAndClassAgain_KeepsThePicks()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(Guid.Empty, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetRace("dwarf");
        draft.SetBackground("criminal");
        draft.SetClass("bard");
        draft.ChooseTools([["smith_tools"], ["dice_set"], ["drum", "flute", "lute"]]);

        // Act
        draft.SetRace("dwarf");
        draft.SetBackground("criminal");
        draft.SetClass("bard");

        // Assert
        draft.ToolPicks.Should().HaveCount(3);
    }
}
