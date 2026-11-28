using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;

using FluentValidation.TestHelper;

namespace CharacterCreatorApp.Application.UnitTests.Validators;

public class GetHiddenOptionsQueryValidatorTests
{
    private readonly GetHiddenOptionsQueryValidator _validator = new();

    [Theory]
    [InlineData("races", null)]
    [InlineData("classes", null)]
    [InlineData("backgrounds", null)]
    [InlineData("feats", null)]
    [InlineData("subclasses", "ranger")]
    public void Valid_WithAKnownListAndWhatItNeeds_Passes(string list, string? classId)
    {
        // Arrange
        var query = new GetHiddenOptionsQuery("2014", list, ["PHB"], classId);

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

    [Theory]
    [InlineData("monsters")]
    [InlineData("spells")] // GET spell-allowance counts the spells step's, with its list
    public void Invalid_WithAnUnknownList_FailsWithInvalidList(string list)
    {
        // Act
        var result = _validator.TestValidate(new GetHiddenOptionsQuery("2014", list, [], "wizard"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.List).WithErrorCode(BookErrors.Codes.InvalidList);
    }

    [Fact]
    public void Invalid_WithoutAClassForTheSubclasses_FailsWithClassRequired()
    {
        // Act
        var result = _validator.TestValidate(new GetHiddenOptionsQuery("2014", "subclasses", []));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ClassId).WithErrorCode(BookErrors.Codes.ClassRequired);
    }
}
