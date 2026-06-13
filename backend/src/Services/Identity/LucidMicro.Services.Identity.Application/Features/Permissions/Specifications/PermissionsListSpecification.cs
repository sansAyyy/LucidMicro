using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;

namespace LucidMicro.Services.Identity.Application.Features.Permissions.Specifications;

public sealed class PermissionsListSpecification : Specification<Permission>
{
    public PermissionsListSpecification()
    {
        OrderBy(permission => permission.SortOrder);
        OrderBy(permission => permission.Code);
    }
}
