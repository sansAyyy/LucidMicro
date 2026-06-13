using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Specifications;

internal sealed class AdminUserByPhoneNumberSpecification : Specification<AdminUser>
{
    public AdminUserByPhoneNumberSpecification(string phoneNumber, Guid? excludedId)
    {
        Where(adminUser => adminUser.PhoneNumber == phoneNumber
                           && (!excludedId.HasValue || adminUser.Id != excludedId.Value));
    }
}
