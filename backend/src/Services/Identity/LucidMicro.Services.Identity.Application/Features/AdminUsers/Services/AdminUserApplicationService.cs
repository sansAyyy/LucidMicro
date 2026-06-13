using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Contracts.Notification;
using LucidMicro.Contracts.Notification.IntegrationEvents;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Responses;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Errors;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Services;

public sealed class AdminUserApplicationService : IAdminUserApplicationService
{
    private readonly IRepository<AdminUser, Guid> _adminUsers;
    private readonly IAdminUserRoleRepository _adminUserRoleRepository;
    private readonly IValidator<CreateAdminUserRequest> _createRequestValidator;
    private readonly IOutboxEventWriter _outbox;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IValidator<ResetAdminUserPasswordRequest> _resetPasswordRequestValidator;
    private readonly IReadOnlyRepository<Role, Guid> _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateAdminUserRequest> _updateRequestValidator;

    public AdminUserApplicationService(
        IRepository<AdminUser, Guid> adminUsers,
        IReadOnlyRepository<Role, Guid> roles,
        IAdminUserRoleRepository adminUserRoleRepository,
        IUnitOfWork unitOfWork,
        IOutboxEventWriter outbox,
        IPasswordHashingService passwordHashingService,
        IValidator<CreateAdminUserRequest> createRequestValidator,
        IValidator<ResetAdminUserPasswordRequest> resetPasswordRequestValidator,
        IValidator<UpdateAdminUserRequest> updateRequestValidator)
    {
        _adminUsers = adminUsers;
        _roles = roles;
        _adminUserRoleRepository = adminUserRoleRepository;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _passwordHashingService = passwordHashingService;
        _createRequestValidator = createRequestValidator;
        _resetPasswordRequestValidator = resetPasswordRequestValidator;
        _updateRequestValidator = updateRequestValidator;
    }

    public async Task<Result<PageResult<AdminUserResponse>>> GetListAsync(
        GetAdminUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageRequest = new PageRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        var specification = new AdminUsersListSpecification(request.Keyword);

        var adminUsers = await _adminUsers.PageAsync(specification, pageRequest, cancellationToken);
        var responses = adminUsers.Map(adminUser => AdminUserResponse.FromEntity(adminUser));

        return Result<PageResult<AdminUserResponse>>.Success(responses);
    }

    public async Task<Result<AdminUserResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminUser = await _adminUsers.GetByIdAsync(id, cancellationToken);
        if (adminUser is null)
        {
            return Result<AdminUserResponse>.Failure(AdminUserErrors.NotFound(id));
        }

        var roles = await _adminUserRoleRepository.GetRolesAsync(id, cancellationToken);

