using FluentValidation.Results;
using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.BuildingBlocks.Application.Validation;

public static class ValidationResultExtensions
{
    public static Error ToValidationError(this ValidationResult validationResult, string code)
    {
        ArgumentNullException.ThrowIfNull(validationResult);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (validationResult.IsValid)
        {
            throw new InvalidOperationException("Cannot create a validation error from a valid validation result.");
        }

        var message = string.Join(
            "; ",
            validationResult.Errors
                .Select(failure => failure.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct());

        return Error.Validation(code, message);
    }
}
