using LucidMicro.BuildingBlocks.Domain.Core.Exceptions;

namespace LucidMicro.BuildingBlocks.Domain.Core.Guards;

public static class DomainGuard
{
    public static string RequiredText(string value, string fieldName, int maxLength)
    {
        var trimmedValue = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        if (trimmedValue.Length > maxLength)
        {
            throw new DomainException($"{fieldName} exceeds max length {maxLength}.");
        }

        return trimmedValue;
    }

    public static string? OptionalText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length > maxLength)
        {
            throw new DomainException($"{fieldName} exceeds max length {maxLength}.");
        }

        return trimmedValue;
    }
}
