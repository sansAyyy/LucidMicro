namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string? UserId { get; }

    string? UserName { get; }

    string? Email { get; }
}
