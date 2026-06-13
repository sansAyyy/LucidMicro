using LucidMicro.BuildingBlocks.Domain.Core.Entities;
using LucidMicro.BuildingBlocks.Domain.Core.Guards;

namespace LucidMicro.Services.__ServiceName__.Domain.Entities.__FeatureName__;

public class __EntityName__ : SoftDeleteEntity<Guid>
{
    private __EntityName__()
    {
    }

    private __EntityName__(Guid id, string name, bool isActive)
    {
        Id = id;
        Name = DomainGuard.RequiredText(name, nameof(name), __NameMaxLength__);
        IsActive = isActive;
    }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static __EntityName__ Create(Guid id, string name, bool isActive)
    {
        return new __EntityName__(id, name, isActive);
    }

    public void Update(string name, bool isActive)
    {
        Name = DomainGuard.RequiredText(name, nameof(name), __NameMaxLength__);

        if (isActive)
        {
            Activate();
            return;
        }

        Deactivate();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
