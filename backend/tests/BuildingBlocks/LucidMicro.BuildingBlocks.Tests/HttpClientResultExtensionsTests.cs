using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LucidMicro.BuildingBlocks.Http.Core.Extensions;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class HttpClientResultExtensionsTests
{
    [Fact]
    public async Task PostAsJsonForResultAsync_ReturnsSuccess_WhenResponseIsSuccessful()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var httpClient = CreateHttpClient(handler);

        var result = await httpClient.PostAsJsonForResultAsync(
            "api/test",
            new TestRequest("hello"),
            "Test.RequestFailed",
            "Test.Unavailable",
            serviceName: "Test service");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(new Uri("http://test/api/test"), handler.RequestUri);
        using var body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("hello", body.RootElement.GetProperty("value").GetString());
    }

    [Fact]
    public async Task PostAsJsonForResultAsync_ReturnsRequestFailed_WhenResponseIsNotSuccessful()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var httpClient = CreateHttpClient(handler);

        var result = await httpClient.PostAsJsonForResultAsync(
            "api/test",
            new TestRequest("hello"),
            "Test.RequestFailed",
            "Test.Unavailable",
            serviceName: "Test service");

        Assert.True(result.IsFailure);
        Assert.Equal("Test.RequestFailed", result.Error.Code);
        Assert.Equal("Test service returned HTTP status code 400.", result.Error.Message);
    }

    [Fact]
    public async Task PostAsJsonForResultAsync_IncludesProblemDetails_WhenResponseHasProblemDetails()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new
            {
                title = "request is invalid.",
                code = "Downstream.Validation"
            })
        });
        var httpClient = CreateHttpClient(handler);

        var result = await httpClient.PostAsJsonForResultAsync(
            "api/test",
            new TestRequest("hello"),
            "Test.RequestFailed",
            "Test.Unavailable",
            serviceName: "Test service");

        Assert.True(result.IsFailure);
        Assert.Equal("Test.RequestFailed", result.Error.Code);
        Assert.Equal(
            "Test service returned HTTP status code 400. Downstream error code: Downstream.Validation. request is invalid.",
            result.Error.Message);
    }

    [Fact]
    public async Task PostAsJsonForResultAsync_IgnoresNonJsonErrorBody()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("plain text failure")
        });
        var httpClient = CreateHttpClient(handler);

        var result = await httpClient.PostAsJsonForResultAsync(
            "api/test",
            new TestRequest("hello"),
            "Test.RequestFailed",
            "Test.Unavailable",
            serviceName: "Test service");

        Assert.True(result.IsFailure);
        Assert.Equal("Test.RequestFailed", result.Error.Code);
        Assert.Equal("Test service returned HTTP status code 500.", result.Error.Message);
    }

    [Fact]
    public async Task PostAsJsonForResultAsync_ReturnsUnavailable_WhenRequestFails()
    {
        var handler = new TestHttpMessageHandler(_ => throw new HttpRequestException("Connection failed."));
        var httpClient = CreateHttpClient(handler);

        var result = await httpClient.PostAsJsonForResultAsync(
            "api/test",
            new TestRequest("hello"),
            "Test.RequestFailed",
            "Test.Unavailable",
            serviceName: "Test service",
            unavailableMessage: "Test service is unavailable.");

        Assert.True(result.IsFailure);
        Assert.Equal("Test.Unavailable", result.Error.Code);
        Assert.Equal("Test service is unavailable.", result.Error.Message);
    }

    [Fact]
    public async Task PostAsJsonForResultAsync_ReturnsUnavailable_WhenRequestTimesOut()
    {
        var handler = new TestHttpMessageHandler(_ => throw new OperationCanceledException("Timed out."));
        var httpClient = CreateHttpClient(handler);

        var result = await httpClient.PostAsJsonForResultAsync(
            "api/test",
            new TestRequest("hello"),
            "Test.RequestFailed",
            "Test.Unavailable",
            serviceName: "Test service",
            timeoutMessage: "Test service request timed out.");

        Assert.True(result.IsFailure);
        Assert.Equal("Test.Unavailable", result.Error.Code);
        Assert.Equal("Test service request timed out.", result.Error.Message);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        };
    }

    private sealed record TestRequest(string Value);

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public HttpMethod? RequestMethod { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _handler(request);
        }
    }
}
