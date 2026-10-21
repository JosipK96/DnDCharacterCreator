using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScoreImprovements;
using CharacterCreatorApp.Domain.DomainModels.Characters;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class SetAbilityScoreImprovementsCommandValidatorTests
{
    private readonly SetAbilityScoreImprovementsCommandValidator _validator = new();

    [Fact]
    public void Validate_WithOneIncreasePerFeat_Passes()
    {
        // Arrange - Athlete's Dexterity, and nothing for Alert
        var command = new SetAbilityScoreImprovementsCommand(Guid.NewGuid(), [], ["athlete", "alert"], [new() { ["DEX"] = 1 }, []]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithoutIncreases_Passes()
    {
        // Arrange - no feat raises a picked ability
        var command = new SetAbilityScoreImprovementsCommand(Guid.NewGuid(), [], ["alert"]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithFewerIncreasesThanFeats_ReturnsFeatAbilityIncreasesMismatch()
    {
        // Arrange - which feat the one increase belongs to cannot be told
        var command = new SetAbilityScoreImprovementsCommand(Guid.NewGuid(), [], ["athlete", "alert"], [new() { ["DEX"] = 1 }]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FeatAbilityIncreases)
            .WithErrorCode("CharacterDraft.FeatAbilityIncreasesMismatch");
    }

    [Fact]
    public void Validate_WithAProficiencyPickPerFeat_Passes()
    {
        // Arrange - Keen Mind's skill
        var command = new SetAbilityScoreImprovementsCommand(
            Guid.NewGuid(), [], ["keen_mind_2024"], null, [new FeatProficiencyPick { FeatId = "keen_mind_2024", ProficiencyIds = ["arcana"] }]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithAProficiencyPickWithoutItsFeat_ReturnsFeatProficiencyPicksRequired()
    {
        // Arrange
        var command = new SetAbilityScoreImprovementsCommand(
            Guid.NewGuid(), [], ["keen_mind_2024"], null, [new FeatProficiencyPick { FeatId = string.Empty, ProficiencyIds = ["arcana"] }]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("FeatProficiencyPicks[0].FeatId")
            .WithErrorCode("CharacterDraft.FeatProficiencyPicksRequired");
    }

    [Fact]
    public void Validate_WithAProficiencyPickWithoutItsIds_ReturnsFeatProficiencyPicksRequired()
    {
        // Arrange - a JSON null for the list
        var command = new SetAbilityScoreImprovementsCommand(
            Guid.NewGuid(), [], ["keen_mind_2024"], null, [new FeatProficiencyPick { FeatId = "keen_mind_2024", ProficiencyIds = null! }]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("FeatProficiencyPicks[0].ProficiencyIds")
            .WithErrorCode("CharacterDraft.FeatProficiencyPicksRequired");
    }
}
