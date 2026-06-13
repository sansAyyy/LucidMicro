using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Auditing;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.Auditing;

public sealed class CurrentUserAuditUserProvider : IAuditUserProvider
{
    private readonly ICurrentUser _currentUser;

    public CurrentUserAuditUserProvider(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public string? GetCurrentUserId()
    {
        return _currentUser.IsAuthenticated ? _currentUser.UserId : null;
    }
}
