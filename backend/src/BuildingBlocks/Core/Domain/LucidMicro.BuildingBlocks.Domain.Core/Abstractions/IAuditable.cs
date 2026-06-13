namespace LucidMicro.BuildingBlocks.Domain.Core.Abstractions;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }

    string? CreatedBy { get; }

    DateTimeOffset? LastModifiedAt { get; }

    string? LastModifiedBy { get; }

    void MarkCreated(DateTimeOffset createdAt, string? createdBy = null);

    void MarkModified(DateTimeOffset modifiedAt, string? modifiedBy = null);
}
