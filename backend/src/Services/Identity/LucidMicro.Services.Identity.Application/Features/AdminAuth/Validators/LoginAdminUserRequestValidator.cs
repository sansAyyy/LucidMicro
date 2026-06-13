using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Validators;

public sealed class LoginAdminUserRequestValidator : AbstractValidator<LoginAdminUserRequest>
{
    public LoginAdminUserRequestValidator()
    {
        RuleFor(request => request.LoginName)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("loginName is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 256))
            .WithMessage("loginName exceeds max length 256.");

        RuleFor(request => request.Password)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("password is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 2048))
            .WithMessage("password exceeds max length 2048.");
    }
}
