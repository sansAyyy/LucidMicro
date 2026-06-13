using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Services.Identity.Api.Controllers;
using LucidMicro.Services.Identity.Application.Features.Permissions.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Permissions.Dtos.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LucidMicro.Services.Identity.Api.Tests;

public sealed class PermissionsHttpContractTests
{
    [Fact]
    public async Task GetList_ReturnsPermissionsContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.GetAsync("/api/permissions");
        using var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = json.RootElement.EnumerateArray().Single();
        Assert.Equal(TestPermissionApplicationService.PermissionId, item.GetProperty("id").GetGuid());
        Assert.Equal("identity.roles.read", item.GetProperty("code").GetString());
        Assert.Equal("查看角色", item.GetProperty("name").GetString());
        Assert.Equal("identity", item.GetProperty("groupCode").GetString());
        Assert.Equal("roles", item.GetProperty("resourceCode").GetString());
        Assert.Equal("read", item.GetProperty("action").GetString());
        Assert.True(item.GetProperty("isEnabled").GetBoolean());
    }

    [Fact]
    public async Task GetList_ReturnsUnauthorized_WhenAnonymous()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/permissions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private sealed class TestApiFactory : WebApplicationFactory<PermissionsController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IPermissionApplicationService>();
                services.AddSingleton<IPermissionApplicationService, TestPermissionApplicationService>();
            });
        }
    }

    private sealed class TestPermissionApplicationService : IPermissionApplicationService
    {
        public static readonly Guid PermissionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public Task<Result<IReadOnlyList<PermissionResponse>>> GetListAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PermissionResponse> permissions =
            [
                new PermissionResponse(
                    PermissionId,
                    "identity.roles.read",
                    "查看角色",
                    null,
                    "identity",
                    "身份认证",
                    "roles",
                    "角色",
                    "read",
                    2010,
                    true)
            ];

            return Task.FromResult(Result<IReadOnlyList<PermissionResponse>>.Success(permissions));
        }
    }
}
