namespace LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;

public sealed record GetRolesRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Keyword { get; init; }
}
