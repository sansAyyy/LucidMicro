using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Requests;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Responses;

namespace LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Abstractions;

public interface I__FeatureName__ApplicationService
{
    Task<Result<PageResult<__EntityName__Response>>> GetListAsync(
        Get__FeatureName__Request request,
        CancellationToken cancellationToken = default);

    Task<Result<__EntityName__Response>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<__EntityName__Response>> CreateAsync(
        Create__EntityName__Request request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        Guid id,
        Update__EntityName__Request request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
