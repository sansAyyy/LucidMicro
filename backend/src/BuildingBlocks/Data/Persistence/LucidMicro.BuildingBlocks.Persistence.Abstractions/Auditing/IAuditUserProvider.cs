namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Auditing;

public interface IAuditUserProvider
{
    string? GetCurrentUserId();
}
