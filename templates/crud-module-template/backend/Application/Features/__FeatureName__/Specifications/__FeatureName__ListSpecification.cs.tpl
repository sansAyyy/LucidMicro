using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.__ServiceName__.Domain.Entities.__FeatureName__;

namespace LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Specifications;

internal sealed class __FeatureName__ListSpecification : Specification<__EntityName__>
{
    public __FeatureName__ListSpecification(string? keyword)
    {
        var normalizedKeyword = NormalizeOptional(keyword);
        if (normalizedKeyword is not null)
        {
            Where(entity => entity.Name.Contains(normalizedKeyword));
        }

        OrderByDescending(entity => entity.CreatedAt);
        ApplyNoTracking();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
