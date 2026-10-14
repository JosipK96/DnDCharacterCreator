using CharacterCreatorApp.Application.Common.Filtering;

using FluentAssertions;

namespace CharacterCreatorApp.Application.UnitTests.Filtering;

public class PageOptionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void PageSize_OutsideOneToOneHundred_FallsBackToTen(int requested)
    {
        // Act
        var options = new PageOptions { PageSize = requested };

        // Assert
        options.PageSize.Should().Be(10);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void PageSize_AtEitherEndOfTheRange_IsKept(int requested)
    {
        // Act
        var options = new PageOptions { PageSize = requested };

        // Assert
        options.PageSize.Should().Be(requested);
    }

    [Fact]
    public void PageIndex_BelowZero_FallsBackToTheFirstPage()
    {
        // Act
        var options = new PageOptions { PageIndex = -1 };

        // Assert
        options.PageIndex.Should().Be(0);
    }

    [Fact]
    public void Default_IsTheFirstPageOfTen()
    {
        // Act
        var options = PageOptions.Default;

        // Assert
        options.PageIndex.Should().Be(0);
        options.PageSize.Should().Be(10);
    }
}
