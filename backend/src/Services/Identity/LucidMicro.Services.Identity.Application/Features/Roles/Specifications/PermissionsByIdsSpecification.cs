using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Specifications;

public sealed class PermissionsByIdsSpecification : Specification<Permission>
{
    public PermissionsByIdsSpecification(IReadOnlyCollection<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        Where(permission => ids.Contains(permission.Id));
    }
}
