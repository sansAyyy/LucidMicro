using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Specifications;

internal sealed class AdminUserByUserNameSpecification : Specification<AdminUser>
{
    public AdminUserByUserNameSpecification(string userName, Guid? excludedId)
    {
        if (excludedId is null)
        {
            Where(adminUser => adminUser.UserName == userName);
            return;
        }

        Where(adminUser => adminUser.UserName == userName && adminUser.Id != excludedId.Value);
    }
}
