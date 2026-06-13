using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Specifications;

public sealed class RolesListSpecification : Specification<Role>
{
    public RolesListSpecification(string? keyword)
    {
        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        if (normalizedKeyword is not null)
        {
            Where(role =>
                role.Code.Contains(normalizedKeyword)
                || role.Name.Contains(normalizedKeyword)
                || (role.Description != null && role.Description.Contains(normalizedKeyword)));
        }

        OrderByDescending(role => role.IsSystem);
        OrderBy(role => role.Code);
    }
}
