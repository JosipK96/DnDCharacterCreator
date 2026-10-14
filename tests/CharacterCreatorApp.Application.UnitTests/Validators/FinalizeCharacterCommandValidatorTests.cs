using CharacterCreatorApp.Application.Features.CharacterCreation.FinalizeCharacter;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class FinalizeCharacterCommandValidatorTests
{
    private readonly FinalizeCharacterCommandValidator _validator = new();

    [Fact]
    public void Valid_WithValidDraftId_Passes()
    {
        // Arrange
        var command = new FinalizeCharacterCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_WithEmptyDraftId_Fails()
    {
        // Arrange
        var command = new FinalizeCharacterCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DraftId);
    }
}