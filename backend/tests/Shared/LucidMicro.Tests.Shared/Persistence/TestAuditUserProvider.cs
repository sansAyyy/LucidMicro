using LucidMicro.BuildingBlocks.Persistence.Abstractions.Auditing;

namespace LucidMicro.Tests.Shared.Persistence;

public sealed class TestAuditUserProvider : IAuditUserProvider
{
    private readonly string? _userId;

    public TestAuditUserProvider(string? userId)
    {
        _userId = userId;
    }

    public string? GetCurrentUserId()
    {
        return _userId;
    }
}
