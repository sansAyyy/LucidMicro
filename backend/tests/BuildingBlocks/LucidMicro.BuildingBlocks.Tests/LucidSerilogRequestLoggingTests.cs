using LucidMicro.BuildingBlocks.Logging.SerilogIntegration.RequestLogging;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class LucidSerilogRequestLoggingTests
{
    [Theory]
    [InlineData(StatusCodes.Status200OK, LogEventLevel.Information)]
    [InlineData(StatusCodes.Status302Found, LogEventLevel.Information)]
    [InlineData(StatusCodes.Status400BadRequest, LogEventLevel.Warning)]
    [InlineData(StatusCodes.Status404NotFound, LogEventLevel.Warning)]
    [InlineData(StatusCodes.Status500InternalServerError, LogEventLevel.Error)]
    [InlineData(StatusCodes.Status503ServiceUnavailable, LogEventLevel.Error)]
    public void GetLevel_ReturnsExpectedLevel_ForStatusCode(int statusCode, LogEventLevel expectedLevel)
    {
        var httpContext = CreateHttpContext(statusCode, "/api/test");

        var level = LucidSerilogRequestLogging.GetLevel(httpContext, elapsed: 1, exception: null);

        Assert.Equal(expectedLevel, level);
    }

    [Fact]
    public void GetLevel_ReturnsError_WhenExceptionExists()
    {
        var httpContext = CreateHttpContext(StatusCodes.Status200OK, "/api/test");

        var level = LucidSerilogRequestLogging.GetLevel(
            httpContext,
            elapsed: 1,
            exception: new InvalidOperationException("Test exception."));

        Assert.Equal(LogEventLevel.Error, level);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthz")]
    [InlineData("/live")]
    [InlineData("/ready")]
    public void GetLevel_ReturnsDebug_ForSuccessfulHealthCheck(string path)
    {
        var httpContext = CreateHttpContext(StatusCodes.Status200OK, path);

        var level = LucidSerilogRequestLogging.GetLevel(httpContext, elapsed: 1, exception: null);

        Assert.Equal(LogEventLevel.Debug, level);
    }

    [Fact]
    public void GetLevel_ReturnsError_ForFailedHealthCheck()
    {
        var httpContext = CreateHttpContext(StatusCodes.Status503ServiceUnavailable, "/health");

        var level = LucidSerilogRequestLogging.GetLevel(httpContext, elapsed: 1, exception: null);

        Assert.Equal(LogEventLevel.Error, level);
    }

    private static DefaultHttpContext CreateHttpContext(int statusCode, string path)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.Response.StatusCode = statusCode;

        return httpContext;
    }
}
