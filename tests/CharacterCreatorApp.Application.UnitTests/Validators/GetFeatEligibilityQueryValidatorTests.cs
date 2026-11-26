using CharacterCreatorApp.Application.Features.CharacterCreation.GetFeatEligibility;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class GetFeatEligibilityQueryValidatorTests
{
    private readonly GetFeatEligibilityQueryValidator _validator = new();

    [Fact]
    public void Validate_WithIncreasesInAnySpelling_Passes()
    {
        // Arrange
        var query = new GetFeatEligibilityQuery(Guid.NewGuid(), ["strength", "STR", "dex"]);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithAnIncreaseThatIsNoAbility_ReturnsInvalidAsiAbility()
    {
        // Arrange
        var query = new GetFeatEligibilityQuery(Guid.NewGuid(), ["strength", "luck"]);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("Increases[1]").WithErrorCode("CharacterDraft.InvalidAsiAbility");
    }

    [Fact]
    public void Validate_WithoutADraftId_ReturnsDraftIdRequired()
    {
        // Arrange
        var query = new GetFeatEligibilityQuery(Guid.Empty, []);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DraftId).WithErrorCode("CharacterDraft.DraftIdRequired");
    }
}
