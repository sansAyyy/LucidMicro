using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using Microsoft.AspNetCore.Http;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.Services;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId => FindClaimValue(
        ClaimTypes.NameIdentifier,
        JwtRegisteredClaimNames.Sub,
        "sub");

    public string? UserName => FindClaimValue(
        ClaimTypes.Name,
        JwtRegisteredClaimNames.Name,
        "name");

    public string? Email => FindClaimValue(
        ClaimTypes.Email,
        JwtRegisteredClaimNames.Email,
        "email");

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    private string? FindClaimValue(params string[] claimTypes)
    {
        if (!IsAuthenticated)
        {
            return null;
        }

        return claimTypes
            .Select(claimType => Principal?.FindFirst(claimType)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
