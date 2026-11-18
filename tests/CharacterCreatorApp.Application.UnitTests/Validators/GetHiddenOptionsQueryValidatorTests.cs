using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class GetHiddenOptionsQueryValidatorTests
{
    private readonly GetHiddenOptionsQueryValidator _validator = new();

    [Theory]
    [InlineData("races", null, null)]
    [InlineData("classes", null, null)]
    [InlineData("backgrounds", null, null)]
    [InlineData("feats", null, null)]
    [InlineData("subclasses", "ranger", null)]
    [InlineData("spells", "wizard", 0)]
    [InlineData("spells", "wizard", 9)]
    public void Valid_WithAKnownListAndWhatItNeeds_Passes(string list, string? classId, int? maxLevel)
    {
        // Arrange
        var query = new GetHiddenOptionsQuery("2014", list, ["PHB"], classId, MaxLevel: maxLevel);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_WithAnUnknownEdition_FailsWithInvalidEdition()
    {
        // Act
        var result = _validator.TestValidate(new GetHiddenOptionsQuery("2016", "races", []));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Edition).WithErrorCode(BookErrors.Codes.InvalidEdition);
    }

    [Fact]
    public void Invalid_WithAnUnknownList_FailsWithInvalidList()
    {
        // Act
        var result = _validator.TestValidate(new GetHiddenOptionsQuery("2014", "monsters", []));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.List).WithErrorCode(BookErrors.Codes.InvalidList);
    }

    [Theory]
    [InlineData("subclasses")]
    [InlineData("spells")]
    public void Invalid_WithoutAClassForAClassList_FailsWithClassRequired(string list)
    {
        // Act
        var result = _validator.TestValidate(new GetHiddenOptionsQuery("2014", list, []));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ClassId).WithErrorCode(BookErrors.Codes.ClassRequired);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void Invalid_WithASpellLevelOutOfRange_FailsWithInvalidMaxLevel(int maxLevel)
    {
        // Act
        var result = _validator.TestValidate(new GetHiddenOptionsQuery("2014", "spells", [], "wizard", MaxLevel: maxLevel));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxLevel).WithErrorCode(BookErrors.Codes.InvalidMaxLevel);
    }
}
