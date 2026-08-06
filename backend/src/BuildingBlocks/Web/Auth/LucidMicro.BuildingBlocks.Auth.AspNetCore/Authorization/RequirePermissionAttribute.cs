using Microsoft.AspNetCore.Authorization;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Lucid.Permission:";

    public RequirePermissionAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        Policy = PolicyPrefix + permission.Trim();
    }
}
