using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Specifications;

internal sealed class AdminUsersListSpecification : Specification<AdminUser>
{
    public AdminUsersListSpecification(string? keyword)
    {
        var normalizedKeyword = NormalizeOptional(keyword);
        if (normalizedKeyword is not null)
        {
            Where(adminUser =>
                adminUser.UserName.Contains(normalizedKeyword)
                || adminUser.Email.Contains(normalizedKeyword)
                || adminUser.DisplayName.Contains(normalizedKeyword)
                || (adminUser.PhoneNumber != null && adminUser.PhoneNumber.Contains(normalizedKeyword)));
        }

        OrderByDescending(adminUser => adminUser.CreatedAt);
        ApplyNoTracking();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
