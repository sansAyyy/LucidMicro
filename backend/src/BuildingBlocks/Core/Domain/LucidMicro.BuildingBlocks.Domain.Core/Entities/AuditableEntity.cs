using LucidMicro.BuildingBlocks.Domain.Core.Abstractions;

namespace LucidMicro.BuildingBlocks.Domain.Core.Entities;

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable
    where TId : notnull
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(TId id)
    {
        Id = id;
    }

    public DateTimeOffset CreatedAt { get; protected set; }

    public string? CreatedBy { get; protected set; }

    public DateTimeOffset? LastModifiedAt { get; protected set; }

    public string? LastModifiedBy { get; protected set; }

    public void MarkCreated(DateTimeOffset createdAt, string? createdBy = null)
    {
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public void MarkModified(DateTimeOffset modifiedAt, string? modifiedBy = null)
    {
        LastModifiedAt = modifiedAt;
        LastModifiedBy = modifiedBy;
    }
}

