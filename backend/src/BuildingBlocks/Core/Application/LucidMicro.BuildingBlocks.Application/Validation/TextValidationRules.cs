namespace LucidMicro.BuildingBlocks.Application.Validation;

public static class TextValidationRules
{
    public static bool BeRequired(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static bool HaveTrimmedMaxLength(string? value, int maxLength)
    {
        return value is null || value.Trim().Length <= maxLength;
    }

    public static bool BeOptionalWithTrimmedMaxLength(string? value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maxLength;
    }
}
