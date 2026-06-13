using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.Contracts.Notification;
using LucidMicro.Contracts.Notification.Http.Requests;
using LucidMicro.Services.Identity.Application.ExternalServices.Notifications;
using LucidMicro.Services.Identity.Application.DependencyInjection;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Options;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Services;
using LucidMicro.Services.Identity.Application.Tests.TestDoubles;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Tests.Shared.Time;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.Services.Identity.Application.Tests;

public sealed class SmsLoginApplicationServiceTests
{
    [Fact]
    public void AddIdentityApplication_RegistersSmsLoginApplicationService()
    {
        var services = new ServiceCollection();

        AddSmsLoginDependencies(services);
        services.AddIdentityApplication();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();

        Assert.IsType<SmsLoginApplicationService>(service);
    }

    [Fact]
    public async Task SendCodeAsync_ReturnsValidationError_WhenPhoneNumberIsMissing()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();

        var result = await service.SendCodeAsync(new SendSmsLoginCodeRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.Validation", result.Error.Code);
    }

    [Fact]
    public async Task SendCodeAsync_SavesCodeAndSendsNotification_WhenRequestIsValid()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();
        var store = (TestSmsLoginCodeStore)provider.GetRequiredService<ISmsLoginCodeStore>();
        var notifications = (TestNotificationClient)provider.GetRequiredService<INotificationClient>();

