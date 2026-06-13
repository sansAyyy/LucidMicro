using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;

public static class SoftDeleteIndexBuilderExtensions
{
    public static IndexBuilder HasSoftDeleteFilter(this IndexBuilder indexBuilder)
    {
        ArgumentNullException.ThrowIfNull(indexBuilder);

        return indexBuilder.HasFilter(SoftDeleteRelationalConventions.IsNotDeletedFilter);
    }

    public static IndexBuilder<TEntity> HasSoftDeleteFilter<TEntity>(this IndexBuilder<TEntity> indexBuilder)
    {
        ArgumentNullException.ThrowIfNull(indexBuilder);

        return indexBuilder.HasFilter(SoftDeleteRelationalConventions.IsNotDeletedFilter);
    }
}
