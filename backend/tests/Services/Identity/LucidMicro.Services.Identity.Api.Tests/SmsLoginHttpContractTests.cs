using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Services.Identity.Api.Controllers;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LucidMicro.Services.Identity.Api.Tests;

public sealed class SmsLoginHttpContractTests
{
    [Fact]
    public async Task SendCode_ReturnsNoContent_WhenRequestIsValid()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sms-login/codes", new
        {
            phoneNumber = "13800138000"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SendCode_ReturnsBadRequest_WhenRequestIsInvalid()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sms-login/codes", new { });
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Identity.SmsLogin.Validation", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Login_ReturnsTokenResponseContract_WhenCodeIsValid()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sms-login", new
        {
            phoneNumber = "13800138000",
            code = "123456"
        });
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("access-token", json.RootElement.GetProperty("accessToken").GetString());
        Assert.True(json.RootElement.TryGetProperty("expiresAt", out _));
        Assert.Equal("refresh-token", json.RootElement.GetProperty("refreshToken").GetString());
        Assert.True(json.RootElement.TryGetProperty("refreshTokenExpiresAt", out _));
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenCodeIsMissing()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sms-login", new
        {
            phoneNumber = "13800138000"
        });
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Identity.SmsLogin.Validation", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(content);
    }

    private sealed class TestApiFactory : WebApplicationFactory<SmsLoginController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ISmsLoginApplicationService>();
                services.AddSingleton<ISmsLoginApplicationService, TestSmsLoginApplicationService>();
            });
        }
    }

    private sealed class TestSmsLoginApplicationService : ISmsLoginApplicationService
    {
        private static readonly SmsLoginResponse TokenResponse = new(
            "access-token",
            new DateTimeOffset(2026, 5, 28, 13, 0, 0, TimeSpan.Zero),
            "refresh-token",
            new DateTimeOffset(2026, 6, 4, 13, 0, 0, TimeSpan.Zero));

        public Task<Result> SendCodeAsync(
            SendSmsLoginCodeRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return Task.FromResult(Result.Failure(
                    Error.Validation("Identity.SmsLogin.Validation", "phoneNumber is required.")));
            }

            return Task.FromResult(Result.Success());
        }

        public Task<Result<SmsLoginResponse>> LoginAsync(
            LoginBySmsCodeRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Task.FromResult(Result<SmsLoginResponse>.Failure(
                    Error.Validation("Identity.SmsLogin.Validation", "code is required.")));
            }

            return Task.FromResult(Result<SmsLoginResponse>.Success(TokenResponse));
        }
    }
}
