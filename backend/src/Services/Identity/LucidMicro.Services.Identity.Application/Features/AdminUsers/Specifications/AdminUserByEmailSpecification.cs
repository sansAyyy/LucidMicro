using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Specifications;

internal sealed class AdminUserByEmailSpecification : Specification<AdminUser>
{
    public AdminUserByEmailSpecification(string email, Guid? excludedId)
    {
        if (excludedId is null)
        {
            Where(adminUser => adminUser.Email == email);
            return;
        }

        Where(adminUser => adminUser.Email == email && adminUser.Id != excludedId.Value);
    }
}
