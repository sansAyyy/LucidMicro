namespace LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;

public sealed record CreateRoleRequest
{
    public string? Code { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public bool IsEnabled { get; init; } = true;
}
