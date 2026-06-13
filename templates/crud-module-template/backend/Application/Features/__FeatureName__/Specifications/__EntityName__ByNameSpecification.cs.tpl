using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.__ServiceName__.Domain.Entities.__FeatureName__;

namespace LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Specifications;

internal sealed class __EntityName__ByNameSpecification : Specification<__EntityName__>
{
    public __EntityName__ByNameSpecification(string name, Guid? excludedId)
    {
        if (excludedId is null)
        {
            Where(entity => entity.Name == name);
            return;
        }

        Where(entity => entity.Name == name && entity.Id != excludedId.Value);
    }
}
