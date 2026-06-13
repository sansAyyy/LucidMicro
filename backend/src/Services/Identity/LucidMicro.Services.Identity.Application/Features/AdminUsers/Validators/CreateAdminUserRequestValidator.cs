using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Validators;

public sealed class CreateAdminUserRequestValidator : AbstractValidator<CreateAdminUserRequest>
{
    public CreateAdminUserRequestValidator()
    {
        RuleFor(request => request.UserName)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("userName is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 64))
            .WithMessage("userName exceeds max length 64.");

        RuleFor(request => request.Email)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("email is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 256))
            .WithMessage("email exceeds max length 256.");

        RuleFor(request => request.DisplayName)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("displayName is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 128))
            .WithMessage("displayName exceeds max length 128.");

        RuleFor(request => request.PhoneNumber)
            .Must(value => TextValidationRules.BeOptionalWithTrimmedMaxLength(value, 32))
            .WithMessage("phoneNumber exceeds max length 32.");

        RuleFor(request => request.Password)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("password is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 2048))
            .WithMessage("password exceeds max length 2048.");
    }
}
