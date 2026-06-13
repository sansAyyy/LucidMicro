using LucidMicro.BuildingBlocks.Persistence.Abstractions.Auditing;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Auditing;

public sealed class DefaultAuditUserProvider : IAuditUserProvider
{
    public string? GetCurrentUserId()
    {
        return null;
    }
}
