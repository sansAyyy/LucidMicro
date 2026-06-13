namespace LucidMicro.BuildingBlocks.OpenApi.AspNetCore.Options;

public sealed class LucidOpenApiOptions
{
    public const string ConfigurationSectionName = "Lucid:OpenApi";

    public string Title { get; set; } = "LucidMicro API";

    public string Version { get; set; } = "v1";

    public string? Description { get; set; }

    public bool EnableBearerSecurity { get; set; } = true;
}
