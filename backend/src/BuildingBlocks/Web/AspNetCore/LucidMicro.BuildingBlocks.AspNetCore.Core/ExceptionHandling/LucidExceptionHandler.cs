using LucidMicro.BuildingBlocks.Application.Exceptions;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.BuildingBlocks.Domain.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LucidMicro.BuildingBlocks.AspNetCore.ExceptionHandling;

public sealed class LucidExceptionHandler : IExceptionHandler
{
    private const string DomainErrorCode = "Domain.Validation";
    private const string ServerErrorCode = "Server.Error";
    private const string ServerErrorMessage = "An unexpected error occurred.";

    private readonly ILogger<LucidExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public LucidExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<LucidExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException
            && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var error = MapException(exception);
        var statusCode = ErrorHttpStatusCodeMapper.Map(error.Type);

        LogException(exception, statusCode);

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = ErrorProblemDetailsFactory.Create(error, statusCode, httpContext)
        });
    }

    private static Error MapException(Exception exception)
    {
        return exception switch
        {
            BusinessException businessException => businessException.Error,
            DomainException domainException => Error.Validation(
                domainException.Code ?? DomainErrorCode,
                domainException.Message),
            _ => Error.Failure(ServerErrorCode, ServerErrorMessage)
        };
    }

    private void LogException(Exception exception, int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
            return;
        }

        _logger.LogWarning(exception, "Handled exception occurred.");
    }
}
