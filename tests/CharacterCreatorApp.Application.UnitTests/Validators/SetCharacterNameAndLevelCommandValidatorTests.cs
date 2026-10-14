using CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class SetCharacterNameAndLevelCommandValidatorTests
{
    private readonly SetCharacterNameAndLevelCommandValidator _validator = new();

    [Fact]
    public void Valid_WithCorrectData_Passes()
    {
        // Arrange
        var command = new SetCharacterNameAndLevelCommand(
            Guid.NewGuid(),
            "Thorin Oakenshield",
            5,
            RulesEdition.Edition2014
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_WithEmptyDraftId_Fails()
    {
        // Arrange
        var command = new SetCharacterNameAndLevelCommand(
            Guid.Empty,
            "Valid Name",
            5,
            RulesEdition.Edition2014
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DraftId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Invalid_WithEmptyName_Fails(string? invalidName)
    {
        // Arrange
        var command = new SetCharacterNameAndLevelCommand(
            Guid.NewGuid(),
            invalidName!,
            5,
            RulesEdition.Edition2014
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Invalid_WithNameTooLong_Fails()
    {
        // Arrange
        var tooLongName = new string('a', 101);
        var command = new SetCharacterNameAndLevelCommand(
            Guid.NewGuid(),
            tooLongName,
            5,
            RulesEdition.Edition2014
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Valid_WithNameExactly100Characters_Passes()
    {
        // Arrange
        var maxLengthName = new string('a', 100);
        var command = new SetCharacterNameAndLevelCommand(
            Guid.NewGuid(),
            maxLengthName,
            5,
            RulesEdition.Edition2014
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(100)]
    public void Invalid_WithLevelOutOfRange_Fails(int invalidLevel)
    {
        // Arrange
        var command = new SetCharacterNameAndLevelCommand(
            Guid.NewGuid(),
            "Valid Name",
            invalidLevel,
            RulesEdition.Edition2014
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Level);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void Valid_WithLevelInRange_Passes(int validLevel)
    {
        // Arrange
        var command = new SetCharacterNameAndLevelCommand(
            Guid.NewGuid(),
            "Valid Name",
            validLevel,
            RulesEdition.Edition2014
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Level);
    }

    [Theory]
    [InlineData("2014")]
    [InlineData("2024")]
    public void Valid_WithEitherEdition_Passes(string edition)
    {
        // Arrange
        var command = new SetCharacterNameAndLevelCommand(Guid.NewGuid(), "Valid Name", 5, edition);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("2019")]
    [InlineData("5e")]
    [InlineData(null)]
    public void Invalid_WithAnUnknownEdition_FailsWithItsCode(string? edition)
    {
        // Arrange
        var command = new SetCharacterNameAndLevelCommand(Guid.NewGuid(), "Valid Name", 5, edition!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Edition).WithErrorCode("CharacterDraft.InvalidEdition");
    }
}