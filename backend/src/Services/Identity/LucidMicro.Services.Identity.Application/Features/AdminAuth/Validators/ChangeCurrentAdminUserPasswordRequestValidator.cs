using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Validators;

public sealed class ChangeCurrentAdminUserPasswordRequestValidator
    : AbstractValidator<ChangeCurrentAdminUserPasswordRequest>
{
    public ChangeCurrentAdminUserPasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("currentPassword is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 2048))
            .WithMessage("currentPassword exceeds max length 2048.");

        RuleFor(request => request.NewPassword)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("newPassword is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 2048))
            .WithMessage("newPassword exceeds max length 2048.");

        RuleFor(request => request.ConfirmPassword)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("confirmPassword is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 2048))
            .WithMessage("confirmPassword exceeds max length 2048.")
            .Equal(request => request.NewPassword)
            .WithMessage("confirmPassword must match newPassword.");
    }
}
