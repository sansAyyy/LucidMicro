namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

public sealed record AccessToken(
    string Token,
    DateTimeOffset ExpiresAt);
