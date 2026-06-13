using Microsoft.AspNetCore.Http;

namespace LucidMicro.BuildingBlocks.RateLimiting.AspNetCore.Options;

public sealed class LucidRateLimitingOptions
{
    public const string ConfigurationSectionName = "Lucid:RateLimiting";

    public bool Enabled { get; set; }

    public int PermitLimit { get; set; } = 100;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; }

    public int RejectionStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;
}
