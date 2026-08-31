namespace StudentRoadMap.Domain.Common;

/// <summary>
/// Barcha domen entity'lari uchun bazaviy sinf. Tenglik <see cref="Id"/> bo'yicha aniqlanadi.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity identifikatori bo'sh bo'lishi mumkin emas.", nameof(id));
        }

        Id = id;
    }

    /// <summary>EF Core migratsiya/materializatsiya uchun parametrsiz konstruktor.</summary>
    protected Entity()
    {
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (Id == Guid.Empty || other.Id == Guid.Empty)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
