namespace LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;

public sealed record UpdateRoleRequest
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public bool IsEnabled { get; init; } = true;
}
