namespace LucidMicro.BuildingBlocks.Domain.Core.Abstractions;

public interface ISoftDelete
{
    bool IsDeleted { get; }

    void MarkDeleted();

    void Restore();
}