        return Result<AdminUserResponse>.Success(AdminUserResponse.FromEntity(adminUser, roles));
    }

    public async Task<Result<AdminUserResponse>> CreateAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _createRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AdminUserResponse>.Failure(
                validationResult.ToValidationError(AdminUserErrors.ValidationErrorCode));
        }

        var userName = request.UserName!.Trim();
        var email = request.Email!.Trim();
        var displayName = request.DisplayName!.Trim();
        var phoneNumber = NormalizeOptional(request.PhoneNumber);
        var passwordHash = _passwordHashingService.HashPassword(request.Password!);

        var conflictResult = await ValidateUniqueAsync(userName, email, phoneNumber, null, cancellationToken);
        if (conflictResult.IsFailure)
        {
            return Result<AdminUserResponse>.Failure(conflictResult.Error);
        }

        var adminUser = AdminUser.Create(
            Guid.NewGuid(),
            userName,
            email,
            displayName,
            phoneNumber,
            passwordHash,
            request.IsActive);

        await _adminUsers.AddAsync(adminUser, cancellationToken);
        await _outbox.AddAsync(CreateNotificationEvent(adminUser), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminUserResponse>.Success(AdminUserResponse.FromEntity(adminUser));
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _updateRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult.ToValidationError(AdminUserErrors.ValidationErrorCode));
        }

        var adminUser = await _adminUsers.GetByIdAsync(id, cancellationToken);
        if (adminUser is null)
        {
            return Result.Failure(AdminUserErrors.NotFound(id));
        }

        var userName = request.UserName!.Trim();
        var email = request.Email!.Trim();
        var displayName = request.DisplayName!.Trim();
        var phoneNumber = NormalizeOptional(request.PhoneNumber);

        var conflictResult = await ValidateUniqueAsync(userName, email, phoneNumber, id, cancellationToken);
        if (conflictResult.IsFailure)
        {
            return conflictResult;
        }

        adminUser.Update(userName, email, displayName, phoneNumber, request.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminUser = await _adminUsers.GetByIdAsync(id, cancellationToken);
        if (adminUser is null)
        {
            return Result.Failure(AdminUserErrors.NotFound(id));
        }

        _adminUsers.Remove(adminUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminUser = await _adminUsers.GetByIdAsync(id, cancellationToken);
        if (adminUser is null)
        {
            return Result.Failure(AdminUserErrors.NotFound(id));
        }

        adminUser.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminUser = await _adminUsers.GetByIdAsync(id, cancellationToken);
        if (adminUser is null)
        {
            return Result.Failure(AdminUserErrors.NotFound(id));
        }

        adminUser.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(
        Guid id,
        ResetAdminUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _resetPasswordRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult.ToValidationError(AdminUserErrors.ValidationErrorCode));
        }

        var adminUser = await _adminUsers.GetByIdAsync(id, cancellationToken);
        if (adminUser is null)
        {
            return Result.Failure(AdminUserErrors.NotFound(id));
        }

        adminUser.ChangePassword(_passwordHashingService.HashPassword(request.NewPassword!));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> AssignRolesAsync(
        Guid id,
        AssignAdminUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var adminUser = await _adminUsers.GetByIdAsync(id, cancellationToken);
        if (adminUser is null)
        {
            return Result.Failure(AdminUserErrors.NotFound(id));
        }

        var roleIds = request.RoleIds.Distinct().ToArray();
        if (roleIds.Length > 0)
        {
            var roles = await _roles.ListAsync(new RolesByIdsSpecification(roleIds), cancellationToken);
            var existingIds = roles.Select(role => role.Id).ToHashSet();
            var missingRoleId = roleIds.FirstOrDefault(roleId => !existingIds.Contains(roleId));
            if (missingRoleId != Guid.Empty)
            {
                return Result.Failure(AdminUserErrors.RoleNotFound(missingRoleId));
            }
        }

        await _adminUserRoleRepository.ReplaceRolesAsync(id, roleIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> ValidateUniqueAsync(
        string userName,
        string email,
        string? phoneNumber,
        Guid? excludedId,
        CancellationToken cancellationToken)
    {
        if (await _adminUsers.AnyAsync(new AdminUserByUserNameSpecification(userName, excludedId), cancellationToken))
        {
            return Result.Failure(AdminUserErrors.UserNameConflict());
        }

        if (await _adminUsers.AnyAsync(new AdminUserByEmailSpecification(email, excludedId), cancellationToken))
        {
            return Result.Failure(AdminUserErrors.EmailConflict());
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber)
            && await _adminUsers.AnyAsync(
                new AdminUserByPhoneNumberSpecification(phoneNumber, excludedId),
                cancellationToken))
        {
            return Result.Failure(AdminUserErrors.PhoneNumberConflict());
        }

        return Result.Success();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static NotificationSendRequestedIntegrationEvent CreateNotificationEvent(AdminUser adminUser)
    {
        return NotificationSendRequestedIntegrationEvent.Create(
            adminUser.Email,
            NotificationChannels.InApp,
            "Admin account created",
            $"Your admin account '{adminUser.UserName}' has been created.");
    }
}
