using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }
}
