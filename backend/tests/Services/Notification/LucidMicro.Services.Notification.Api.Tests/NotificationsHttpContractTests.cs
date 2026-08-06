using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Contracts.Notification;
using LucidMicro.Contracts.Notification.Http.Requests;
using LucidMicro.Services.Notification.Api.Controllers;
using LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Responses;
using LucidMicro.Services.Notification.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LucidMicro.Services.Notification.Api.Tests;

public sealed class NotificationsHttpContractTests
{
    [Fact]
    public async Task GetList_ReturnsPagedNotificationContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.GetAsync("/api/notifications?pageNumber=1&pageSize=1");

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, json.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("pageNumber").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("pageSize").GetInt32());
        var item = json.RootElement.GetProperty("items").EnumerateArray().Single();
        Assert.Equal(TestNotificationApplicationService.NotificationId, item.GetProperty("id").GetGuid());
        Assert.Equal("admin@example.com", item.GetProperty("recipient").GetString());
    }

    [Fact]
    public async Task GetById_ReturnsNotificationContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.GetAsync($"/api/notifications/{TestNotificationApplicationService.NotificationId}");

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TestNotificationApplicationService.NotificationId, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("admin@example.com", json.RootElement.GetProperty("recipient").GetString());
        Assert.Equal(NotificationChannels.InApp, json.RootElement.GetProperty("channel").GetString());
        Assert.Equal("Welcome", json.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Hello", json.RootElement.GetProperty("content").GetString());
        Assert.Equal(NotificationStatuses.Sent, json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNotificationDoesNotExist()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.GetAsync($"/api/notifications/{Guid.NewGuid()}");

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Notification.NotFound", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task GetList_ReturnsUnauthorized_WhenAnonymous()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notifications?pageNumber=1&pageSize=1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsUnauthorized_WhenAnonymous()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/notifications/{TestNotificationApplicationService.NotificationId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetList_ReturnsForbidden_WhenPermissionIsMissing()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client, includeReadPermission: false);

        var response = await client.GetAsync("/api/notifications?pageNumber=1&pageSize=1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InternalCreate_ReturnsCreatedNotificationContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/internal/notifications",
            new SendNotificationRequest(
                "admin@example.com",
                NotificationChannels.InApp,
                "Welcome",
                "Hello"));

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            $"/api/notifications/{TestNotificationApplicationService.NotificationId}",
            response.Headers.Location?.ToString());
        Assert.Equal(TestNotificationApplicationService.NotificationId, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("admin@example.com", json.RootElement.GetProperty("recipient").GetString());
        Assert.Equal(NotificationChannels.InApp, json.RootElement.GetProperty("channel").GetString());
        Assert.Equal("Welcome", json.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Hello", json.RootElement.GetProperty("content").GetString());
        Assert.Equal(NotificationStatuses.Sent, json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task InternalCreate_ReturnsValidationProblem_WhenApplicationValidationFails()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/internal/notifications",
            new SendNotificationRequest(
                "",
                NotificationChannels.InApp,
                "Welcome",
                "Hello"));

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Notification.Validation", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task InternalCreate_ReturnsValidationProblem_WhenChannelIsInvalid()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/internal/notifications",
            new SendNotificationRequest(
                "admin@example.com",
                "Unknown",
                "Welcome",
                "Hello"));

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Notification.Validation", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task OpenApi_ReturnsNotificationDocumentContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("LucidMicro Notification API", json.RootElement.GetProperty("info").GetProperty("title").GetString());
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
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/notifications");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Authorization, Content-Type");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Contains("Authorization", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(content);
    }

    private static string CreateAdminToken(bool includeReadPermission = true)
    {
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("change-me-to-a-secure-32-byte-minimum-signing-key"));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "LucidMicro.Identity",
            audience: "LucidMicro.Admin",
            claims: CreateAdminClaims(includeReadPermission),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AddAdminToken(HttpClient client, bool includeReadPermission = true)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                CreateAdminToken(includeReadPermission));
    }

    private static Claim[] CreateAdminClaims(bool includeReadPermission)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "admin-id"),
            new Claim(JwtRegisteredClaimNames.UniqueName, "admin")
        };

        if (includeReadPermission)
        {
            claims.Add(new Claim("permission", "notification.notifications.read"));
        }

        return claims.ToArray();
    }

    private sealed class TestApiFactory : WebApplicationFactory<NotificationsController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<INotificationApplicationService>();
                services.AddSingleton<INotificationApplicationService, TestNotificationApplicationService>();
            });
        }
    }

    private sealed class TestNotificationApplicationService : INotificationApplicationService
    {
        public static readonly Guid NotificationId = Guid.Parse("3377e91e-6fd2-4e2c-a2f9-80ac9f9acb01");

        public Task<Result<PageResult<NotificationResponse>>> GetListAsync(
            GetNotificationsRequest request,
            CancellationToken cancellationToken = default)
        {
            var pageResult = new PageResult<NotificationResponse>(
                [
                    CreateResponse(
                        "admin@example.com",
                        NotificationChannel.InApp,
                        "Welcome",
                        "Hello")
                ],
                2,
                request.PageNumber,
                request.PageSize);

            return Task.FromResult(Result<PageResult<NotificationResponse>>.Success(pageResult));
        }

        public Task<Result<NotificationResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (id != NotificationId)
            {
                return Task.FromResult(Result<NotificationResponse>.Failure(
                    Error.NotFound("Notification.NotFound", $"Notification '{id}' was not found.")));
            }

            return Task.FromResult(Result<NotificationResponse>.Success(CreateResponse(
                "admin@example.com",
                NotificationChannel.InApp,
                "Welcome",
                "Hello")));
        }

        public Task<Result<NotificationResponse>> CreateAsync(
            CreateNotificationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Recipient))
            {
                return Task.FromResult(Result<NotificationResponse>.Failure(
                    Error.Validation("Notification.Validation", "recipient is required.")));
            }

            return Task.FromResult(Result<NotificationResponse>.Success(CreateResponse(
                request.Recipient,
                request.Channel,
                request.Subject,
                request.Content ?? string.Empty)));
        }

        private static NotificationResponse CreateResponse(
            string recipient,
            NotificationChannel channel,
            string? subject,
            string content)
        {
            return new NotificationResponse(
                NotificationId,
                recipient,
                channel,
                subject,
                content,
                NotificationStatus.Sent,
                new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero),
                null,
                null);
        }
    }
}
