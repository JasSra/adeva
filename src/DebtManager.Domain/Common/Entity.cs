namespace DebtManager.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; protected set; }
}
