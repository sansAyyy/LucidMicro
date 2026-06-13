using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Specifications;

public sealed class RoleByCodeSpecification : Specification<Role>
{
    public RoleByCodeSpecification(string code, Guid? excludedId = null)
    {
        Where(role => role.Code == code && (excludedId == null || role.Id != excludedId.Value));
    }
}
