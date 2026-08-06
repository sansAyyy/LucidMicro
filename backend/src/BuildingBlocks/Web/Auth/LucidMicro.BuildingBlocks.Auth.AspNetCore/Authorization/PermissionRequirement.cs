using Microsoft.AspNetCore.Authorization;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
