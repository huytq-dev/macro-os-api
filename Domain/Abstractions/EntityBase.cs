namespace Domain;

public abstract class EntityBase<T> : IEntity
{
    public T Id { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
