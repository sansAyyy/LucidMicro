namespace LucidMicro.BuildingBlocks.Cors.AspNetCore.Options;

public sealed class LucidCorsOptions
{
    public const string ConfigurationSectionName = "Lucid:Cors";

    public const string PolicyName = "LucidCors";

    public bool Enabled { get; set; }

    public string[] AllowedOrigins { get; set; } = [];

    public string[] AllowedMethods { get; set; } =
    [
        "GET",
        "POST",
        "PUT",
        "DELETE",
        "OPTIONS"
    ];

    public string[] AllowedHeaders { get; set; } =
    [
        "Authorization",
        "Content-Type"
    ];

    public bool AllowCredentials { get; set; }
}
