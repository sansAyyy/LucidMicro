using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Responses;
using LucidMicro.Services.Identity.Application.Features.Roles.Errors;
using LucidMicro.Services.Identity.Application.Features.Roles.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Services;

public sealed class RoleApplicationService : IRoleApplicationService
{
    private readonly IValidator<CreateRoleRequest> _createRequestValidator;
    private readonly IReadOnlyRepository<Permission, Guid> _permissions;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IRepository<Role, Guid> _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateRoleRequest> _updateRequestValidator;

    public RoleApplicationService(
        IRepository<Role, Guid> roles,
        IReadOnlyRepository<Permission, Guid> permissions,
        IRolePermissionRepository rolePermissionRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateRoleRequest> createRequestValidator,
        IValidator<UpdateRoleRequest> updateRequestValidator)
    {
        _roles = roles;
        _permissions = permissions;
        _rolePermissionRepository = rolePermissionRepository;
        _unitOfWork = unitOfWork;
        _createRequestValidator = createRequestValidator;
        _updateRequestValidator = updateRequestValidator;
    }

    public async Task<Result<PageResult<RoleResponse>>> GetListAsync(
        GetRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageRequest = new PageRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        var specification = new RolesListSpecification(request.Keyword);

        var roles = await _roles.PageAsync(specification, pageRequest, cancellationToken);
        var responses = roles.Map(RoleResponse.FromEntity);

        return Result<PageResult<RoleResponse>>.Success(responses);
    }

    public async Task<Result<RoleDetailResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var role = await _roles.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return Result<RoleDetailResponse>.Failure(RoleErrors.NotFound(id));
        }

        var permissionIds = await _rolePermissionRepository.GetPermissionIdsAsync(id, cancellationToken);

        return Result<RoleDetailResponse>.Success(RoleDetailResponse.FromEntity(role, permissionIds));
    }

    public async Task<Result<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _createRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<RoleResponse>.Failure(
                validationResult.ToValidationError(RoleErrors.ValidationErrorCode));
        }

        var code = request.Code!.Trim();
        if (await _roles.AnyAsync(new RoleByCodeSpecification(code), cancellationToken))
        {
            return Result<RoleResponse>.Failure(RoleErrors.CodeConflict());
        }

        var role = Role.Create(
            Guid.NewGuid(),
            code,
            request.Name!.Trim(),
            NormalizeOptional(request.Description),
            isSystem: false,
            request.IsEnabled);

        await _roles.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RoleResponse>.Success(RoleResponse.FromEntity(role));
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _updateRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult.ToValidationError(RoleErrors.ValidationErrorCode));
        }

        var role = await _roles.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(id));
        }

        role.Update(request.Name!.Trim(), NormalizeOptional(request.Description), request.IsEnabled);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var role = await _roles.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(id));
        }

        _roles.Remove(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> AssignPermissionsAsync(
        Guid id,
        AssignRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var role = await _roles.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(id));
        }

        var permissionIds = request.PermissionIds.Distinct().ToArray();
        if (permissionIds.Length > 0)
        {
            var permissions = await _permissions.ListAsync(
                new PermissionsByIdsSpecification(permissionIds),
                cancellationToken);
            var existingIds = permissions.Select(permission => permission.Id).ToHashSet();
            var missingPermissionId = permissionIds.FirstOrDefault(permissionId => !existingIds.Contains(permissionId));
            if (missingPermissionId != Guid.Empty)
            {
                return Result.Failure(RoleErrors.PermissionNotFound(missingPermissionId));
            }
        }

        await _rolePermissionRepository.ReplacePermissionsAsync(id, permissionIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
