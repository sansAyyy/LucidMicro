using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace LucidMicro.BuildingBlocks.Logging.SerilogIntegration.RequestLogging;

internal static class LucidSerilogRequestLogging
{
    public static LogEventLevel GetLevel(
        HttpContext httpContext,
        double elapsed,
        Exception? exception)
    {
        if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            return LogEventLevel.Warning;
        }

        if (IsHealthCheck(httpContext.Request.Path))
        {
            return LogEventLevel.Debug;
        }

        return LogEventLevel.Information;
    }

    private static bool IsHealthCheck(PathString path)
    {
        return path.StartsWithSegments("/health")
            || path.StartsWithSegments("/healthz")
            || path.StartsWithSegments("/live")
            || path.StartsWithSegments("/ready");
    }
}
