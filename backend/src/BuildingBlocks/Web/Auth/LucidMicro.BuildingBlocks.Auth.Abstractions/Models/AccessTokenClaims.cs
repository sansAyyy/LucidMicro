namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

public sealed record AccessTokenClaims(
    string Subject,
    string? Name = null,
    IReadOnlyDictionary<string, string>? AdditionalClaims = null)
{
    public IReadOnlyList<AccessTokenClaim> AdditionalClaimValues { get; init; } = [];
}
