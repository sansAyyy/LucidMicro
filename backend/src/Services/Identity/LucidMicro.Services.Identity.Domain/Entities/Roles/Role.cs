using LucidMicro.BuildingBlocks.Domain.Core.Entities;
using LucidMicro.BuildingBlocks.Domain.Core.Guards;

namespace LucidMicro.Services.Identity.Domain.Entities.Roles;

public class Role : SoftDeleteEntity<Guid>
{
    private Role()
    {
    }

    private Role(
        Guid id,
        string code,
        string name,
        string? description,
        bool isSystem,
        bool isEnabled)
    {
        Id = id;
        Code = DomainGuard.RequiredText(code, nameof(code), 64);
        IsSystem = isSystem;
        ApplyProfile(name, description, isEnabled);
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsSystem { get; private set; }

    public bool IsEnabled { get; private set; }

    public static Role Create(
        Guid id,
        string code,
        string name,
        string? description,
        bool isSystem,
        bool isEnabled)
    {
        return new Role(id, code, name, description, isSystem, isEnabled);
    }

    public void Update(string name, string? description, bool isEnabled)
    {
        ApplyProfile(name, description, isEnabled);
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    private void ApplyProfile(string name, string? description, bool isEnabled)
    {
        Name = DomainGuard.RequiredText(name, nameof(name), 128);
        Description = DomainGuard.OptionalText(description, nameof(description), 512);
        IsEnabled = isEnabled;
    }
}
