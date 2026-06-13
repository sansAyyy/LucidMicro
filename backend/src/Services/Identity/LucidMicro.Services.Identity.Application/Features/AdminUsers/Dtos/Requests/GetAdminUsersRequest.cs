namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;

public sealed record GetAdminUsersRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Keyword { get; init; }
}
