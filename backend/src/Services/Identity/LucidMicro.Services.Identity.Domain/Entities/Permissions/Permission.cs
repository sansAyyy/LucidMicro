using LucidMicro.BuildingBlocks.Domain.Core.Entities;
using LucidMicro.BuildingBlocks.Domain.Core.Guards;

namespace LucidMicro.Services.Identity.Domain.Entities.Permissions;

public class Permission : AuditableEntity<Guid>
{
    private Permission()
    {
    }

    private Permission(
        Guid id,
        string code,
        string name,
        string? description,
        string groupCode,
        string groupName,
        string resourceCode,
        string resourceName,
        string action,
        int sortOrder,
        bool isEnabled)
    {
        Id = id;
        Code = DomainGuard.RequiredText(code, nameof(code), 128);
        ApplyMetadata(name, description, groupCode, groupName, resourceCode, resourceName, action, sortOrder, isEnabled);
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string GroupCode { get; private set; } = string.Empty;

    public string GroupName { get; private set; } = string.Empty;

    public string ResourceCode { get; private set; } = string.Empty;

    public string ResourceName { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsEnabled { get; private set; }

    public static Permission Create(
        Guid id,
        string code,
        string name,
        string? description,
        string groupCode,
        string groupName,
        string resourceCode,
        string resourceName,
        string action,
        int sortOrder,
        bool isEnabled)
    {
        return new Permission(
            id,
            code,
            name,
            description,
            groupCode,
            groupName,
            resourceCode,
            resourceName,
            action,
            sortOrder,
            isEnabled);
    }

    public void UpdateMetadata(
        string name,
        string? description,
        string groupCode,
        string groupName,
        string resourceCode,
        string resourceName,
        string action,
        int sortOrder,
        bool isEnabled)
    {
        ApplyMetadata(name, description, groupCode, groupName, resourceCode, resourceName, action, sortOrder, isEnabled);
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    private void ApplyMetadata(
        string name,
        string? description,
        string groupCode,
        string groupName,
        string resourceCode,
        string resourceName,
        string action,
        int sortOrder,
        bool isEnabled)
    {
        Name = DomainGuard.RequiredText(name, nameof(name), 128);
        Description = DomainGuard.OptionalText(description, nameof(description), 512);
        GroupCode = DomainGuard.RequiredText(groupCode, nameof(groupCode), 64);
        GroupName = DomainGuard.RequiredText(groupName, nameof(groupName), 128);
        ResourceCode = DomainGuard.RequiredText(resourceCode, nameof(resourceCode), 64);
        ResourceName = DomainGuard.RequiredText(resourceName, nameof(resourceName), 128);
        Action = DomainGuard.RequiredText(action, nameof(action), 64);
        SortOrder = sortOrder;
        IsEnabled = isEnabled;
    }
}
