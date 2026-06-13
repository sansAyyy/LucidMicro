using LucidMicro.BuildingBlocks.Domain.Core.Abstractions;

namespace LucidMicro.BuildingBlocks.Domain.Core.Entities;

public abstract class SoftDeleteEntity<TId> : AuditableEntity<TId>, ISoftDelete
    where TId : notnull
{
    protected SoftDeleteEntity()
    {
    }

    protected SoftDeleteEntity(TId id)
    {
        Id = id;
    }

    public bool IsDeleted { get; protected set; }

    public void MarkDeleted()
    {
        IsDeleted = true;
    }

    public void Restore()
    {
        IsDeleted = false;
    }
}

