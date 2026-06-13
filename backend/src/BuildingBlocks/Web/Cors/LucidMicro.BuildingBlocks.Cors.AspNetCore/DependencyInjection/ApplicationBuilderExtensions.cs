using LucidMicro.BuildingBlocks.Cors.AspNetCore.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Cors.AspNetCore.DependencyInjection;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLucidCors(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.ApplicationServices
            .GetRequiredService<IOptions<LucidCorsOptions>>()
            .Value;

        return options.Enabled
            ? app.UseCors(LucidCorsOptions.PolicyName)
            : app;
    }
}
