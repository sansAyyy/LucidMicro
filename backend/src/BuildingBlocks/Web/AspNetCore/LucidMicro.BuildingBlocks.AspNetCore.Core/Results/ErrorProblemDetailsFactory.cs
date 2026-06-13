using System.Diagnostics;
using LucidMicro.BuildingBlocks.Application.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.BuildingBlocks.AspNetCore.Results;

internal static class ErrorProblemDetailsFactory
{
    public static ProblemDetails Create(
        Error error,
        int statusCode,
        HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Message
        };

        problemDetails.Extensions["code"] = error.Code;
        problemDetails.Extensions["errorType"] = error.Type.ToString();
        problemDetails.Extensions["traceId"] = GetTraceId(httpContext);

        return problemDetails;
    }

    private static string? GetTraceId(HttpContext? httpContext)
    {
        var traceId = Activity.Current?.TraceId.ToString();

        return string.IsNullOrWhiteSpace(traceId)
            ? httpContext?.TraceIdentifier
            : traceId;
    }
}
