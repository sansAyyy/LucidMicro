using LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;
using LucidMicro.Services.__ServiceName__.Domain.Entities.__FeatureName__;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LucidMicro.Services.__ServiceName__.Infrastructure.Persistence.Configurations;

public sealed class __EntityName__Configuration : IEntityTypeConfiguration<__EntityName__>
{
    public void Configure(EntityTypeBuilder<__EntityName__> builder)
    {
        builder.ToTable("__TableName__");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id");

        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(__NameMaxLength__)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasIndex(entity => entity.Name)
            .IsUnique()
            .HasDatabaseName("ix___TableName___name")
            .HasSoftDeleteFilter();
    }
}
