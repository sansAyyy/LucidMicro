namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

public sealed record AccessTokenClaim(
    string Type,
    string Value);
