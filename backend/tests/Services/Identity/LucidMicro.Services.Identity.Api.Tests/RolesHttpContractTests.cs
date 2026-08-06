using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Identity.Api.Controllers;
using LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LucidMicro.Services.Identity.Api.Tests;

public sealed class RolesHttpContractTests
{
    [Fact]
    public async Task GetList_ReturnsPagedRolesContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.GetAsync("/api/roles?pageNumber=1&pageSize=10&keyword=admin");
        using var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, json.RootElement.GetProperty("totalCount").GetInt32());
        var item = json.RootElement.GetProperty("items").EnumerateArray().Single();
        Assert.Equal(TestRoleApplicationService.RoleId, item.GetProperty("id").GetGuid());
        Assert.Equal("super-admin", item.GetProperty("code").GetString());
        Assert.True(item.GetProperty("isSystem").GetBoolean());
        Assert.True(item.GetProperty("isEnabled").GetBoolean());
    }

    [Fact]
    public async Task GetById_ReturnsRoleDetailContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.GetAsync($"/api/roles/{TestRoleApplicationService.RoleId}");
        using var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TestRoleApplicationService.RoleId, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("super-admin", json.RootElement.GetProperty("code").GetString());
        Assert.Equal(TestRoleApplicationService.PermissionId, json.RootElement.GetProperty("permissionIds")[0].GetGuid());
    }

    [Fact]
    public async Task AssignPermissions_ReturnsNoContent()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.PutAsJsonAsync(
            $"/api/roles/{TestRoleApplicationService.RoleId}/permissions",
            new AssignRolePermissionsRequest
            {
                PermissionIds = [TestRoleApplicationService.PermissionId]
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetList_ReturnsUnauthorized_WhenAnonymous()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/roles?pageNumber=1&pageSize=10");

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
                new Claim(JwtRegisteredClaimNames.UniqueName, "admin"),
                new Claim("permission", "identity.roles.read"),
                new Claim("permission", "identity.roles.manage"),
                new Claim("permission", "identity.roles.assign-permissions")
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

    private sealed class TestApiFactory : WebApplicationFactory<RolesController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IRoleApplicationService>();
                services.AddSingleton<IRoleApplicationService, TestRoleApplicationService>();
            });
        }
    }

    private sealed class TestRoleApplicationService : IRoleApplicationService
    {
        public static readonly Guid RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid PermissionId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public Task<Result<PageResult<RoleResponse>>> GetListAsync(
            GetRolesRequest request,
            CancellationToken cancellationToken = default)
        {
            var pageResult = new PageResult<RoleResponse>(
                [CreateResponse()],
                1,
                request.PageNumber,
                request.PageSize);

            return Task.FromResult(Result<PageResult<RoleResponse>>.Success(pageResult));
        }

        public Task<Result<RoleDetailResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<RoleDetailResponse>.Success(new RoleDetailResponse(
                RoleId,
                "super-admin",
                "SuperAdmin",
                null,
                true,
                true,
                [PermissionId],
                new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero),
                null)));
        }

        public Task<Result<RoleResponse>> CreateAsync(
            CreateRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result> UpdateAsync(
            Guid id,
            UpdateRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result> AssignPermissionsAsync(
            Guid id,
            AssignRolePermissionsRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }

        private static RoleResponse CreateResponse()
        {
            return new RoleResponse(
                RoleId,
                "super-admin",
                "SuperAdmin",
                null,
                true,
                true,
                new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero),
                null);
        }
    }
}
