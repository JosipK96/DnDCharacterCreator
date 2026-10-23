using CharacterCreatorApp.Application.Features.CharacterCreation;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class SetHitPointsCommandValidatorTests
{
    private readonly SetHitPointsCommandValidator _validator = new();

    [Fact]
    public void Valid_WithAResultOfOne_Passes()
    {
        // Arrange - the top depends on the class's die, which the handler checks
        var command = new SetHitPointsCommand(Guid.NewGuid(), 1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Invalid_WithAResultBelowOne_FailsWithInvalidHitDieResult(int dieResult)
    {
        // Arrange
        var command = new SetHitPointsCommand(Guid.NewGuid(), dieResult);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Result).WithErrorCode(CharacterDraftErrors.Codes.InvalidHitDieResult);
    }

    [Fact]
    public void Invalid_WithEmptyDraftId_Fails()
    {
        // Arrange
        var command = new SetHitPointsCommand(Guid.Empty, 4);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DraftId).WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
    }
}
