using LucidMicro.BuildingBlocks.OpenApi.AspNetCore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace LucidMicro.BuildingBlocks.OpenApi.AspNetCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidOpenApi(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        services
            .AddOptions<LucidOpenApiOptions>()
            .Bind(configurationSection)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Title), "Lucid OpenAPI title is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Version), "Lucid OpenAPI version is required.")
            .ValidateOnStart();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var openApiOptions = context.ApplicationServices
                    .GetRequiredService<IOptions<LucidOpenApiOptions>>()
                    .Value;

                document.Info.Title = openApiOptions.Title;
                document.Info.Version = openApiOptions.Version;
                document.Info.Description = openApiOptions.Description;

                if (openApiOptions.EnableBearerSecurity)
                {
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "JWT Authorization header using the Bearer scheme."
                    };
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
