using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Identity.Api.Controllers;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LucidMicro.Services.Identity.Api.Tests;

public sealed class AdminUsersHttpContractTests
{
    [Fact]
    public async Task GetList_ReturnsPagedAdminUsersContract()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.GetAsync("/api/admin-users?pageNumber=1&pageSize=10&keyword=admin");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, json.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("pageNumber").GetInt32());
        Assert.Equal(10, json.RootElement.GetProperty("pageSize").GetInt32());
        var item = json.RootElement.GetProperty("items").EnumerateArray().Single();
        Assert.Equal(TestAdminUserApplicationService.AdminUserId, item.GetProperty("id").GetGuid());
        Assert.Equal("admin", item.GetProperty("userName").GetString());
        Assert.Equal("admin@example.com", item.GetProperty("email").GetString());
        Assert.Equal("Admin", item.GetProperty("displayName").GetString());
        Assert.Equal("13800138000", item.GetProperty("phoneNumber").GetString());
        Assert.True(item.GetProperty("isActive").GetBoolean());
        Assert.Empty(item.GetProperty("roles").EnumerateArray());
        Assert.True(item.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task GetList_ReturnsUnauthorized_WhenAnonymous()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin-users?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AssignRoles_ReturnsNoContent()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        AddAdminToken(client);

        var response = await client.PutAsJsonAsync(
            $"/api/admin-users/{TestAdminUserApplicationService.AdminUserId}/roles",
            new AssignAdminUserRolesRequest
            {
                RoleIds = [TestAdminUserApplicationService.RoleId]
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
                new Claim("permission", "identity.admin-users.read"),
                new Claim("permission", "identity.admin-users.create"),
                new Claim("permission", "identity.admin-users.update"),
                new Claim("permission", "identity.admin-users.enable"),
                new Claim("permission", "identity.admin-users.disable"),
                new Claim("permission", "identity.admin-users.reset-password"),
                new Claim("permission", "identity.admin-users.delete")
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

    private sealed class TestApiFactory : WebApplicationFactory<AdminUsersController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IAdminUserApplicationService>();
                services.AddSingleton<IAdminUserApplicationService, TestAdminUserApplicationService>();
            });
        }
    }

    private sealed class TestAdminUserApplicationService : IAdminUserApplicationService
    {
        public static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public Task<Result<PageResult<AdminUserResponse>>> GetListAsync(
            GetAdminUsersRequest request,
            CancellationToken cancellationToken = default)
        {
            var pageResult = new PageResult<AdminUserResponse>(
                [CreateResponse()],
                1,
                request.PageNumber,
                request.PageSize);

            return Task.FromResult(Result<PageResult<AdminUserResponse>>.Success(pageResult));
        }

        public Task<Result<AdminUserResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result<AdminUserResponse>> CreateAsync(
            CreateAdminUserRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result> UpdateAsync(
            Guid id,
            UpdateAdminUserRequest request,
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

        public Task<Result> ActivateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result> DeactivateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result> ResetPasswordAsync(
            Guid id,
            ResetAdminUserPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result> AssignRolesAsync(
            Guid id,
            AssignAdminUserRolesRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }

        private static AdminUserResponse CreateResponse()
        {
            return new AdminUserResponse(
                AdminUserId,
                "admin",
                "admin@example.com",
                "Admin",
                "13800138000",
                true,
                null,
                [],
                new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero),
                null);
        }
    }
}
