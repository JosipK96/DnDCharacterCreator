using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

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
            },
            AbilityScoreMethod.StandardArray
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
            },
            AbilityScoreMethod.StandardArray
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
            },
            AbilityScoreMethod.StandardArray
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
            CreateValidScores(),
            AbilityScoreMethod.StandardArray
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
            },
            AbilityScoreMethod.StandardArray
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
            },
            AbilityScoreMethod.StandardArray
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
            },
            AbilityScoreMethod.StandardArray
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
            },
            AbilityScoreMethod.StandardArray
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Scores.Keys);
    }

    [Theory]
    [InlineData(AbilityScoreMethod.StandardArray, new[] { 15, 14, 13, 12, 10, 8 })]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 15, 15, 15, 8, 8, 8 })]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 8, 8, 8, 8, 8, 8 })]
    [InlineData(AbilityScoreMethod.Rolled, new[] { 18, 3, 13, 12, 10, 8 })]
    public void Valid_WithScoresTheMethodCanProduce_Passes(string method, int[] scores)
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(Guid.NewGuid(), ScoresOf(scores), method);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(AbilityScoreMethod.StandardArray, new[] { 15, 15, 13, 12, 10, 8 })]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 15, 15, 15, 15, 8, 8 })]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 7, 14, 13, 12, 10, 8 })]
    [InlineData(AbilityScoreMethod.Rolled, new[] { 19, 14, 13, 12, 10, 8 })]
    [InlineData(AbilityScoreMethod.Rolled, new[] { 2, 14, 13, 12, 10, 8 })]
    public void Invalid_WithScoresTheMethodCannotProduce_Fails(string method, int[] scores)
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(Guid.NewGuid(), ScoresOf(scores), method);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Scores)
            .WithErrorCode("CharacterDraft.AbilityScoresDoNotFitMethod");
        result.Errors.Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("4d6")]
    [InlineData("Standard-Array")]
    public void Invalid_WithUnknownMethod_FailsOnTheMethodAlone(string method)
    {
        // Arrange
        var command = new SetCharacterAbilityScoresCommand(Guid.NewGuid(), CreateValidScores(), method);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Method)
            .WithErrorCode("CharacterDraft.InvalidAbilityScoreMethod");
        result.Errors.Should().ContainSingle();
    }

    private static Dictionary<string, int> ScoresOf(int[] scores) => new()
    {
        ["STR"] = scores[0],
        ["DEX"] = scores[1],
        ["CON"] = scores[2],
        ["INT"] = scores[3],
        ["WIS"] = scores[4],
        ["CHA"] = scores[5]
    };

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