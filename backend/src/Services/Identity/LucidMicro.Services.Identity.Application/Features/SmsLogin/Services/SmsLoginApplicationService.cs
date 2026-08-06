using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.Contracts.Notification;
using LucidMicro.Contracts.Notification.Http.Requests;
using LucidMicro.Services.Identity.Application.ExternalServices.Notifications;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Responses;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Errors;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Options;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Services;

public sealed class SmsLoginApplicationService : ISmsLoginApplicationService
{
    private readonly IAccessTokenService _accessTokenService;
    private readonly IAdminAccessTokenClaimsFactory _accessTokenClaimsFactory;
    private readonly IRepository<AdminUser, Guid> _adminUsers;
    private readonly ISmsLoginCodeGenerator _codeGenerator;
    private readonly ISmsLoginCodeStore _codeStore;
    private readonly IValidator<LoginBySmsCodeRequest> _loginValidator;
    private readonly INotificationClient _notifications;
    private readonly SmsLoginOptions _options;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IValidator<SendSmsLoginCodeRequest> _sendCodeValidator;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public SmsLoginApplicationService(
        IValidator<SendSmsLoginCodeRequest> sendCodeValidator,
        IValidator<LoginBySmsCodeRequest> loginValidator,
        ISmsLoginCodeStore codeStore,
        ISmsLoginCodeGenerator codeGenerator,
        INotificationClient notifications,
        SmsLoginOptions options,
        IRepository<AdminUser, Guid> adminUsers,
        IUnitOfWork unitOfWork,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        TimeProvider timeProvider,
        IAdminAccessTokenClaimsFactory accessTokenClaimsFactory)
    {
        ArgumentNullException.ThrowIfNull(sendCodeValidator);
        ArgumentNullException.ThrowIfNull(loginValidator);
        ArgumentNullException.ThrowIfNull(codeStore);
        ArgumentNullException.ThrowIfNull(codeGenerator);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(adminUsers);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(accessTokenService);
        ArgumentNullException.ThrowIfNull(refreshTokenService);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(accessTokenClaimsFactory);
        options.Validate();

        _sendCodeValidator = sendCodeValidator;
        _loginValidator = loginValidator;
        _codeStore = codeStore;
        _codeGenerator = codeGenerator;
        _notifications = notifications;
        _options = options;
        _adminUsers = adminUsers;
        _unitOfWork = unitOfWork;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _timeProvider = timeProvider;
        _accessTokenClaimsFactory = accessTokenClaimsFactory;
    }

    public async Task<Result> SendCodeAsync(
        SendSmsLoginCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _sendCodeValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult.ToValidationError(SmsLoginErrors.ValidationErrorCode));
        }

        var phoneNumber = request.PhoneNumber!.Trim();
        if (!await _codeStore.CanSendAsync(phoneNumber, cancellationToken))
        {
            return Result.Failure(SmsLoginErrors.TooManyRequests());
        }

        var code = _codeGenerator.Generate();
        await _codeStore.SaveCodeAsync(phoneNumber, code, cancellationToken);

        var notificationResult = await _notifications.SendAsync(
            new SendNotificationRequest(
                phoneNumber,
                NotificationChannels.Sms,
                "SMS login verification code",
                $"Your verification code is {code}."),
            cancellationToken);

        if (notificationResult.IsFailure)
        {
            await _codeStore.RemoveCodeAsync(phoneNumber, cancellationToken);

            return Result.Failure(SmsLoginErrors.NotificationUnavailable());
        }

        return Result.Success();
    }

    public async Task<Result<SmsLoginResponse>> LoginAsync(
        LoginBySmsCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<SmsLoginResponse>.Failure(
                validationResult.ToValidationError(SmsLoginErrors.ValidationErrorCode));
        }

        var phoneNumber = request.PhoneNumber!.Trim();
        var savedCode = await _codeStore.GetCodeAsync(phoneNumber, cancellationToken);
        if (string.IsNullOrWhiteSpace(savedCode))
        {
            return Result<SmsLoginResponse>.Failure(SmsLoginErrors.CodeExpired());
        }

        if (!string.Equals(savedCode, request.Code!.Trim(), StringComparison.Ordinal))
        {
            var attempts = await _codeStore.IncrementAttemptAsync(phoneNumber, cancellationToken);
            if (attempts >= _options.MaxAttempts)
            {
                await _codeStore.RemoveCodeAsync(phoneNumber, cancellationToken);

                return Result<SmsLoginResponse>.Failure(SmsLoginErrors.TooManyAttempts());
            }

            return Result<SmsLoginResponse>.Failure(SmsLoginErrors.InvalidCode());
        }

        await _codeStore.RemoveCodeAsync(phoneNumber, cancellationToken);

        var adminUser = await _adminUsers.FirstOrDefaultAsync(
            new AdminUserByPhoneNumberSpecification(phoneNumber, null),
            cancellationToken);

        if (adminUser is null)
        {
            return Result<SmsLoginResponse>.Failure(SmsLoginErrors.InvalidCredentials());
        }

        if (!adminUser.IsActive)
        {
            return Result<SmsLoginResponse>.Failure(SmsLoginErrors.Disabled());
        }

        adminUser.MarkLogin(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SmsLoginResponse>.Success(
            await CreateLoginResponseAsync(adminUser, cancellationToken));
    }

    private async Task<SmsLoginResponse> CreateLoginResponseAsync(
        AdminUser adminUser,
        CancellationToken cancellationToken)
    {
        var tokenClaims = await _accessTokenClaimsFactory.CreateAsync(adminUser, cancellationToken);
        var accessToken = _accessTokenService.GenerateAccessToken(tokenClaims);
        var refreshToken = _refreshTokenService.GenerateRefreshToken(tokenClaims);

        return new SmsLoginResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt);
    }
}
