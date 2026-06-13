using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Specifications;

internal sealed class AdminUserByLoginNameSpecification : Specification<AdminUser>
{
    public AdminUserByLoginNameSpecification(string loginName)
    {
        Where(adminUser => adminUser.UserName == loginName || adminUser.Email == loginName);
    }
}
