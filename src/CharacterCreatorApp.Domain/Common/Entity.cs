namespace CharacterCreatorApp.Domain.Common;

/// <summary>
/// Base for anything the database stores by identity. Two entities of the same type are equal
/// when their ids match, regardless of whether they are the same instance — which is what you
/// want when one came from a query and the other from the change tracker.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
#pragma warning disable CS8618 // EF materialises entities without running a constructor
    public TId Id { get; protected set; }

    protected Entity()
    {
    }
#pragma warning restore CS8618

    protected Entity(TId id)
    {
        Id = id;
    }

    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && other.Id.Equals(Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
