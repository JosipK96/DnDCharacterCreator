using CharacterCreatorApp.Domain.Common;

using FluentAssertions;

namespace CharacterCreatorApp.Domain.UnitTests;

public class EntityTests
{
    private sealed class Thing : Entity<Guid>
    {
        public Thing(Guid id)
            : base(id)
        {
        }
    }

    private sealed class OtherThing : Entity<Guid>
    {
        public OtherThing(Guid id)
            : base(id)
        {
        }
    }

    [Fact]
    public void TwoInstancesWithTheSameId_AreEqual()
    {
        // Arrange - the same row loaded twice is two instances
        var id = Guid.NewGuid();

        // Act & Assert
        new Thing(id).Should().Be(new Thing(id));
        (new Thing(id) == new Thing(id)).Should().BeTrue();
        new Thing(id).GetHashCode().Should().Be(new Thing(id).GetHashCode());
    }

    [Fact]
    public void TwoInstancesWithDifferentIds_AreNotEqual()
    {
        new Thing(Guid.NewGuid()).Should().NotBe(new Thing(Guid.NewGuid()));
    }

    [Fact]
    public void DifferentTypesWithTheSameId_AreNotEqual()
    {
        // Arrange - an id is only unique within its own type
        var id = Guid.NewGuid();

        // Act & Assert
        new Thing(id).Equals(new OtherThing(id)).Should().BeFalse();
    }

    [Fact]
    public void ComparingToNull_IsNotEqual()
    {
        var thing = new Thing(Guid.NewGuid());

        thing.Equals(null).Should().BeFalse();
        (thing == null).Should().BeFalse();
        (thing != null).Should().BeTrue();
    }

    [Fact]
    public void TwoNulls_AreEqual()
    {
        Thing? left = null;
        Thing? right = null;

        (left == right).Should().BeTrue();
    }
}
