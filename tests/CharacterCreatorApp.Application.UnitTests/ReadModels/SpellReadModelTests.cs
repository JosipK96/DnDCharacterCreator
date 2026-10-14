using CharacterCreatorApp.Application.ReadModels;

using FluentAssertions;

namespace CharacterCreatorApp.Application.UnitTests.ReadModels;

/// <summary>
/// One row for each shape the spell data actually uses.
/// </summary>
public class SpellReadModelTests
{
    [Theory]
    [InlineData(1, "action", "1 action")]
    [InlineData(1, "bonus", "1 bonus action")]
    [InlineData(1, "reaction", "1 reaction")]
    [InlineData(1, "minute", "1 minute")]
    [InlineData(10, "minute", "10 minutes")]
    [InlineData(8, "hour", "8 hours")]
    public void CastingTime_ReadsTheWayTheBookPrintsIt(int number, string unit, string expected)
    {
        // Arrange
        var spell = Spell() with { CastingTimeNumber = number, CastingTimeUnit = unit };

        // Act & Assert
        spell.Model.CastingTime.Should().Be(expected);
    }

    [Theory]
    [InlineData("point", "feet", 60, "60 feet")]
    [InlineData("point", "touch", 0, "Touch")]
    [InlineData("point", "self", 0, "Self")]
    [InlineData("point", "sight", 0, "Sight")]
    [InlineData("point", "miles", 1, "1 mile")]
    [InlineData("point", "miles", 500, "500 miles")]
    [InlineData("point", "unlimited", 0, "Unlimited")]
    [InlineData("cone", "feet", 15, "Self (15-foot cone)")]
    [InlineData("radius", "feet", 30, "Self (30-foot radius)")]
    [InlineData("line", "feet", 100, "Self (100-foot line)")]
    [InlineData("radius", "miles", 1, "Self (1-mile radius)")]
    [InlineData("special", "self", 0, "Special")]
    public void Range_ReadsTheWayTheBookPrintsIt(string type, string distanceType, int amount, string expected)
    {
        // Arrange
        var spell = Spell() with { RangeType = type, RangeDistanceType = distanceType, RangeDistanceAmount = amount };

        // Act & Assert
        spell.Model.Range.Should().Be(expected);
    }

    [Theory]
    [InlineData("instant", null, null, "Instantaneous")]
    [InlineData("timed", 1, "minute", "1 minute")]
    [InlineData("timed", 10, "minute", "10 minutes")]
    [InlineData("timed", 8, "hour", "8 hours")]
    [InlineData("timed", 1, "round", "1 round")]
    [InlineData("permanent", null, null, "Until dispelled")]
    [InlineData("special", null, null, "Special")]
    public void Duration_ReadsTheWayTheBookPrintsIt(string type, int? amount, string? unit, string expected)
    {
        // Arrange
        var spell = Spell() with { DurationType = type, DurationAmount = amount, DurationUnit = unit };

        // Act & Assert
        spell.Model.Duration.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, true, false, null, "V, S")]
    [InlineData(true, false, true, null, "V, M")]
    [InlineData(true, true, true, "a sprig of mistletoe", "V, S, M (a sprig of mistletoe)")]
    [InlineData(false, false, false, null, "")]
    public void Components_ListsEachComponentTheSpellNeeds(bool verbal, bool somatic, bool material, string? materialText, string expected)
    {
        // Arrange
        var spell = Spell() with { Verbal = verbal, Somatic = somatic, Material = material, MaterialText = materialText };

        // Act & Assert
        spell.Model.Components.Should().Be(expected);
    }

    private static SpellParts Spell() => new();

    // SpellReadModel is a class with required members; this record lets each row change only
    // the parts it is about.
    private sealed record SpellParts
    {
        public int CastingTimeNumber { get; init; } = 1;
        public string CastingTimeUnit { get; init; } = "action";
        public string RangeType { get; init; } = "point";
        public string? RangeDistanceType { get; init; } = "feet";
        public int? RangeDistanceAmount { get; init; } = 60;
        public bool Verbal { get; init; } = true;
        public bool Somatic { get; init; } = true;
        public bool Material { get; init; }
        public string? MaterialText { get; init; }
        public string DurationType { get; init; } = "instant";
        public int? DurationAmount { get; init; }
        public string? DurationUnit { get; init; }

        public SpellReadModel Model => new()
        {
            Id = "test-spell",
            Name = "Test Spell",
            School = "Evocation",
            Description = "A test spell.",
            CastingTimeNumber = CastingTimeNumber,
            CastingTimeUnit = CastingTimeUnit,
            RangeType = RangeType,
            RangeDistanceType = RangeDistanceType,
            RangeDistanceAmount = RangeDistanceAmount,
            HasVerbalComponent = Verbal,
            HasSomaticComponent = Somatic,
            HasMaterialComponent = Material,
            Material = MaterialText,
            DurationType = DurationType,
            DurationAmount = DurationAmount,
            DurationUnit = DurationUnit,
        };
    }
}
