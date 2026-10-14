using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScoreImprovements;

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
}
