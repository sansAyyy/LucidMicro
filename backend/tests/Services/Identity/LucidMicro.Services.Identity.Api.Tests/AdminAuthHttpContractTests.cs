using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Services.Identity.Api.Controllers;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LucidMicro.Services.Identity.Api.Tests;

public sealed class AdminAuthHttpContractTests
{
    [Fact]
    public async Task Login_ReturnsTokenResponseContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin-auth/login", new
        {
            loginName = "admin",
            password = "secret"
        });

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("access-token", json.RootElement.GetProperty("accessToken").GetString());
        Assert.True(json.RootElement.TryGetProperty("expiresAt", out _));
        Assert.Equal("refresh-token", json.RootElement.GetProperty("refreshToken").GetString());
        Assert.True(json.RootElement.TryGetProperty("refreshTokenExpiresAt", out _));
    }

    [Fact]
    public async Task Refresh_ReturnsTokenResponseContract_WhenRefreshTokenIsValid()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin-auth/refresh", new
        {
            refreshToken = "valid-refresh-token"
        });

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("access-token", json.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal("refresh-token", json.RootElement.GetProperty("refreshToken").GetString());
    }

    [Theory]
    [InlineData("invalid-refresh-token")]
    [InlineData("access-token")]
    public async Task Refresh_ReturnsUnauthorized_WhenRefreshTokenIsInvalid(string refreshToken)
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin-auth/refresh", new
        {
            refreshToken
        });

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Identity.AdminAuth.InvalidRefreshToken", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Refresh_ReturnsValidationError_WhenRefreshTokenIsMissing()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin-auth/refresh", new { });

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Identity.AdminAuth.Validation", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Refresh_ReturnsServerErrorWithTraceId_WhenUnhandledExceptionOccurs()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin-auth/refresh", new
        {
            refreshToken = "throw"
        });

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", json.RootElement.GetProperty("title").GetString());
        Assert.Equal("Server.Error", json.RootElement.GetProperty("code").GetString());
        Assert.Equal("Failure", json.RootElement.GetProperty("errorType").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task OpenApi_ReturnsIdentityDocumentContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("LucidMicro Identity API", json.RootElement.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", json.RootElement.GetProperty("info").GetProperty("version").GetString());
        Assert.Equal(
            "http",
            json.RootElement
                .GetProperty("components")
                .GetProperty("securitySchemes")
                .GetProperty("Bearer")
                .GetProperty("type")
                .GetString());
    }

    [Fact]
    public async Task Scalar_ReturnsApiReferencePage()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/scalar");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("scalar", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cors_Preflight_ReturnsAllowedOrigin()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/admin-auth/login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Authorization, Content-Type");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Contains("Authorization", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }

    [Fact]
    public async Task Me_ReturnsCurrentUserWithPermissionsContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.GetAsync("/api/admin-auth/me");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TestAdminAuthApplicationService.AdminUserId, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("admin", json.RootElement.GetProperty("userName").GetString());
        Assert.Equal("admin@example.com", json.RootElement.GetProperty("email").GetString());
        Assert.Equal("Admin", json.RootElement.GetProperty("displayName").GetString());
        Assert.True(json.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal("identity.admin-users.read", json.RootElement.GetProperty("permissions")[0].GetString());
        Assert.Equal("identity.roles.read", json.RootElement.GetProperty("permissions")[1].GetString());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(content);
    }

    private static string CreateAdminToken()
    {
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("change-me-to-a-secure-32-byte-minimum-signing-key"));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "LucidMicro.Identity",
            audience: "LucidMicro.Admin",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "admin-id"),
                new Claim(JwtRegisteredClaimNames.UniqueName, "admin")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AddAdminToken(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateAdminToken());
    }

    private sealed class TestApiFactory : WebApplicationFactory<AdminAuthController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IAdminAuthApplicationService>();
                services.AddSingleton<IAdminAuthApplicationService, TestAdminAuthApplicationService>();
            });
        }
    }

    private sealed class TestAdminAuthApplicationService : IAdminAuthApplicationService
    {
        public static readonly Guid AdminUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        private static readonly LoginAdminUserResponse TokenResponse = new(
            "access-token",
            new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero),
            "refresh-token",
            new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero));

        public Task<Result<LoginAdminUserResponse>> LoginAsync(
            LoginAdminUserRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<LoginAdminUserResponse>.Success(TokenResponse));
        }

        public Task<Result<LoginAdminUserResponse>> RefreshAsync(
            RefreshAdminUserTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.RefreshToken == "throw")
            {
                throw new InvalidOperationException("Test exception.");
            }

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Task.FromResult(Result<LoginAdminUserResponse>.Failure(
                    Error.Validation("Identity.AdminAuth.Validation", "refreshToken is required.")));
            }

            if (request.RefreshToken != "valid-refresh-token")
            {
                return Task.FromResult(Result<LoginAdminUserResponse>.Failure(
                    Error.Unauthorized("Identity.AdminAuth.InvalidRefreshToken", "Invalid refresh token.")));
            }

            return Task.FromResult(Result<LoginAdminUserResponse>.Success(TokenResponse));
        }

        public Task<Result<CurrentAdminUserResponse>> GetCurrentAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<CurrentAdminUserResponse>.Success(new CurrentAdminUserResponse(
                AdminUserId,
                "admin",
                "admin@example.com",
                "Admin",
                null,
                true,
                null,
                ["identity.admin-users.read", "identity.roles.read"])));
        }

        public Task<Result> ChangeCurrentPasswordAsync(
            ChangeCurrentAdminUserPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
