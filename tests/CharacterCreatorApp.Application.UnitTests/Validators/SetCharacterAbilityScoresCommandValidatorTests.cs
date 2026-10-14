using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class SetCharacterAbilityScoresCommandValidatorTests
{
    private readonly SetCharacterAbilityScoresCommandValidator _validator = new();

    [Fact]
    public void Valid_WithSixCorrectScores_Passes()
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(
            Guid.NewGuid(),
            new Dictionary<string, int>
            {
                ["STR"] = 15,
                ["DEX"] = 14,
                ["CON"] = 13,
                ["INT"] = 12,
                ["WIS"] = 10,
                ["CHA"] = 8
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_WithCaseInsensitiveKeys_Passes()
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(
            Guid.NewGuid(),
            new Dictionary<string, int>
            {
                ["str"] = 15,
                ["dex"] = 14,
                ["con"] = 13,
                ["int"] = 12,
                ["wis"] = 10,
                ["cha"] = 8
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_WithMixedCaseKeys_Passes()
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(
            Guid.NewGuid(),
            new Dictionary<string, int>
            {
                ["Str"] = 15,
                ["DeX"] = 14,
                ["CON"] = 13,
                ["int"] = 12,
                ["Wis"] = 10,
                ["CHA"] = 8
            }
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
        var command = new SetCharacterAbilityScoresCommand(
            Guid.Empty,
            CreateValidScores()
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DraftId);
    }

    [Fact]
    public void Invalid_WithFewerThanSixScores_Fails()
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(
            Guid.NewGuid(),
            new Dictionary<string, int>
            {
                ["STR"] = 15,
                ["DEX"] = 14,
                ["CON"] = 13
                // Missing 3 scores
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Scores)
            .WithErrorMessage("Must provide exactly 6 ability scores");
    }

    [Fact]
    public void Invalid_WithMoreThanSixScores_Fails()
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(
            Guid.NewGuid(),
            new Dictionary<string, int>
            {
                ["STR"] = 15,
                ["DEX"] = 14,
                ["CON"] = 13,
                ["INT"] = 12,
                ["WIS"] = 10,
                ["CHA"] = 8,
                ["LUCK"] = 10 // Extra invalid score
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Scores)
            .WithErrorMessage("Must provide exactly 6 ability scores");
    }

    [Fact]
    public void Invalid_WithInvalidAbilityScoreKey_Fails()
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(
            Guid.NewGuid(),
            new Dictionary<string, int>
            {
                ["STR"] = 15,
                ["DEX"] = 14,
                ["CON"] = 13,
                ["INT"] = 12,
                ["WIS"] = 10,
                ["INVALID"] = 8 // Invalid key
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Scores.Keys)
            .WithErrorMessage("Invalid ability score key names. Valid names are STR, DEX, CON, INT, WIS, CHA");
    }

    [Fact]
    public void Invalid_WithMultipleInvalidKeys_Fails()
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(
            Guid.NewGuid(),
            new Dictionary<string, int>
            {
                ["STRENGTH"] = 15, // Invalid - should be STR
                ["DEXTERITY"] = 14, // Invalid - should be DEX
                ["CON"] = 13,
                ["INT"] = 12,
                ["WIS"] = 10,
                ["CHA"] = 8
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Scores.Keys);
    }

    private static Dictionary<string, int> CreateValidScores()
    {
        return new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        };
    }
}