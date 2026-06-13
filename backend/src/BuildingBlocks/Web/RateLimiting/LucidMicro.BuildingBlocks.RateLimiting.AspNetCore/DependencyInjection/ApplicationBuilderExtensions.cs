using LucidMicro.BuildingBlocks.RateLimiting.AspNetCore.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.RateLimiting.AspNetCore.DependencyInjection;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLucidRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.ApplicationServices
            .GetRequiredService<IOptions<LucidRateLimitingOptions>>()
            .Value;

        return options.Enabled
            ? app.UseRateLimiter()
            : app;
    }
}
