using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Abstractions;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Requests;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Responses;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Specifications;
using LucidMicro.Services.__ServiceName__.Domain.Entities.__FeatureName__;

namespace LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Services;

public sealed class __FeatureName__ApplicationService : I__FeatureName__ApplicationService
{
    private const string ValidationErrorCode = "__ServiceName__.__FeatureName__.Validation";

    private readonly IRepository<__EntityName__, Guid> ___entityNamePluralCamel__;
    private readonly IValidator<Create__EntityName__Request> _createRequestValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<Update__EntityName__Request> _updateRequestValidator;

    public __FeatureName__ApplicationService(
        IRepository<__EntityName__, Guid> __entityNamePluralCamel__,
        IUnitOfWork unitOfWork,
        IValidator<Create__EntityName__Request> createRequestValidator,
        IValidator<Update__EntityName__Request> updateRequestValidator)
    {
        ___entityNamePluralCamel__ = __entityNamePluralCamel__;
        _unitOfWork = unitOfWork;
        _createRequestValidator = createRequestValidator;
        _updateRequestValidator = updateRequestValidator;
    }

    public async Task<Result<PageResult<__EntityName__Response>>> GetListAsync(
        Get__FeatureName__Request request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageRequest = new PageRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        var specification = new __FeatureName__ListSpecification(request.Keyword);

        var entities = await ___entityNamePluralCamel__.PageAsync(specification, pageRequest, cancellationToken);
        var responses = entities.Map(__EntityName__Response.FromEntity);

        return Result<PageResult<__EntityName__Response>>.Success(responses);
    }

    public async Task<Result<__EntityName__Response>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await ___entityNamePluralCamel__.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result<__EntityName__Response>.Failure(NotFound(id));
        }

        return Result<__EntityName__Response>.Success(__EntityName__Response.FromEntity(entity));
    }

    public async Task<Result<__EntityName__Response>> CreateAsync(
        Create__EntityName__Request request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _createRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<__EntityName__Response>.Failure(validationResult.ToValidationError(ValidationErrorCode));
        }

        var name = request.Name!.Trim();

        var conflictResult = await ValidateUniqueAsync(name, null, cancellationToken);
        if (conflictResult.IsFailure)
        {
            return Result<__EntityName__Response>.Failure(conflictResult.Error);
        }

        var entity = __EntityName__.Create(Guid.NewGuid(), name, request.IsActive);

        await ___entityNamePluralCamel__.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<__EntityName__Response>.Success(__EntityName__Response.FromEntity(entity));
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        Update__EntityName__Request request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _updateRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult.ToValidationError(ValidationErrorCode));
        }

        var entity = await ___entityNamePluralCamel__.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(NotFound(id));
        }

        var name = request.Name!.Trim();

        var conflictResult = await ValidateUniqueAsync(name, id, cancellationToken);
        if (conflictResult.IsFailure)
        {
            return conflictResult;
        }

        entity.Update(name, request.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await ___entityNamePluralCamel__.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(NotFound(id));
        }

        ___entityNamePluralCamel__.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> ValidateUniqueAsync(
        string name,
        Guid? excludedId,
        CancellationToken cancellationToken)
    {
        if (await ___entityNamePluralCamel__.AnyAsync(new __EntityName__ByNameSpecification(name, excludedId), cancellationToken))
        {
            return Result.Failure(Error.Conflict("__ServiceName__.__FeatureName__.NameConflict", "__EntityName__ name already exists."));
        }

        return Result.Success();
    }

    private static Error NotFound(Guid id)
    {
        return Error.NotFound("__ServiceName__.__FeatureName__.NotFound", "__EntityName__ '" + id + "' was not found.");
    }
}
