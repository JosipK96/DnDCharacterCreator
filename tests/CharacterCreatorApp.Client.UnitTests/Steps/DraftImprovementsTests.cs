using CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Improvements;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Feats;

using FluentAssertions;

using static CharacterCreatorApp.Client.UnitTests.Fixtures.TestData;

namespace CharacterCreatorApp.Client.UnitTests.Steps;

/// <summary>
/// Rebuilding the improvements step's slots from a saved draft, and turning them back into its save,
/// without rendering the step.
/// </summary>
public class DraftImprovementsTests
{
    private static readonly FeatApiResponse Alert = Feat("alert-phb", "Alert");
    private static readonly FeatApiResponse Athlete = Feat("athlete-phb", "Athlete", increase: new(["STR", "DEX"], 1, 20));
    private static readonly FeatApiResponse Skilled = Feat("skilled-phb", "Skilled", choice: new(FeatChoiceApiResponse.Skills, 2, ["arcana", "history", "stealth"]));
    private static readonly FeatApiResponse KeenMind = Feat("keen-mind-phb", "Keen Mind", choice: new(FeatChoiceApiResponse.Skills, 1, ["arcana", "history"]));

    [Fact]
    public void RestoreSlots_WithAnImprovementAndAFeatSaved_PutsTheImprovementFirst()
    {
        // Arrange - the draft does not record which slot each came from
        var draft = new CharacterDraftDetailsApiResponse
        {
            SelectedFeatIds = ["alert-phb"],
            AbilityScoreImprovements = [new Dictionary<string, int> { ["CON"] = 2 }],
        };

        // Act
        var slots = DraftImprovements.RestoreSlots(draft, bonusFeatCount: 0, asiCount: 2);

        // Assert
        slots.Should().HaveCount(2);
        slots[0].TakesFeat.Should().BeFalse();
        slots[0].Improvement.Should().Equal(new Dictionary<string, int> { ["CON"] = 2 });
        slots[1].TakesFeat.Should().BeTrue();
        slots[1].FeatId.Should().Be("alert-phb");
    }

    [Fact]
    public void RestoreSlots_ForAVariantHuman_PutsTheRaceFeatFirstWithItsAbility()
    {
        // Arrange
        var draft = new CharacterDraftDetailsApiResponse
        {
            SelectedFeatIds = ["athlete-phb", "alert-phb"],
            FeatAbilityIncreases = [new Dictionary<string, int> { ["DEX"] = 1 }, []],
        };

        // Act
        var slots = DraftImprovements.RestoreSlots(draft, bonusFeatCount: 1, asiCount: 1);

        // Assert
        slots.Select(s => (s.IsRaceFeat, s.FeatId)).Should().Equal((true, "athlete-phb"), (false, "alert-phb"));
        slots[0].FeatAbility.Should().Be("DEX");
        slots[1].FeatAbility.Should().BeNull();
    }

    [Fact]
    public void RestoreSlots_ForALevelUpToANewImprovement_KeepsTheSavedOneAndAddsAnEmptySlot()
    {
        // Arrange
        var draft = new CharacterDraftDetailsApiResponse { AbilityScoreImprovements = [new Dictionary<string, int> { ["STR"] = 2 }] };

        // Act
        var slots = DraftImprovements.RestoreSlots(draft, bonusFeatCount: 0, asiCount: 2);

        // Assert
        slots.Should().HaveCount(2);
        slots[0].Improvement.Should().Equal(new Dictionary<string, int> { ["STR"] = 2 });
        slots[1].Improvement.Should().BeEmpty();
        slots[1].TakesFeat.Should().BeFalse();
    }

    [Fact]
    public void RestoreSlots_WithMoreSavedThanTheCharacterHasEarned_StartsEveryRaceFeatAndImprovementEmpty()
    {
        // Arrange - two improvements saved, but the level now gives one
        var draft = new CharacterDraftDetailsApiResponse
        {
            AbilityScoreImprovements = [new Dictionary<string, int> { ["STR"] = 2 }, new Dictionary<string, int> { ["DEX"] = 2 }],
        };

        // Act
        var slots = DraftImprovements.RestoreSlots(draft, bonusFeatCount: 1, asiCount: 1);

        // Assert
        slots.Should().HaveCount(2);
        slots[0].Should().BeEquivalentTo(new { IsRaceFeat = true, TakesFeat = true, FeatId = (string?)null });
        slots[1].Should().BeEquivalentTo(new { IsRaceFeat = false, TakesFeat = false });
        slots[1].Improvement.Should().BeEmpty();
    }