        var result = await service.SendCodeAsync(new SendSmsLoginCodeRequest
        {
            PhoneNumber = " 13800138000 "
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("123456", store.Codes["13800138000"]);
        var request = Assert.Single(notifications.Requests);
        Assert.Equal("13800138000", request.Recipient);
        Assert.Equal(NotificationChannels.Sms, request.Channel);
        Assert.Contains("123456", request.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendCodeAsync_ReturnsTooManyRequests_WhenPhoneNumberIsRateLimited()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();
        var store = (TestSmsLoginCodeStore)provider.GetRequiredService<ISmsLoginCodeStore>();
        store.CanSend = false;

        var result = await service.SendCodeAsync(new SendSmsLoginCodeRequest
        {
            PhoneNumber = "13800138000"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.TooManyRequests", result.Error.Code);
    }

    [Fact]
    public async Task SendCodeAsync_RemovesCode_WhenNotificationFails()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();
        var store = (TestSmsLoginCodeStore)provider.GetRequiredService<ISmsLoginCodeStore>();
        var notifications = (TestNotificationClient)provider.GetRequiredService<INotificationClient>();
        notifications.Result = Result.Failure(Error.Failure("Notification.Failed", "Notification failed."));

        var result = await service.SendCodeAsync(new SendSmsLoginCodeRequest
        {
            PhoneNumber = "13800138000"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.NotificationUnavailable", result.Error.Code);
        Assert.False(store.Codes.ContainsKey("13800138000"));
    }

    [Fact]
    public async Task LoginAsync_ReturnsValidationError_WhenCodeIsMissing()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();

        var result = await service.LoginAsync(new LoginBySmsCodeRequest
        {
            PhoneNumber = "13800138000"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.Validation", result.Error.Code);
    }

    [Fact]
    public async Task LoginAsync_ReturnsCodeExpired_WhenCodeDoesNotExist()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();

        var result = await service.LoginAsync(new LoginBySmsCodeRequest
        {
            PhoneNumber = "13800138000",
            Code = "123456"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.CodeExpired", result.Error.Code);
    }

    [Fact]
    public async Task LoginAsync_IncrementsAttemptAndReturnsInvalidCode_WhenCodeDoesNotMatch()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();
        var store = (TestSmsLoginCodeStore)provider.GetRequiredService<ISmsLoginCodeStore>();
        await store.SaveCodeAsync("13800138000", "123456");

        var result = await service.LoginAsync(new LoginBySmsCodeRequest
        {
            PhoneNumber = "13800138000",
            Code = "654321"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.InvalidCode", result.Error.Code);
        Assert.Equal(1, store.Attempts["13800138000"]);
        Assert.True(store.Codes.ContainsKey("13800138000"));
    }

    [Fact]
    public async Task LoginAsync_RemovesCodeAndReturnsTooManyAttempts_WhenAttemptsReachLimit()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();
        var store = (TestSmsLoginCodeStore)provider.GetRequiredService<ISmsLoginCodeStore>();
        await store.SaveCodeAsync("13800138000", "123456");
        store.Attempts["13800138000"] = 4;

        var result = await service.LoginAsync(new LoginBySmsCodeRequest
        {
            PhoneNumber = "13800138000",
            Code = "654321"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.TooManyAttempts", result.Error.Code);
        Assert.False(store.Codes.ContainsKey("13800138000"));
        Assert.False(store.Attempts.ContainsKey("13800138000"));
    }

    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenAdminUserDoesNotExist()
    {
        using var provider = CreateServiceProvider();
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();
        var store = (TestSmsLoginCodeStore)provider.GetRequiredService<ISmsLoginCodeStore>();
        await store.SaveCodeAsync("13800138000", "123456");

        var result = await service.LoginAsync(new LoginBySmsCodeRequest
        {
            PhoneNumber = "13800138000",
            Code = "123456"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.InvalidCredentials", result.Error.Code);
        Assert.False(store.Codes.ContainsKey("13800138000"));
    }

    [Fact]
    public async Task LoginAsync_ReturnsDisabled_WhenAdminUserIsDisabled()
    {
        var adminUser = CreateAdminUser(isActive: false);
        using var provider = CreateServiceProvider(adminUser);
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();
        var store = (TestSmsLoginCodeStore)provider.GetRequiredService<ISmsLoginCodeStore>();
        await store.SaveCodeAsync(adminUser.PhoneNumber!, "123456");

        var result = await service.LoginAsync(new LoginBySmsCodeRequest
        {
            PhoneNumber = adminUser.PhoneNumber,
            Code = "123456"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Identity.SmsLogin.Disabled", result.Error.Code);
        Assert.False(store.Codes.ContainsKey(adminUser.PhoneNumber!));
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokensAndMarksLogin_WhenCodeMatchesAdminUserPhoneNumber()
    {
        var now = new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);
        var adminUser = CreateAdminUser();
        using var provider = CreateServiceProvider([adminUser], now);
        var service = provider.GetRequiredService<ISmsLoginApplicationService>();
        var store = (TestSmsLoginCodeStore)provider.GetRequiredService<ISmsLoginCodeStore>();
        var unitOfWork = provider.GetRequiredService<TestUnitOfWork>();
        var accessTokenService = provider.GetRequiredService<TestAccessTokenService>();
        await store.SaveCodeAsync(" 13800138000 ", "123456");

        var result = await service.LoginAsync(new LoginBySmsCodeRequest
        {
            PhoneNumber = " 13800138000 ",
            Code = " 123456 "
        });

        Assert.True(result.IsSuccess);
        Assert.Equal($"token:{adminUser.Id}", result.Value.AccessToken);
        Assert.Equal(new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero), result.Value.ExpiresAt);
        Assert.Equal($"refresh-token:{adminUser.Id}", result.Value.RefreshToken);
        Assert.Equal(new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero), result.Value.RefreshTokenExpiresAt);
        Assert.Equal(now.UtcDateTime, adminUser.LastLoginAt);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.False(store.Codes.ContainsKey("13800138000"));
        Assert.NotNull(accessTokenService.LastClaims);
        Assert.Equal(adminUser.Id.ToString(), accessTokenService.LastClaims.Subject);
        Assert.Equal(adminUser.UserName, accessTokenService.LastClaims.Name);
        Assert.Equal(adminUser.Email, accessTokenService.LastClaims.AdditionalClaims?["email"]);
        Assert.NotNull(accessTokenService.LastRefreshClaims);
        Assert.Equal(adminUser.Id.ToString(), accessTokenService.LastRefreshClaims.Subject);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        return CreateServiceProvider([]);
    }

    private static ServiceProvider CreateServiceProvider(params AdminUser[] adminUsers)
    {
        return CreateServiceProvider(adminUsers, new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero));
    }

    private static ServiceProvider CreateServiceProvider(
        AdminUser[] adminUsers,
        DateTimeOffset utcNow)
    {
        var services = new ServiceCollection();
        AddSmsLoginDependencies(services, adminUsers, utcNow);
        services.AddIdentityApplication();

        return services.BuildServiceProvider();
    }

    private static void AddSmsLoginDependencies(IServiceCollection services)
    {
        AddSmsLoginDependencies(services, [], new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero));
    }

    private static void AddSmsLoginDependencies(
        IServiceCollection services,
        AdminUser[] adminUsers,
        DateTimeOffset utcNow)
    {
        services.AddScoped<ISmsLoginCodeStore, TestSmsLoginCodeStore>();
        services.AddScoped<ISmsLoginCodeGenerator, TestSmsLoginCodeGenerator>();
        services.AddScoped<INotificationClient, TestNotificationClient>();
        services.AddScoped<IRepository<AdminUser, Guid>>(_ => new InMemoryAdminUserRepository(adminUsers));
        services.AddScoped<IUnitOfWork, TestUnitOfWork>();
        services.AddScoped<TestUnitOfWork>(serviceProvider => (TestUnitOfWork)serviceProvider.GetRequiredService<IUnitOfWork>());
        services.AddScoped<TestAccessTokenService>();
        services.AddScoped<IAccessTokenService>(serviceProvider => serviceProvider.GetRequiredService<TestAccessTokenService>());
        services.AddScoped<IRefreshTokenService>(serviceProvider => serviceProvider.GetRequiredService<TestAccessTokenService>());
        services.AddSingleton<TimeProvider>(new TestTimeProvider(utcNow));
        services.AddSingleton(new SmsLoginOptions
        {
            MaxAttempts = 5
        });
    }

    private static AdminUser CreateAdminUser(bool isActive = true)
    {
        return AdminUser.Create(
            Guid.NewGuid(),
            "admin",
            "admin@example.com",
            "Admin",
            "13800138000",
            "password-hash",
            isActive);
    }

    private sealed class TestSmsLoginCodeStore : ISmsLoginCodeStore
    {
        public Dictionary<string, string> Codes { get; } = [];

        public Dictionary<string, int> Attempts { get; } = [];

        public bool CanSend { get; set; } = true;

        public Task<bool> CanSendAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CanSend);
        }

        public Task SaveCodeAsync(
            string phoneNumber,
            string code,
            CancellationToken cancellationToken = default)
        {
            var normalizedPhoneNumber = phoneNumber.Trim();
            Codes[normalizedPhoneNumber] = code.Trim();
            Attempts.Remove(normalizedPhoneNumber);

            return Task.CompletedTask;
        }

        public Task<string?> GetCodeAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            Codes.TryGetValue(phoneNumber.Trim(), out var code);

            return Task.FromResult<string?>(code);
        }

        public Task RemoveCodeAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            var normalizedPhoneNumber = phoneNumber.Trim();
            Codes.Remove(normalizedPhoneNumber);
            Attempts.Remove(normalizedPhoneNumber);

            return Task.CompletedTask;
        }

        public Task<int> IncrementAttemptAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            var normalizedPhoneNumber = phoneNumber.Trim();
            var next = Attempts.GetValueOrDefault(normalizedPhoneNumber) + 1;
            Attempts[normalizedPhoneNumber] = next;

            return Task.FromResult(next);
        }
    }

    private sealed class TestSmsLoginCodeGenerator : ISmsLoginCodeGenerator
    {
        public string Generate()
        {
            return "123456";
        }
    }

    private sealed class TestNotificationClient : INotificationClient
    {
        public List<SendNotificationRequest> Requests { get; } = [];

        public Result Result { get; set; } = Result.Success();

        public Task<Result> SendAsync(
            SendNotificationRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            return Task.FromResult(Result);
        }
    }
}
