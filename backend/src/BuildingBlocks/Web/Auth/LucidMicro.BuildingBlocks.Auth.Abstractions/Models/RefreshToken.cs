namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

public sealed record RefreshToken(
    string Token,
    DateTimeOffset ExpiresAt);
