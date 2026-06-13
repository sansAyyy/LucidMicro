using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Specifications;

public sealed class RolesByIdsSpecification : Specification<Role>
{
    public RolesByIdsSpecification(IReadOnlyCollection<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        Where(role => ids.Contains(role.Id));
    }
}
