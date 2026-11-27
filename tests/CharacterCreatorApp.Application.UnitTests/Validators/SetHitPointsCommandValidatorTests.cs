using CharacterCreatorApp.Application.Features.CharacterCreation;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;

using FluentAssertions;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class SetHitPointsCommandValidatorTests
{
    private readonly SetHitPointsCommandValidator _validator = new();

    [Fact]
    public void Valid_WithAResultOfOne_Passes()
    {
        // Arrange - the top depends on the class's die, and the levels on the draft, which the handler checks
        var command = new SetHitPointsCommand(Guid.NewGuid(), new Dictionary<int, int> { [2] = 1, [3] = 8 });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_WithNoResults_Passes()
    {
        // Arrange - a new character leaving every level alone takes the fixed value for each
        var command = new SetHitPointsCommand(Guid.NewGuid(), new Dictionary<int, int>());

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
        var command = new SetHitPointsCommand(Guid.NewGuid(), new Dictionary<int, int> { [2] = 4, [3] = dieResult });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(CharacterDraftErrors.Codes.InvalidHitDieResult);
    }

    [Fact]
    public void Invalid_WithNoResultsAtAll_FailsWithHitDieResultsRequired()
    {
        // Arrange - a body without the dictionary
        var command = new SetHitPointsCommand(Guid.NewGuid(), null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Results).WithErrorCode(CharacterDraftErrors.Codes.HitDieResultsRequired);
    }

    [Fact]
    public void Invalid_WithEmptyDraftId_Fails()
    {
        // Arrange
        var command = new SetHitPointsCommand(Guid.Empty, new Dictionary<int, int> { [5] = 4 });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DraftId).WithErrorCode(CharacterDraftErrors.Codes.DraftIdRequired);
    }
}
