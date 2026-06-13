using LucidMicro.Services.__ServiceName__.Domain.Entities.__FeatureName__;

namespace LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Responses;

public sealed record __EntityName__Response(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt)
{
    public static __EntityName__Response FromEntity(__EntityName__ entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new __EntityName__Response(
            entity.Id,
            entity.Name,
            entity.IsActive,
            entity.CreatedAt,
            entity.LastModifiedAt);
    }
}