    [Fact]
    public void RestoreSlots_WithAnImprovementSavedByTheAbilitysFullName_KeysItByTheCanonicalId()
    {
        // Arrange - the step saved "strength" before it used the ids, and the API accepted it
        var draft = new CharacterDraftDetailsApiResponse
        {
            AbilityScoreImprovements = [new Dictionary<string, int> { ["strength"] = 1, ["Dexterity"] = 1 }],
        };

        // Act
        var slots = DraftImprovements.RestoreSlots(draft, bonusFeatCount: 0, asiCount: 1);

        // Assert
        slots.Single().Improvement.Should().Equal(new Dictionary<string, int> { ["STR"] = 1, ["DEX"] = 1 });
        slots.Single().PointsLeft.Should().Be(0);
    }

    [Fact]
    public void RestoreProficiencyPicks_InTheApisOrder_GivesTheBackgroundFeatsPicksFirstAndEachSlotItsOwn()
    {
        // Arrange - Skilled from the background, Keen Mind in a slot
        var draft = new CharacterDraftDetailsApiResponse
        {
            SelectedFeatIds = ["keen-mind-phb"],
            FeatProficiencyPicks =
            [
                new FeatProficiencyPickApiModel { FeatId = "skilled-phb", ProficiencyIds = ["arcana", "stealth"] },
                new FeatProficiencyPickApiModel { FeatId = "keen-mind-phb", ProficiencyIds = ["history"] },
            ],
        };
        var slots = DraftImprovements.RestoreSlots(draft, bonusFeatCount: 0, asiCount: 1);

        // Act
        var backgroundPicks = DraftImprovements.RestoreProficiencyPicks(draft, Skilled, slots, [Alert, KeenMind]);

        // Assert
        backgroundPicks.Should().Equal("arcana", "stealth");
        slots.Single().ProficiencyPicks.Should().Equal("history");
    }

    [Fact]
    public void RestoreProficiencyPicks_WithPicksSavedForAnotherFeat_LeavesTheSlotsPicksEmpty()
    {
        // Arrange - the picks are for a feat the slot no longer takes
        var draft = new CharacterDraftDetailsApiResponse
        {
            SelectedFeatIds = ["keen-mind-phb"],
            FeatProficiencyPicks = [new FeatProficiencyPickApiModel { FeatId = "skilled-phb", ProficiencyIds = ["arcana", "stealth"] }],
        };
        var slots = DraftImprovements.RestoreSlots(draft, bonusFeatCount: 0, asiCount: 1);

        // Act
        var backgroundPicks = DraftImprovements.RestoreProficiencyPicks(draft, backgroundFeat: null, slots, [KeenMind]);

        // Assert
        backgroundPicks.Should().BeEmpty();
        slots.Single().ProficiencyPicks.Should().ContainSingle().Which.Should().BeNull();
    }

    [Fact]
    public void ToSave_WithARaceFeatPointsAndAFeat_SendsTheFeatsInSlotOrderAndTheBackgroundsPicksFirst()
    {
        // Arrange
        var raceFeat = new AsiSlot { IsRaceFeat = true, TakesFeat = true, FeatId = "athlete-phb", FeatAbility = "DEX" };
        var points = new AsiSlot();
        points.Raise("WIS");
        points.Raise("WIS");
        var feat = new AsiSlot { TakesFeat = true, FeatId = "keen-mind-phb", ProficiencyPicks = ["history"] };

        // Act
        var toSave = DraftImprovements.ToSave([raceFeat, points, feat], [Athlete, KeenMind], Skilled, ["arcana", "stealth"]);

        // Assert
        toSave.Improvements.Should().ContainSingle().Which.Should().Equal(new Dictionary<string, int> { ["WIS"] = 2 });
        toSave.FeatIds.Should().Equal("athlete-phb", "keen-mind-phb");
        toSave.FeatAbilityIncreases.Should().HaveCount(2);
        toSave.FeatAbilityIncreases[0].Should().Equal(new Dictionary<string, int> { ["DEX"] = 1 });
        toSave.FeatAbilityIncreases[1].Should().BeEmpty();
        toSave.ProficiencyPicks.Select(p => (p.FeatId, string.Join(",", p.ProficiencyIds)))
            .Should().Equal(("skilled-phb", "arcana,stealth"), ("keen-mind-phb", "history"));
    }

    [Fact]
    public void SpentPoints_WithAHalfSpentImprovementAndAFeat_SumsThePointsByAbility()
    {
        // Arrange
        var first = new AsiSlot();
        first.Raise("STR");
        first.Raise("DEX");
        var second = new AsiSlot();
        second.Raise("STR");
        var feat = new AsiSlot { TakesFeat = true, FeatId = "alert-phb" };

        // Act
        var spent = DraftImprovements.SpentPoints([first, second, feat]);

        // Assert
        spent.Should().Equal(new Dictionary<string, int> { ["STR"] = 2, ["DEX"] = 1 });
    }
}
