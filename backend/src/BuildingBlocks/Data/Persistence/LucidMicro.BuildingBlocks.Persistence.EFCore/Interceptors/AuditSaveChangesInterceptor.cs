using LucidMicro.BuildingBlocks.Domain.Core.Abstractions;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Interceptors;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditUserProvider _auditUserProvider;
    private readonly TimeProvider _timeProvider;

    public AuditSaveChangesInterceptor(IAuditUserProvider auditUserProvider, TimeProvider timeProvider)
    {
        _auditUserProvider = auditUserProvider;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditChanges(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditChanges(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditChanges(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        var userId = _auditUserProvider.GetCurrentUserId();

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyCreated(entry.Entity, now, userId);
                    break;

                case EntityState.Modified:
                    ApplyModified(entry.Entity, now, userId);
                    break;

                case EntityState.Deleted:
                    ApplyDeleted(entry, now, userId);
                    break;
            }
        }
    }

    private static void ApplyCreated(object entity, DateTimeOffset now, string? userId)
    {
        if (entity is not IAuditable auditable)
        {
            return;
        }

        var createdAt = auditable.CreatedAt == default ? now : auditable.CreatedAt;
        var createdBy = auditable.CreatedBy ?? userId;

        auditable.MarkCreated(createdAt, createdBy);
    }

    private static void ApplyModified(object entity, DateTimeOffset now, string? userId)
    {
        if (entity is IAuditable auditable)
        {
            auditable.MarkModified(now, userId);
        }
    }

    private static void ApplyDeleted(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, DateTimeOffset now, string? userId)
    {
        if (entry.Entity is not ISoftDelete softDelete)
        {
            return;
        }

        entry.State = EntityState.Modified;
        softDelete.MarkDeleted();

        ApplyModified(entry.Entity, now, userId);
    }
}
