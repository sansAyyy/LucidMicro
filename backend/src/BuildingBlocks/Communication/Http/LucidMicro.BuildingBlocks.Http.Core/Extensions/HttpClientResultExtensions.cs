using System.Net.Http.Json;
using System.Text.Json;
using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.BuildingBlocks.Http.Core.Extensions;

public static class HttpClientResultExtensions
{
    public static async Task<Result> PostAsJsonForResultAsync<TValue>(
        this HttpClient httpClient,
        string? requestUri,
        TValue value,
        string requestFailedCode,
        string unavailableCode,
        string serviceName = "HTTP service",
        string? timeoutMessage = null,
        string? unavailableMessage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFailedCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(unavailableCode);

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                requestUri,
                value,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var errorMessage = await CreateRequestFailedMessageAsync(
                response,
                NormalizeServiceName(serviceName),
                cancellationToken);

            return Result.Failure(Error.Failure(
                requestFailedCode,
                errorMessage));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(Error.Failure(
                unavailableCode,
                string.IsNullOrWhiteSpace(timeoutMessage)
                    ? "HTTP request timed out."
                    : timeoutMessage));
        }
        catch (HttpRequestException)
        {
            return Result.Failure(Error.Failure(
                unavailableCode,
                string.IsNullOrWhiteSpace(unavailableMessage)
                    ? "HTTP service is unavailable."
                    : unavailableMessage));
        }
    }

    private static async Task<string> CreateRequestFailedMessageAsync(
        HttpResponseMessage response,
        string serviceName,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var baseMessage = $"{serviceName} returned HTTP status code {statusCode}.";
        var problemDetails = await TryReadProblemDetailsAsync(response, cancellationToken);

        if (problemDetails is null)
        {
            return baseMessage;
        }

        if (string.IsNullOrWhiteSpace(problemDetails.Code))
        {
            return string.IsNullOrWhiteSpace(problemDetails.Title)
                ? baseMessage
                : $"{baseMessage} {problemDetails.Title}";
        }

        return string.IsNullOrWhiteSpace(problemDetails.Title)
            ? $"{baseMessage} Downstream error code: {problemDetails.Code}."
            : $"{baseMessage} Downstream error code: {problemDetails.Code}. {problemDetails.Title}";
    }

    private static async Task<DownstreamProblemDetails?> TryReadProblemDetailsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content is null)
        {
            return null;
        }

        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        try
        {
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var title = TryGetString(root, "title");
            var code = TryGetString(root, "code");

            return string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(code)
                ? null
                : new DownstreamProblemDetails(title, code);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string NormalizeServiceName(string serviceName)
    {
        return string.IsNullOrWhiteSpace(serviceName)
            ? "HTTP service"
            : serviceName;
    }

    private sealed record DownstreamProblemDetails(string? Title, string? Code);
}
