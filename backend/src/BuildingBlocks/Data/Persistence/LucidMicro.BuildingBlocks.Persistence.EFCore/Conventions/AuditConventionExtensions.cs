using LucidMicro.BuildingBlocks.Domain.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;

internal static class AuditConventionExtensions
{
    private const int UserIdMaxLength = 128;
    private const int TimestampPrecision = 3;
    private const string CreatedAtColumnName = "created_at";
    private const string CreatedByColumnName = "created_by";
    private const string LastModifiedAtColumnName = "last_modified_at";
    private const string LastModifiedByColumnName = "last_modified_by";

    public static void ApplyAuditConventions(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is not null
                || entityType.IsOwned()
                || !typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var entity = modelBuilder.Entity(entityType.ClrType);

            entity.Property<DateTimeOffset>(nameof(IAuditable.CreatedAt))
                .HasColumnName(CreatedAtColumnName)
                .HasPrecision(TimestampPrecision)
                .IsRequired();

            entity.Property<string?>(nameof(IAuditable.CreatedBy))
                .HasColumnName(CreatedByColumnName)
                .HasMaxLength(UserIdMaxLength);

            entity.Property<DateTimeOffset?>(nameof(IAuditable.LastModifiedAt))
                .HasColumnName(LastModifiedAtColumnName)
                .HasPrecision(TimestampPrecision);

            entity.Property<string?>(nameof(IAuditable.LastModifiedBy))
                .HasColumnName(LastModifiedByColumnName)
                .HasMaxLength(UserIdMaxLength);
        }
    }
}
