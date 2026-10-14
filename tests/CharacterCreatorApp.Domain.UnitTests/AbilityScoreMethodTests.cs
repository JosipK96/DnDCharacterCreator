using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class AbilityScoreMethodTests
{
    [Theory]
    [InlineData(AbilityScoreMethod.StandardArray, new[] { 15, 14, 13, 12, 10, 8 }, true)]
    [InlineData(AbilityScoreMethod.StandardArray, new[] { 8, 10, 12, 13, 14, 15 }, true)]
    [InlineData(AbilityScoreMethod.StandardArray, new[] { 15, 15, 13, 12, 10, 8 }, false)]
    [InlineData(AbilityScoreMethod.StandardArray, new[] { 16, 14, 13, 12, 10, 8 }, false)]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 15, 15, 15, 8, 8, 8 }, true)]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 8, 8, 8, 8, 8, 8 }, true)]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 15, 14, 13, 12, 10, 8 }, true)]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 15, 15, 15, 9, 8, 8 }, false)]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 16, 8, 8, 8, 8, 8 }, false)]
    [InlineData(AbilityScoreMethod.PointBuy, new[] { 7, 8, 8, 8, 8, 8 }, false)]
    [InlineData(AbilityScoreMethod.Rolled, new[] { 3, 18, 10, 11, 12, 9 }, true)]
    [InlineData(AbilityScoreMethod.Rolled, new[] { 2, 18, 10, 11, 12, 9 }, false)]
    [InlineData(AbilityScoreMethod.Rolled, new[] { 3, 19, 10, 11, 12, 9 }, false)]
    public void Allows_TheScoresAMethodCanProduce_AndNoOthers(string method, int[] scores, bool expected)
    {
        // Act
        var allowed = AbilityScoreMethod.Allows(method, scores);

        // Assert
        allowed.Should().Be(expected);
    }

    [Fact]
    public void Allows_AnUnknownMethod_IsFalse()
    {
        // Act
        var allowed = AbilityScoreMethod.Allows("manual", [15, 14, 13, 12, 10, 8]);

        // Assert
        allowed.Should().BeFalse();
    }

    [Theory]
    [InlineData(new[] { 15, 14, 13, 12, 10 })]
    [InlineData(new[] { 15, 14, 13, 12, 10, 8, 8 })]
    public void Allows_OtherThanSixScores_IsFalse(int[] scores)
    {
        // Act
        var allowed = AbilityScoreMethod.Allows(AbilityScoreMethod.Rolled, scores);

        // Assert
        allowed.Should().BeFalse();
    }
}
