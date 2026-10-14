using CharacterCreatorApp.Domain.Common.Results;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class ResponseTests
{
    [Fact]
    public void Value_OnASuccessfulResponse_ReturnsTheValue()
    {
        // Arrange
        Response<int> response = 42;

        // Act
        var value = response.Value;

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void Value_OnAFailedResponse_ThrowsInsteadOfReturningDefault()
    {
        // Arrange - for Response<Guid> the old behaviour handed back Guid.Empty, a plausible id
        Response<Guid> response = Error.NotFound("Draft.NotFound", "Not found");

        // Act
        Func<Guid> act = () => response.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft.NotFound*");
    }

    [Fact]
    public void ToString_OnAFailedResponse_ListsTheErrorCodesWithoutThrowing()
    {
        // Arrange - the record's synthesized ToString reads every public property, Value included
        Response<string> response = Error.Validation("Draft.Invalid", "Incomplete");

        // Act
        var text = response.ToString();

        // Assert
        text.Should().Contain("IsSuccess = False").And.Contain("Draft.Invalid");
    }

    [Fact]
    public void ToString_OnASuccessfulResponse_ShowsTheValue()
    {
        // Arrange
        Response<string> response = "Thorin";

        // Act
        var text = response.ToString();

        // Assert
        text.Should().Contain("IsSuccess = True").And.Contain("Thorin");
    }

    [Fact]
    public void Errors_OnASuccessfulResponse_IsEmpty()
    {
        // Arrange
        Response<int> response = 1;

        // Act & Assert
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Equality_BetweenTwoFailuresWithTheSameErrorList_DoesNotReadValue()
    {
        // Arrange - synthesized equality compares fields, so it must not trip the Value guard
        var errors = new List<Error> { Error.Conflict("Draft.Conflict", "Conflict") };
        Response<int> left = errors;
        Response<int> right = errors;

        // Act
        var equal = left == right;

        // Assert
        equal.Should().BeTrue();
    }
}
