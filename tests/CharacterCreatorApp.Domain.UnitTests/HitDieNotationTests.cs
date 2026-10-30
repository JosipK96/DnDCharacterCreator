using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class HitDieNotationTests
{
    [Theory]
    [InlineData("1d6", 4, 6)]
    [InlineData("d8", 5, 8)]
    [InlineData("10", 6, 10)]
    [InlineData("1d12", 7, 12)]
    public void SizeOfAndFixedValueOf_ReadTheDieWhicheverWayItIsWritten(string hitDie, int fixedValue, int size)
    {
        HitDieNotation.FixedValueOf(hitDie).Should().Be(fixedValue);
        HitDieNotation.SizeOf(hitDie).Should().Be(size);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("1dx")]
    [InlineData("d0")]
    [InlineData("1d-6")]
    public void SizeOf_ForAnUnreadableDie_Throws(string hitDie)
    {
        var act = () => HitDieNotation.SizeOf(hitDie);

        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("1d10", true, 10)]
    [InlineData("nonsense", false, 0)]
    public void TryParse_SaysWhetherTheTextIsADie(string hitDie, bool expected, int size)
    {
        HitDieNotation.TryParse(hitDie, out var parsed).Should().Be(expected);
        parsed.Should().Be(size);
    }
}
