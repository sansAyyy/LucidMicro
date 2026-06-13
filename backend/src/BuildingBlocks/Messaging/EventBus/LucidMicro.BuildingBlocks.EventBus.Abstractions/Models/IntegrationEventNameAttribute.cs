namespace LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventNameAttribute(string name) : Attribute
{
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Integration event name cannot be empty.", nameof(name))
        : name;
}
